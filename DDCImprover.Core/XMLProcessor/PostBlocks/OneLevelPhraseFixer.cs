using Rocksmith2014Xml;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DDCImprover.Core.PostBlocks
{
    internal sealed class OneLevelPhraseFixer : IProcessorBlock
    {
        /// <summary>
        /// Adds a second difficulty level to phrases that have only one level.
        /// </summary>
        public void Apply(RS2014Song song, Action<string> Log)
        {
            Log("********** Begin one level phrase fix **********");

            // Skip first phrase (COUNT) and last phrase (END)
            for (int phraseID = 1; phraseID < song.Phrases.Count - 1; phraseID++)
            {
                if (song.Phrases[phraseID].MaxDifficulty == 0)
                {
                    Log($"--Phrase #{phraseID} ({song.Phrases[phraseID].Name}, {song.PhraseIterations.First(pi => pi.PhraseId == phraseID).Time.TimeToString()}) has only one level.");

                    var phraseIterations = from pi in song.PhraseIterations
                                           where pi.PhraseId == phraseID
                                           select pi;

                    foreach (var pi in phraseIterations)
                    {
                        float startTime = pi.Time;
                        float endTime = song.PhraseIterations[song.PhraseIterations.IndexOf(pi) + 1].Time;

                        var firstLevel = song.Levels[0];
                        var secondLevel = song.Levels[1];

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

                        // Make copies of current notes that will be added to the harder difficulty level
                        var harderLevelNotes = new List<Note>(from note in firstLevelNotes select new Note(note));

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

                        song.Phrases[phraseID].MaxDifficulty = 1;
                    }
                }
            }
            Log("**********  End one level phrase fix  **********");
        }
    }
}
