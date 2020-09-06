using DDCImprover.Core.PreBlocks;
using Rocksmith2014.XML;
using Rocksmith2014.XML.Extensions;

using System;
using System.Linq;
using static DDCImprover.Core.XMLProcessor;

namespace DDCImprover.Core
{
    internal class XMLPreProcessor
    {
        private XMLProcessor Parent { get; }
        private InstrumentalArrangement Arrangement { get; }
        public int LastPhraseTime { get; }
        public int? FirstNGSectionTime { get; }

        private bool IsNonDDFile => Arrangement.Levels.Count == 1;

        private readonly Action<string> Log;

        internal XMLPreProcessor(XMLProcessor parent, Action<string> logAction)
        {
            Parent = parent;
            Arrangement = parent.OriginalArrangement!;
            Log = logAction;
            LastPhraseTime = Arrangement.PhraseIterations[^1].Time;

            if (Preferences.RestoreFirstNoguitarSection && IsNonDDFile)
            {
                var firstSection = Arrangement.Sections[0];

                if (firstSection.Name == "noguitar")
                {
                    FirstNGSectionTime = firstSection.Time;
                }
            }
        }

        internal void Process()
        {
            Log("-------------------- Preprocessing Started --------------------");

            if (Preferences.RestoreNoguitarSectionAnchors && IsNonDDFile)
                StoreNGSectionAnchors();

            var context = new ProcessorContext(Arrangement, Log);

            context
                // Add missing linknext to chords
                .ApplyFix(new EOFLinkNextChordTechNoteFix())

                // Shorten handshapes of chord slides
                .ApplyFix(new EOFChordSlideHandshapeLengthFix())

                // Correct any wrong crowd events
                .ApplyFix(new EOFCrowdEventsFix())

                // Add crowd events
                .ApplyFixIf(Preferences.AddCrowdEvents, new CrowdEventAdder())

                // Process 'move' phrases
                .ApplyFix(new PhraseMover(Parent.StatusMessages, Parent.AddedBeats))

                // Apply workaround for DDC moving phrases
                .ApplyFixIf(IsNonDDFile, new WeakBeatPhraseMovingFix())

                // Enable 'unpitchedSlides' arrangement property if needed
                .ApplyFix(new UnpitchedSlideChecker())

                // Adjust handshape lengths
                .ApplyFixIf(Preferences.AdjustHandshapes, new HandShapeAdjuster())

                // Process 'FIXOPEN' chords
                .ApplyFixIf(Preferences.FixChordNames, new FixOpenChordProcessor())

                // Generate tone events
                .ApplyFix(new ToneEventGenerator())

                // Process custom events
                .ApplyFix(new CustomEventsPreProcessor());

            if (Preferences.CheckXML)
            {
                Lint();

                if (Parent.StatusMessages.Count > 0)
                {
                    Parent.SortStatusMessages();
                }
            }

            SetRightHandOnTapNotes();

#if DEBUG
            RemoveChordNoteIgnores();
#endif

            Log("------------------- Preprocessing Completed --------------------");
        }

        /// <summary>
        /// Sets the 'rightHand' property to 1 on tapped notes.
        /// </summary>
        private void SetRightHandOnTapNotes()
        {
            int rightHandCount = 0;

            foreach (var note in Arrangement.Levels.SelectMany(lev => lev.Notes))
            {
                if (note.IsTap && !note.IsRightHand)
                {
                    note.IsRightHand = true;
                    rightHandCount++;
                }
            }

            if (rightHandCount != 0)
                Log($"Set 'rightHand' to 1 on {rightHandCount} tap note{(rightHandCount == 1 ? "" : "s")}.");
        }

        /// <summary>
        /// Removes useless ignores from chord notes.
        /// </summary>
        private void RemoveChordNoteIgnores()
        {
            int removeCount = 0;

            foreach (var chord in Arrangement.Levels.SelectMany(lev => lev.Chords))
            {
                if (chord.ChordNotes is null)
                    continue;

                foreach (var chordNote in chord.ChordNotes)
                {
                    if (chordNote.IsIgnore)
                    {
                        chordNote.IsIgnore = false;
                        removeCount++;
                    }
                }
            }

            if (removeCount != 0)
                Log($"Removed unnecessary ignore from {removeCount} chordNote{(removeCount == 1 ? "" : "s")}.");
        }

        /// <summary>
        /// Fix for DDC removing anchors from the beginning of noguitar sections.
        /// </summary>
        private void StoreNGSectionAnchors()
        {
            int firstBeatTime = Arrangement.Ebeats[0].Time;

            // Check for anchor at the beginning of the beatmap
            Anchor? firstBeatAnchor = Arrangement.Levels[0].Anchors.FindByTime(firstBeatTime);
            if (firstBeatAnchor is Anchor)
            {
                Log($"Stored the anchor found at the beginning of the beatmap at {firstBeatTime.TimeToString()}.");

                Parent.NGAnchors.Add(firstBeatAnchor);
            }

            // Check NG sections
            var ngSectionAnchors =
               from section in Arrangement.Sections
               join anchor in Arrangement.Levels[0].Anchors on section.Time equals anchor.Time
               where section.Name == "noguitar"
               select anchor;

            foreach (var ngAnchor in ngSectionAnchors)
            {
                Log($"Stored an anchor found at the beginning of a noguitar section at {ngAnchor.Time.TimeToString()}.");

                Parent.NGAnchors.Add(ngAnchor);
            }
        }

        /// <summary>
        /// Checks the XML for problems.
        /// </summary>
        private void Lint()
        {
            var arrangementChecker = new ArrangementChecker(Arrangement, Parent.StatusMessages, Log);
            arrangementChecker.RunAllChecks();
        }
    }
}