using DDCImprover.Core.PostBlocks;
using DynamicData;
using Rocksmith2014Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DDCImprover.Core
{
    public partial class XMLProcessor
    {
        internal class XMLPostProcessor
        {
            private XMLProcessor Parent { get; }
            private RS2014Song DDCSong { get; }
            private float OldLastPhraseTime { get; }
            private int OldPhraseIterationCount { get; }
            private float? FirstNGSectionTime { get; }
            private bool WasNonDDFile { get; }

            internal XMLPostProcessor(XMLProcessor parent)
            {
                Parent = parent;
                DDCSong = Parent.DDCSong;

                WasNonDDFile = Parent.OriginalSong.Levels.Count == 1;

                OldLastPhraseTime = Parent.preProcessor.LastPhraseTime;
                OldPhraseIterationCount = Parent.preProcessor.Song.PhraseIterations.Count;

                FirstNGSectionTime = Parent.preProcessor.FirstNGSectionTime;
            }

            private void Log(string str) => Parent.Log(str);

            internal void Process()
            {
                Log("-------------------- Postprocessing Started --------------------");


                var context = new PreProcessorContext(DDCSong, Log);

                context
                    // Remove temporary beats
                    .ApplyFixIf(Parent.AddedBeats.Count > 0, new TemporaryBeatRemover(Parent.AddedBeats))

                    // Restore anchors at the beginning of Noguitar sections
                    .ApplyFixIf(Preferences.RestoreNoguitarSectionAnchors && Parent.NGAnchors.Count > 0, new NoguitarAnchorRestorer(Parent.NGAnchors))

                    // Restore END phrase position if needed
                    .ApplyFixIf(WasNonDDFile, new ENDPhraseProcessor(OldLastPhraseTime))

                    // Process chord names
                    .ApplyFixIf(Preferences.FixChordNames && DDCSong?.ChordTemplates.Any() == true, new ChordNameProcessor(Parent.StatusMessages))

                    // Remove beats that come after the audio has ended
                    .ApplyFixIf(Preferences.RemoveBeatsPastAudioEnd, new ExtraneousBeatsRemover())

                    // Remove time signature events
                    .ApplyFixIf(Preferences.RemoveTimeSignatureEvents, new TimeSignatureEventRemover())

                    // Add second level to phrases with only one level
                    .ApplyFixIf(Preferences.FixOneLevelPhrases, new OneLevelPhraseFixer())

                    // Restore first noguitar section
                    .ApplyFixIf(WasNonDDFile && FirstNGSectionTime.HasValue, new FirstNoguitarSectionRestorer(FirstNGSectionTime.Value));

                //RemoveTemporaryBeats();

                //if (Preferences.RestoreNoguitarSectionAnchors && Parent.NGAnchors.Count > 0)
                //    RestoreNGSectionAnchors();

                if (WasNonDDFile)
                {
                    //ProcessENDPhrase();

                    RemoveTemporaryMeasures();
                }

                //if (Preferences.FixChordNames && DDCSong?.ChordTemplates.Any() == true)
                //    ProcessChordNames();

                //if ()
                //    RemoveBeatsPastAudioEnd();

                //if (Preferences.RemoveTimeSignatureEvents)
                //    RemoveTimeSignatureEvents();

                CheckPhraseIterationCount();

                if (WasNonDDFile)
                {
                    RemoveUnnecessaryNGPhrase();

                    //if (Preferences.FixOneLevelPhrases)
                    //    ProcessOneLevelPhrases();

                    //if (FirstNGSectionTime != null)
                    //    RestoreFirstNGSection();
                }

                PostProcessCustomEvents();

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
            /// Postprocesses custom events. Available:
            /// "so"
            /// </summary>
            private void PostProcessCustomEvents()
            {
                var events = DDCSong.Events;

                var slideoutEvents = events.Where(ev => ev.Code.StartsWith("so", StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var slideEvent in slideoutEvents)
                {
                    float? parsedTime = TimeParser.Parse(slideEvent.Code);
                    float slideTime = parsedTime ?? slideEvent.Time;

                    // Find the max level for the phrase the slide is in
                    var phraseIter = DDCSong.PhraseIterations.Last(pi => pi.Time < slideTime);
                    int diff = DDCSong.Phrases[phraseIter.PhraseId].MaxDifficulty;
                    var level = DDCSong.Levels[diff];

                    int noteIndex = level.Notes.FindIndexByTime(slideTime);
                    int chordIndex = level.Chords.FindIndexByTime(slideTime);
                    if (noteIndex == -1 && chordIndex == -1)
                    {
                        string error = $"Could not find the notes or chord for SlideOut event at {slideEvent.Time.TimeToString()}";
                        Parent.StatusMessages.Add(new ImproverMessage(error, MessageType.Error, slideEvent.Time));
                        Log(error);
                        continue;
                    }

                    var notes = new List<Note>();
                    ChordTemplate originalChordTemplate;

                    if (noteIndex != -1) // These are notes that follow a linknext chord
                    {
                        do
                        {
                            var note = level.Notes[noteIndex];
                            if (note.IsUnpitchedSlide)
                                notes.Add(note);
                            noteIndex++;
                        } while (noteIndex < level.Notes.Count && Utils.TimeEqualToMilliseconds(level.Notes[noteIndex].Time, slideTime));

                        // Find the chord that is linked to the slide, its template and handshape
                        var linkNextChord = level.Chords.Last(c => c.Time < slideTime);
                        var linkNextChordHs = level.HandShapes.FirstOrDefault(hs => Utils.TimeEqualToMilliseconds(hs.StartTime, linkNextChord.Time));
                        originalChordTemplate = DDCSong.ChordTemplates[linkNextChord.ChordId];

                        // Shorten handshapes that EOF has set to include the slide out
                        if (linkNextChordHs != null && linkNextChordHs.EndTime > linkNextChord.Time + linkNextChord.ChordNotes[0].Sustain)
                        {
                            linkNextChordHs.EndTime = (float)Math.Round(
                                linkNextChord.Time + linkNextChord.ChordNotes[0].Sustain,
                                3,
                                MidpointRounding.AwayFromZero);
                        }
                    }
                    else // It is a normal chord with unpitched slide out
                    {
                        var chord = level.Chords[chordIndex];
                        notes.AddRange(chord.ChordNotes.Where(cn => cn.IsUnpitchedSlide));

                        originalChordTemplate = DDCSong.ChordTemplates[chord.ChordId];

                        // The length of the handshape likely needs to be shortened
                        var chordHs = level.HandShapes.FirstOrDefault(hs => Utils.TimeEqualToMilliseconds(hs.StartTime, chord.Time));
                        chordHs.EndTime = (float)Math.Round(
                            chordHs.StartTime + ((chordHs.EndTime - chordHs.StartTime) / 3),
                            3,
                            MidpointRounding.AwayFromZero);
                    }

                    if (notes.Count == 0)
                    {
                        string error = $"Invalid SlideOut event at {slideEvent.Time.TimeToString()}";
                        Parent.StatusMessages.Add(new ImproverMessage(error, MessageType.Error, slideEvent.Time));
                        Log(error);
                        continue;
                    }

                    // Create a new handshape at the slide end
                    float endTime = notes[0].Time + notes[0].Sustain;
                    float startTime = endTime - Math.Min(notes[0].Sustain / 3, 250f);
                    int chordId = DDCSong.ChordTemplates.Count;
                    level.HandShapes.InsertByTime(new HandShape(chordId, startTime, endTime));

                    // Create a new chordtemplate for the handshape
                    var soChordTemplate = new ChordTemplate();
                    foreach (var note in notes)
                    {
                        soChordTemplate.Frets[note.String] = note.SlideUnpitchTo;
                        soChordTemplate.Fingers[note.String] = originalChordTemplate.Fingers[note.String];
                    }
                    DDCSong.ChordTemplates.Add(soChordTemplate);

                    Log($"Processed SlideOut event at {slideEvent.Time.TimeToString()}");
                }

                events.Remove(slideoutEvents);
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
}
