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

                // Removes DDC's noguitar phrase if it is no longer needed due to END phrase restoration
                .ApplyFixIf(WasNonDDFile, new UnnecessaryNGPhraseRemover())

                // Process chord names
                .ApplyFixIf(Preferences.FixChordNames && DDCSong.ChordTemplates.Any(), new ChordNameProcessor(Parent.StatusMessages))

                // Remove beats that come after the audio has ended
                .ApplyFixIf(Preferences.RemoveBeatsPastAudioEnd, new ExtraneousBeatsRemover())

                // Remove time signature events
                .ApplyFixIf(Preferences.RemoveTimeSignatureEvents, new TimeSignatureEventRemover())

                // Add second level to phrases with only one level
                .ApplyFixIf(Preferences.FixOneLevelPhrases, new OneLevelPhraseFixer())

                // Process custom events
                .ApplyFix(new CustomEventPostProcessor(Parent.StatusMessages))

                // Remove notes that are placeholders for anchors
                .ApplyFixIf(Preferences.RemoveAnchorPlaceholderNotes, new AnchorPlaceholderNoteRemover())

                // Removes "high density" statuses and chord notes from chords that have them
                .ApplyFixIf(Preferences.RemoveHighDensityStatuses, new HighDensityRemover());

            // Restore first noguitar section
            if (WasNonDDFile && FirstNGSectionTime.HasValue)
                context.ApplyFix(new FirstNoguitarSectionRestorer(FirstNGSectionTime.Value));

            if (WasNonDDFile)
            {
                RemoveTemporaryMeasures();

                if (Preferences.CheckForArrIdReset)
                {
                    CheckNeedToResetArrangementId();

                    PhraseLevelRepository.QueueForSave(Parent.XMLFileFullPath, DDCSong.Phrases);
                }
            }

            if(Preferences.RemoveTranscriptionTrack)
                DDCSong.TranscriptionTrack = new Level();

            ValidateDDCXML();

            Log("-------------------- Postprocessing Completed --------------------");
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

        private void CheckNeedToResetArrangementId()
        {
            var phraseLevels = PhraseLevelRepository.TryGetLevels(Parent.XMLFileFullPath);

            if (phraseLevels != null)
            {
                foreach (var phrase in DDCSong.Phrases)
                {
                    if (phraseLevels.ContainsKey(phrase.Name)
                       && phraseLevels[phrase.Name] > phrase.MaxDifficulty)
                    {
                        const string msg = "At least one phrase has lower max difficulty compared to previous DD generation.";

                        Log(msg);
                        Parent.StatusMessages.Add(new ImproverMessage(msg + " The arrangement id should be reset.", MessageType.Warning));
                        return;
                    }
                }
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
