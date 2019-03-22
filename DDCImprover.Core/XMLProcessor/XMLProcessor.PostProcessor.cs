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
            private readonly XMLProcessor Parent;
            private readonly RS2014Song DDCSong;
            private readonly PhraseIterationCollection OriginalPhraseIterations;

            private float NewLastPhraseTime;
            private int NewPhraseIterationCount;
            private readonly float OldLastPhraseTime;
            private readonly int OldPhraseIterationCount;
            //private readonly float FirstPhraseTime;
            private readonly float? FirstNGSectionTime;

            internal XMLPostProcessor(XMLProcessor parent)
            {
                Parent = parent;
                DDCSong = Parent.DDCSong;

                OldLastPhraseTime = Parent.preProcessor.LastPhraseTime;
                OldPhraseIterationCount = Parent.preProcessor.PhraseIterationCount;
                OriginalPhraseIterations = Parent.preProcessor.PhraseIterations;

                //FirstPhraseTime = Parent.preProcessor.FirstPhraseTime;
                FirstNGSectionTime = Parent.preProcessor.FirstNGSectionTime;
            }

            private void Log(string str) => Parent.Log(str);

            internal void Process()
            {
                Log("-------------------- Postprocessing Started --------------------");

                RemoveTemporaryBeats();

                if (Preferences.RestoreNoguitarSectionAnchors && Parent.NGAnchors.Count > 0)
                    RestoreNGSectionAnchors();

                if (Parent.isNonDDFile)
                {
                    ProcessENDPhrase();

                    RemoveTemporaryMeasures();

                    //RestoreFirstBeatTime();
                }

                if (Preferences.FixChordNames && DDCSong?.ChordTemplates.Any() == true)
                    ProcessChordNames();

                if (Preferences.RemoveBeatsPastAudioEnd)
                    RemoveBeatsPastAudioEnd();

                if (Preferences.RemoveTimeSignatureEvents)
                    RemoveTimeSignatureEvents();

                CheckPhraseIterationCount();

                if (Parent.isNonDDFile)
                {
                    RemoveUnnecessaryNGPhrase();

                    if (Preferences.FixOneLevelPhrases)
                        ProcessOneLevelPhrases();

                    if (FirstNGSectionTime != null)
                        RestoreFirstNGSection();

                    //if (Preferences.RemoveEmptyPhrasesAddedByDDC)
                    //    RemoveEmptyPhrasesAddedByDDC();
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

                    int noteIndex = level.Notes.FindIndex(n => Utils.TimeEqualToMilliseconds(n.Time, slideTime));
                    if (noteIndex == -1)
                    {
                        string error = $"Could not find the notes for SlideOut event at {slideEvent.Time.TimeToString()}";
                        Parent.StatusMessages.Add(new ImproverMessage(error, MessageType.Error, slideEvent.Time));
                        Log(error);
                        continue;
                    }

                    List<Note> notes = new List<Note>();
                    do
                    {
                        var note = level.Notes[noteIndex];
                        if (note.IsUnpitchedSlide)
                            notes.Add(note);
                        noteIndex++;
                    } while (noteIndex < level.Notes.Count && Utils.TimeEqualToMilliseconds(level.Notes[noteIndex].Time, slideTime));

                    // Find the chord that is linked to the slide, its template and handshape
                    var linkNextChord = level.Chords.Last(c => c.Time < slideTime);
                    var linkNextChordTemplate = DDCSong.ChordTemplates[linkNextChord.ChordId];
                    var linkNextChordHs = level.HandShapes.FirstOrDefault(hs => Utils.TimeEqualToMilliseconds(hs.StartTime, linkNextChord.Time));

                    // Shorten handshapes that EOF has set to include the slide out
                    if(linkNextChordHs != null && linkNextChordHs.EndTime > linkNextChord.Time + linkNextChord.ChordNotes[0].Sustain)
                    {
                        linkNextChordHs.EndTime = (float)Math.Round(
                            linkNextChord.Time + linkNextChord.ChordNotes[0].Sustain,
                            3,
                            MidpointRounding.AwayFromZero);
                    }

                    // Create a new handshape at the slide end
                    float endTime = notes[0].Time + notes[0].Sustain;
                    float startTime = endTime - Math.Min(notes[0].Sustain / 2, 250f);
                    int chordId = DDCSong.ChordTemplates.Count;
                    var handShape = new HandShape(chordId, startTime, endTime);

                    var ct = new ChordTemplate();
                    sbyte minFret = notes.Min(n => n.Fret);
                    sbyte slideTo = notes[0].SlideUnpitchTo;

                    foreach (var note in notes)
                    {
                        ct.Frets[note.String] = (sbyte)(note.Fret - minFret + slideTo);
                        ct.Fingers[note.String] = linkNextChordTemplate.Fingers[note.String];
                    }

                    DDCSong.ChordTemplates.Add(ct);
                    level.HandShapes.InsertByTime(handShape);

                    Log($"Processed SlideOut event at {slideEvent.Time.TimeToString()}");

                    events.Remove(slideEvent);
                }
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
            /// Removes temporary beats used for phrase moving.
            /// </summary>
            private void RemoveTemporaryBeats()
            {
                if (Parent.addedBeats.Count > 0)
                {
                    foreach (var addedBeat in Parent.addedBeats)
                    {
                        var beatToRemove = DDCSong.Ebeats.Find(beat => beat == addedBeat);
                        DDCSong.Ebeats.Remove(beatToRemove);
                    }
                }
            }

            private void RestoreFirstNGSection()
            {
                // Increase the number attribute of any following noguitar sections
                foreach (var section in DDCSong.Sections.Where(s => s.Name == "noguitar"))
                {
                    ++section.Number;
                }

                // Add removed noguitar section back
                var newFirstSection = new Section("noguitar", FirstNGSectionTime.Value, 1);
                DDCSong.Sections.Insert(0, newFirstSection);

                // Add a new NG phrase as the last phrase
                var newNGPhrase = new Phrase("NG", maxDifficulty: 0, PhraseMask.None);

                DDCSong.Phrases.Add(newNGPhrase);

                // Recreate removed phrase iteration (with phraseId of new NG phrase)
                var newNGPhraseIteration = new PhraseIteration
                {
                    Time = FirstNGSectionTime.Value,
                    PhraseId = DDCSong.Phrases.Count - 1,
                    Variation = string.Empty
                };

                // Add after the first phraseIteration (COUNT)
                DDCSong.PhraseIterations.Insert(1, newNGPhraseIteration);

                Log("Restored first noguitar section.");
            }

            /// <summary>
            /// Restores anchors at the beginning of noguitar sections.
            /// </summary>
            private void RestoreNGSectionAnchors()
            {
                Log("Restoring noguitar section anchors:");

                var firstLevelAnchors = DDCSong.Levels[0].Anchors;

                foreach (Anchor anchor in Parent.NGAnchors)
                {
                    // Add anchor to the first difficulty level
                    int nextAnchorIndex = firstLevelAnchors.FindIndex(a => a.Time > anchor.Time);

                    if (nextAnchorIndex != -1)
                    {
                        firstLevelAnchors.Insert(nextAnchorIndex, anchor);
                    }
                    else
                    {
                        // DDC may have moved the noguitar section
                        if (firstLevelAnchors.Any(a => a.Time == anchor.Time))
                            continue;
                        else
                            firstLevelAnchors.Add(anchor);
                    }

                    Log($"--Restored anchor at time {anchor.Time.TimeToString()}");
                }
            }

            /// <summary>
            /// Checks if DDC has moved the END phrase and restores its original position.
            /// </summary>
            private void ProcessENDPhrase()
            {
                NewLastPhraseTime = DDCSong.PhraseIterations.Last().Time;

                if (!Utils.TimeEqualToMilliseconds(NewLastPhraseTime, OldLastPhraseTime))
                {
                    Log($"DDC has moved END phrase from {OldLastPhraseTime.TimeToString()} to {NewLastPhraseTime.TimeToString()}.");

                    // Restore correct time to last section and phrase iteration
                    if (Preferences.PreserveENDPhraseLocation)
                    {
                        // Check if DDC has added an empty phrase to where we want to move the END phrase
                        var ddcAddedPhraseIteration = DDCSong.PhraseIterations.FirstOrDefault(pi => Utils.TimeEqualToMilliseconds(pi.Time, OldLastPhraseTime));
                        if (ddcAddedPhraseIteration != null)
                        {
                            DDCSong.PhraseIterations.Remove(ddcAddedPhraseIteration);

                            Log($"--Removed phrase added by DDC at {OldLastPhraseTime.TimeToString()}.");
                        }

                        DDCSong.PhraseIterations.Last().Time = OldLastPhraseTime;
                        DDCSong.Sections.Last().Time = OldLastPhraseTime;

                        Log("--Restored END phrase location.");
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

            /// <summary>
            /// Restores correct time for the first phrase (COUNT).
            /// </summary>
            /*private void RestoreFirstBeatTime()
            {
                if (FirstPhraseTime > 0.0)
                {
                    Log("Restored first phrase time.");

                    DDCSong.PhraseIterations[0].Time = FirstPhraseTime;

                    DDCSong.Ebeats[0].Measure = 1;
                }
            }*/

            /// <summary>
            /// Renames chord names to match ODLC and processes chord name commands.
            /// </summary>
            private void ProcessChordNames()
            {
                var chordRenamed = new Dictionary<string, bool>();

                Log("Processing chord names...");

                for (int i = 0; i < DDCSong.ChordTemplates.Count; i++)
                {
                    var currentChordTemplate = DDCSong.ChordTemplates[i];
                    string chordName = currentChordTemplate.ChordName;

                    // One fret handshape fret moving
                    if (chordName.StartsWith("OF"))
                    {
                        Match match = Regex.Match(chordName, @"\d+$");
                        if (match.Success)
                        {
                            sbyte newFretNumber = sbyte.Parse(match.Value, NumberFormatInfo.InvariantInfo);

                            for (int fretIndex = 0; fretIndex < 6; fretIndex++)
                            {
                                if (currentChordTemplate.Frets[fretIndex] == 0)
                                {
                                    // Remove unnecessary open string notes
                                    currentChordTemplate.Frets[fretIndex] = -1;
                                }
                                else if (currentChordTemplate.Frets[fretIndex] != -1)
                                {
                                    currentChordTemplate.Frets[fretIndex] = newFretNumber;
                                }
                            }

                            Log($"Adjusted fret number of one fret chord: {currentChordTemplate.ChordName}");

                            // Remove chord name
                            currentChordTemplate.ChordName = "";
                            currentChordTemplate.DisplayName = "";
                        }
                        else
                        {
                            const string errorMessage = "Unable to read fret value from OF chord.";
                            Parent.StatusMessages.Add(new ImproverMessage(errorMessage, MessageType.Warning));
                            Log(errorMessage);
                        }
                    }

                    // OBSOLETE Lengthen handshapes (arpeggios) 
                    /*if (chordName.Contains("LEN"))
                    {
                        Match match = Regex.Match(chordName, @"\d+s\d+$");
                        if (match.Success)
                        {
                            //float lenghtenAmount = match.Value.ToFloat();
                            float newEndTime = float.Parse(match.Value.Replace('s', '.'), NumberFormatInfo.InvariantInfo);

                            var handShapesToLenghten =
                                from hshape in DDCSong.Levels.SelectMany(lv => lv.HandShapes)
                                where hshape.ChordId == i
                                select hshape;

                            foreach (var handShape in handShapesToLenghten)
                            {
                                handShape.EndTime = newEndTime;
                            }

                            currentChordTemplate.ChordName = Regex.Replace(currentChordTemplate.ChordName, @"LEN\d+s\d+", string.Empty);
                            currentChordTemplate.DisplayName = Regex.Replace(currentChordTemplate.DisplayName, @"LEN\d+s\d+", string.Empty);

                            Log($"--Lengthened chord ID {i} handshape. New end time: {newEndTime}");
                        }
                        else
                        {
                            string errorMessage = $"Unable to read lengthen amount from chord {chordName}, Chord ID: {i}.";
                            Parent.StatusMessages.Add(new ImproverMessage(errorMessage, MessageType.Warning));
                            Log(errorMessage);
                        }
                    }*/

                    string oldChordName = currentChordTemplate.ChordName;
                    string oldDisplayName = currentChordTemplate.DisplayName;
                    string newChordName = oldChordName;
                    string newDisplayName = oldDisplayName;

                    if (oldChordName == " ")
                    {
                        newChordName = "";
                    }
                    else
                    {
                        if (oldChordName.Contains("min"))
                            newChordName = oldChordName.Replace("min", "m");
                        if (oldChordName.Contains("CONV"))
                            newChordName = oldChordName.Replace("CONV", "");
                        if (oldChordName.Contains("-nop"))
                            newChordName = oldChordName.Replace("-nop", "");
                        if (oldChordName.Contains("-arp"))
                            newChordName = oldChordName.Replace("-arp", "");
                    }

                    if (oldDisplayName == " ")
                    {
                        newDisplayName = "";
                    }
                    else
                    {
                        if (oldDisplayName.Contains("min"))
                            newDisplayName = oldDisplayName.Replace("min", "m");
                        if (oldDisplayName.Contains("CONV"))
                            newDisplayName = oldDisplayName.Replace("CONV", "-arp");
                    }

                    // Log message for changed chord names that are not empty
                    if (newChordName != oldChordName && newChordName.Length != 0 && !chordRenamed.ContainsKey(oldChordName))
                    {
                        if (oldChordName.Contains("CONV") || oldChordName.Contains("-arp"))
                            Log($"--Converted {newChordName} handshape into an arpeggio.");
                        else
                            Log($"--Renamed \"{oldChordName}\" to \"{newChordName}\"");

                        // Display renamed chords with the same name only once
                        chordRenamed[oldChordName] = true;
                    }

                    currentChordTemplate.ChordName = newChordName;
                    currentChordTemplate.DisplayName = newDisplayName;
                }
            }

            /// <summary>
            /// Removes beats that are past the end of the audio.
            /// </summary>
            private void RemoveBeatsPastAudioEnd()
            {
                var lastBeat = DDCSong.Ebeats[DDCSong.Ebeats.Count - 1];
                var penultimateBeat = DDCSong.Ebeats[DDCSong.Ebeats.Count - 2];
                float audioEnd = DDCSong.SongLength;
                float lastBeatTime = lastBeat.Time;
                int beatsRemoved = 0;
                bool first = true;

                while (lastBeatTime > audioEnd)
                {
                    // If the second-to-last beat is not past audio end, check which beat is closer to the end
                    if (penultimateBeat.Time < audioEnd)
                    {
                        // If the last beat is closer, keep it
                        if (audioEnd - penultimateBeat.Time > lastBeatTime - audioEnd)
                            break;
                    }

                    DDCSong.Ebeats.Remove(lastBeat);
                    beatsRemoved++;

                    if (first)
                    {
                        Log("Removing beats that are past audio end:");
                        first = false;
                    }

                    Log($"--Removed beat at {lastBeatTime.TimeToString()}.");

                    lastBeat = penultimateBeat;
                    lastBeatTime = lastBeat.Time;
                    penultimateBeat = DDCSong.Ebeats[DDCSong.Ebeats.Count - 2];
                }

                // Move the last beat to the time audio ends
                lastBeat.Time = audioEnd;
            }

            /// <summary>
            /// Removes TS events added by EOF.
            /// </summary>
            private void RemoveTimeSignatureEvents()
            {
                int eventsRemoved = DDCSong.Events.RemoveAll(ev => ev.Code.StartsWith("TS"));

                if (eventsRemoved > 0)
                {
                    Log($"{eventsRemoved} time signature event{(eventsRemoved == 1 ? "" : "s")} removed.");
                }
            }

            private void CheckPhraseIterationCount()
            {
                NewPhraseIterationCount = DDCSong.PhraseIterations.Count;

                if (NewPhraseIterationCount != OldPhraseIterationCount)
                {
                    Log($"PhraseIteration count does not match (Old: {OldPhraseIterationCount}, new: {NewPhraseIterationCount})");
                }
            }

            // Might remove phrases that DDC has moved.
            /*private void RemoveEmptyPhrasesAddedByDDC()
            {
                var phraseIterationsToRemove =
                    from ddcpi in DDCSong.PhraseIterations
                    where ddcpi.PhraseId != 0 // Ignore COUNT phrase
                        // Select phrases with no levels (DDC may create such phrases that have notes in them)
                        && DDCSong.Phrases[ddcpi.PhraseId].MaxDifficulty == 0
                        // Select phrase iterations not present in the original file
                        && !OriginalPhraseIterations.Any(p => Utils.TimeEqualToMilliseconds(p.Time, ddcpi.Time))
                    select ddcpi;

                int nPhrasesIterationsRemoved = phraseIterationsToRemove.Count();

                if (nPhrasesIterationsRemoved == 0)
                    return;

                foreach (var pIter in phraseIterationsToRemove)
                {
                    Log($"--Removed empty phrase added by DDC at {pIter.Time.TimeToString()}.");
                    DDCSong.PhraseIterations.Remove(pIter);
                }
            }*/

            #region One level phrase fix

            /// <summary>
            /// Adds a second difficulty level to phrases that have only one level.
            /// </summary>
            private void ProcessOneLevelPhrases()
            {
                Log("********** Begin one level phrase fix **********");

                // Skip first phrase (COUNT) and last phrase (END)
                for (int phraseID = 1; phraseID < DDCSong.Phrases.Count - 1; phraseID++)
                {
                    if (DDCSong.Phrases[phraseID].MaxDifficulty == 0)
                    {
                        Log($"--Phrase #{phraseID} ({DDCSong.Phrases[phraseID].Name}, {DDCSong.PhraseIterations.First(pi => pi.PhraseId == phraseID).Time.TimeToString()}) has only one level.");

                        var phraseIterations = from pi in DDCSong.PhraseIterations
                                               where pi.PhraseId == phraseID
                                               select pi;

                        foreach (var pi in phraseIterations)
                        {
                            float startTime = pi.Time;
                            float endTime = DDCSong.PhraseIterations[DDCSong.PhraseIterations.IndexOf(pi) + 1].Time;

                            var firstLevel = DDCSong.Levels[0];
                            var secondLevel = DDCSong.Levels[1];

                            var firstLevelNotes = (from note in firstLevel.Notes
                                                   where note.Time >= startTime && note.Time < endTime
                                                   select note).ToArray();

                            int notesInPhrase = firstLevelNotes.Length;

                            if (notesInPhrase == 0) // Phrase is a noguitar phrase
                            {
                                Log("Skipping empty phrase.");
                                break;
                            }

                            Log($"Phrase has {notesInPhrase} note{(notesInPhrase == 1 ? "" : "s")}.");

                            List<Note> harderLevelNotes = new List<Note>();

                            // Make copies of current notes that will be added to the harder difficulty level
                            foreach (var note in firstLevelNotes)
                            {
                                harderLevelNotes.Add(new Note(note));
                                //Log(note.ToString());
                            }

                            int notesRemoved = 0;

                            var removableSustainNotes =
                                firstLevelNotes.Where(note => note.Sustain != 0.0f
                                                              && !note.IsBend
                                                              && !note.IsVibrato
                                                              && !note.IsTremolo
                                                              && !note.IsLinkNext
                                                              && !note.IsSlide
                                                              && !note.IsUnpitchedSlide);

                            var firstLevelAnchors = firstLevel.Anchors;
                            var secondLevelAnchors = secondLevel.Anchors;

                            var anchorsToAddToSecondLevel =
                                    from anchor in firstLevelAnchors
                                    join note in firstLevelNotes on anchor.Time equals note.Time
                                    select anchor;

                            var newSecondLevelAnchors = new AnchorCollection();
                            newSecondLevelAnchors.AddRange(secondLevelAnchors.Union(anchorsToAddToSecondLevel).OrderBy(a => a.Time));

                            secondLevel.Anchors = newSecondLevelAnchors;

                            if (notesInPhrase > 1)
                            {
                                foreach (var note in firstLevelNotes)
                                {
                                    // Remove sliding notes and vibratos
                                    if (note.IsSlide || note.IsUnpitchedSlide || note.IsVibrato)
                                    {
                                        Log($"Removed note at time {note.Time.TimeToString()}");
                                        firstLevel.Notes.Remove(note);
                                        notesRemoved++;
                                    }
                                    else if (note.IsLinkNext) // Remove linkNext from the note
                                    {
                                        Log($"Removed linkNext from note at time {note.Time.TimeToString()}");
                                        note.IsLinkNext = false;

                                        if (note.Sustain < 0.5f && !note.IsBend) // Remove sustain if very short
                                        {
                                            note.Sustain = 0.0f;
                                            Log($"Removed sustain from note at time {note.Time.TimeToString()}");
                                        }
                                    }

                                    // If note has more than two bendValues, remove the rest
                                    if (note.IsBend && note.BendValues.Count > 2)
                                    {
                                        // Remove bendValues
                                        var secondBendValue = note.BendValues[1];
                                        note.BendValues.RemoveRange(2, note.BendValues.Count - 2);

                                        // Set sustain of the note to end at the second bendValue
                                        float newSustain = secondBendValue.Time - note.Time;
                                        note.Sustain = (float)Math.Round(newSustain, 3, MidpointRounding.AwayFromZero);

                                        Log($"Removed bendvalues and adjusted the sustain of note at {note.Time.TimeToString()}");
                                    }
                                }
                            }

                            if (notesRemoved == 0 && removableSustainNotes.Any())
                            {
                                foreach (var note in removableSustainNotes)
                                {
                                    note.Sustain = 0.0f;
                                }

                                Log($"Solution: Removed sustain from note{(notesInPhrase == 1 ? "" : "s")}.");
                            }
                            else if (notesRemoved == 0)
                            {
                                // TODO: One note phrase -> remove all techniques?
                                //if (Debugger.IsAttached)
                                //    Debugger.Break();

                                Log("Solution: Kept phrase as is.");
                            }

                            pi.HeroLevels = new HeroLevelCollection
                            {
                                new HeroLevel(hero: 1, difficulty: 0),
                                new HeroLevel(hero: 2, difficulty: 1),
                                new HeroLevel(hero: 3, difficulty: 1)
                            };

                            // Find correct place where to insert the notes in the second difficulty level
                            float lastNoteTime = harderLevelNotes.Last().Time;
                            int noteAfterIndex = secondLevel.Notes.FindIndex(n => n.Time > lastNoteTime);

                            if (noteAfterIndex == -1) // Add to the end
                                secondLevel.Notes.AddRange(harderLevelNotes);
                            else
                                secondLevel.Notes.InsertRange(noteAfterIndex, harderLevelNotes);

                            DDCSong.Phrases[phraseID].MaxDifficulty = 1;
                        }
                    }
                }
                Log("**********  End one level phrase fix  **********");
            }

            #endregion

            private void ValidateDDCXML()
            {
                if (DDCSong.Levels.SelectMany(lev => lev.HandShapes).Any(hs => hs.ChordId == -1))
                    throw new DDCException("DDC has created a handshape with an invalid chordId (-1).");

                // Check for DDC bug where muted notes with sustain are generated
                if (Parent.isNonDDFile)
                {
                    var notes = DDCSong.Levels.SelectMany(lev => lev.Notes);
                    var mutedNotesWithSustain =
                        from note in notes
                        where note.IsMute && note.Sustain > 0.0f
                        select note;

                    foreach (var note in mutedNotesWithSustain)
                    {
                        bool originallyHadMutedNote = Parent.originalSong.Levels
                            .SelectMany(lev => lev.Notes)
                            .Any(n => Utils.TimeEqualToMilliseconds(n.Time, note.Time) && n.IsMute);

                        bool originallyHadMutedChord = Parent.originalSong.Levels
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
