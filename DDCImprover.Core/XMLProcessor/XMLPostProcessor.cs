using DDCImprover.Core.PostBlocks;
using Rocksmith2014Xml;
using System;
using System.Linq;
using static DDCImprover.Core.XMLProcessor;

namespace DDCImprover.Core
{
    internal sealed class XMLPostProcessor
    {
        private XMLProcessor Parent { get; }
        private RS2014Song DDCSong { get; }
        private float OldLastPhraseTime { get; }
        private int OldPhraseIterationCount { get; }
        private float? FirstNGSectionTime { get; }
        private bool WasNonDDFile { get; }

        private readonly Action<string> Log;

        internal XMLPostProcessor(XMLProcessor parent, XMLPreProcessor preProcessor, Action<string> logAction)
        {
            Parent = parent;
            DDCSong = Parent.DDCSong;
            Log = logAction;

            WasNonDDFile = Parent.OriginalSong.Levels.Count == 1;

            OldLastPhraseTime = preProcessor.LastPhraseTime;
            OldPhraseIterationCount = Parent.OriginalSong.PhraseIterations.Count;

            FirstNGSectionTime = preProcessor.FirstNGSectionTime;
        }

        internal void Process()
        {
            Log("-------------------- Postprocessing Started --------------------");

            CheckPhraseIterationCount();

            var context = new ProcessorContext(DDCSong, Log);

            context
                // Remove temporary beats
                .ApplyFixIf(Parent.AddedBeats.Count > 0, new TemporaryBeatRemover(Parent.AddedBeats))

                // Restore anchors at the beginning of Noguitar sections
                .ApplyFixIf(Preferences.RestoreNoguitarSectionAnchors && Parent.NGAnchors.Count > 0, new NoguitarAnchorRestorer(Parent.NGAnchors))

                // Restore END phrase to original position if needed
                .ApplyFixIf(WasNonDDFile, new ENDPhraseProcessor(OldLastPhraseTime))

                // Process chord names
                .ApplyFixIf(Preferences.FixChordNames && DDCSong?.ChordTemplates.Any() == true, new ChordNameProcessor(Parent.StatusMessages))

                // Remove beats that come after the audio has ended
                .ApplyFixIf(Preferences.RemoveBeatsPastAudioEnd, new ExtraneousBeatsRemover())

                // Remove time signature events
                .ApplyFixIf(Preferences.RemoveTimeSignatureEvents, new TimeSignatureEventRemover())

                // Add second level to phrases with only one level
                .ApplyFixIf(Preferences.FixOneLevelPhrases, new OneLevelPhraseFixer())

                // Process custom events
                .ApplyFix(new CustomEventPostProcessor(Parent.StatusMessages))

                // Remove notes that are placeholders for anchors
                .ApplyFixIf(Preferences.RemoveAnchorPlaceholderNotes, new AnchorPlaceholderNoteRemover());

            // Restore first noguitar section
            if (WasNonDDFile && FirstNGSectionTime.HasValue)
                context.ApplyFix(new FirstNoguitarSectionRestorer(FirstNGSectionTime.Value));

            if (WasNonDDFile)
            {
                RemoveTemporaryMeasures();

                RemoveUnnecessaryNGPhrase();
            }

#if DEBUG
            RemoveHighDensityStatuses();
#endif

            ValidateDDCXML();

            Log("-------------------- Postprocessing Completed --------------------");
        }

        private void RemoveHighDensityStatuses()
        {
            int hiDensRemoved = 0;

            foreach (var chord in DDCSong.Levels.SelectMany(lev => lev.Chords))
            {
                if (chord.IsHighDensity)
                {
                    chord.IsHighDensity = false;
                    ++hiDensRemoved;
                }
            }

            if (hiDensRemoved > 0)
                Log($"{hiDensRemoved} high density statuses removed.");
        }

        /// <summary>
        /// Removes DDC's noguitar phrase if no phrase iterations using it are found.
        /// </summary>
        private void RemoveUnnecessaryNGPhrase()
        {
            const int ngPhraseId = 1;

            if (DDCSong.Phrases[ngPhraseId].MaxDifficulty != 0)
                return;

            var ngPhrasesIterations = from pi in DDCSong.PhraseIterations
                                      where pi.PhraseId == ngPhraseId
                                      select pi;

            if (!ngPhrasesIterations.Any())
            {
                Log("Removed unnecessary noguitar phrase.");

                DDCSong.Phrases.RemoveAt(ngPhraseId);

                // Set correct phrase IDs for phrase iterations
                foreach (var pi in DDCSong.PhraseIterations)
                {
                    if (pi.PhraseId > ngPhraseId)
                    {
                        --pi.PhraseId;
                    }
                }

                // Set correct phrase IDs for NLDs
                foreach (var nld in DDCSong.NewLinkedDiffs)
                {
                    for (int i = 0; i < nld.PhraseCount; i++)
                    {
                        if (nld.Phrases[i].Id > ngPhraseId)
                        {
                            nld.Phrases[i] = new NLDPhrase(nld.Phrases[i].Id - 1);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes the temporary measures used to prevent DDC from moving phrase start positions.
        /// </summary>
        private void RemoveTemporaryMeasures()
        {
            foreach (var beat in DDCSong.Ebeats)
            {
                if (beat.Measure == TempMeasureNumber)
                {
                    beat.Measure = -1;
                }
            }
        }

        private void CheckPhraseIterationCount()
        {
            int newPhraseIterationCount = DDCSong.PhraseIterations.Count;

            if (newPhraseIterationCount != OldPhraseIterationCount)
            {
                Log($"PhraseIteration count does not match (Old: {OldPhraseIterationCount}, new: {newPhraseIterationCount})");
            }
        }

        private void ValidateDDCXML()
        {
            if (DDCSong.Levels.SelectMany(lev => lev.HandShapes).Any(hs => hs.ChordId == -1))
                throw new DDCException("DDC has created a handshape with an invalid chordId (-1).");

            // Check for DDC bug where muted notes with sustain are generated
            if (WasNonDDFile)
            {
                var notes = DDCSong.Levels.SelectMany(lev => lev.Notes);
                var mutedNotesWithSustain =
                    from note in notes
                    where note.IsMute && note.Sustain > 0.0f
                    select note;

                foreach (var note in mutedNotesWithSustain)
                {
                    bool originallyHadMutedNote = Parent.OriginalSong.Levels
                        .SelectMany(lev => lev.Notes)
                        .Any(n => Utils.TimeEqualToMilliseconds(n.Time, note.Time) && n.IsMute);

                    bool originallyHadMutedChord = Parent.OriginalSong.Levels
                        .SelectMany(lev => lev.Chords)
                        .SelectMany(c => c.ChordNotes)
                        .Any(cn => Utils.TimeEqualToMilliseconds(cn.Time, note.Time) && cn.IsMute && cn.Sustain != 0.0f);

                    if (!originallyHadMutedNote && !originallyHadMutedChord)
                    {
                        Parent.StatusMessages.Add(new ImproverMessage($"DDC generated muted note with sustain at {note.Time.TimeToString()}", MessageType.Warning, note.Time));
                    }
                }
            }
        }
    }
}
