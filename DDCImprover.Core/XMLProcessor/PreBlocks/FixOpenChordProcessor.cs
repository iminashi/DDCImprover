using Rocksmith2014Xml;
using System;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Processes the FIXOPEN chord command.
    /// </summary>
    internal sealed class FixOpenChordProcessor : IProcessorBlock
    {
        public void Apply(RS2014Song song, Action<string> Log)
        {
            var ChordTemplates = song.ChordTemplates;
            for (int chordId = 0; chordId < ChordTemplates.Count; chordId++)
            {
                var currentChordTemplate = ChordTemplates[chordId];
                string chordName = currentChordTemplate.ChordName;

                if (chordName.Contains("FIXOPEN"))
                {
                    // Remove open notes from the chord template
                    for (int i = 0; i < 6; ++i)
                    {
                        if (currentChordTemplate.Frets[i] == 0)
                            currentChordTemplate.Frets[i] = -1;
                    }

                    foreach (var level in song.Levels)
                    {
                        var chordsToFix = from chord in level.Chords
                                          where chord.ChordId == chordId
                                          select chord;

                        foreach (var chord in chordsToFix)
                        {
                            var chordNotesToRemove = chord.ChordNotes.Where(cn => cn.Fret == 0).ToArray();
                            // Store sustain of chord
                            double initSustain = chordNotesToRemove[0].Sustain;

                            foreach (var chordNote in chordNotesToRemove)
                            {
                                double sustain = initSustain;

                                var noteToRemove =
                                    (from note in level.Notes
                                     where note.Fret == 0
                                           && note.String == chordNote.String
                                           && note.Time > chord.Time
                                     select note).FirstOrDefault();

                                while (noteToRemove != null)
                                {
                                    sustain += noteToRemove.Sustain;

                                    if (noteToRemove.IsLinkNext)
                                    {
                                        level.Notes.Remove(noteToRemove);

                                        int nextIndex = level.Notes.FindIndex(n => n.Time > chordNote.Time && n.String == chordNote.String);
                                        noteToRemove = nextIndex == -1 ? null : level.Notes[nextIndex];
                                    }
                                    else
                                    {
                                        level.Notes.Remove(noteToRemove);
                                        break;
                                    }
                                }

                                // Add new note
                                var newNote = new Note(chordNote)
                                {
                                    Sustain = (float)Math.Round(sustain, 3, MidpointRounding.AwayFromZero),
                                    IsLinkNext = false
                                };

                                int insertIndex = level.Notes.FindIndex(n => n.Time > chord.Time);
                                if (insertIndex != -1)
                                {
                                    level.Notes.Insert(insertIndex, newNote);
                                }
                                else
                                {
                                    // Should not be able to get here
                                    level.Notes.Insert(0, newNote);
                                }

                                // Remove open notes from chord
                                chord.ChordNotes.Remove(chordNote);
                            }

                            // Move chord and handshape forward 20 ms (hack for DDC)
                            const float amountToMove = 0.020f;
                            var handShape = level.HandShapes.First(hs => Utils.TimeEqualToMilliseconds(hs.StartTime, chord.Time));
                            handShape.StartTime += amountToMove;
                            chord.Time += amountToMove;

                            foreach (var cn in chord.ChordNotes)
                            {
                                cn.Time += amountToMove;

                                // Reduce sustain by amount moved
                                if (cn.Sustain != 0.0f)
                                {
                                    cn.Sustain -= amountToMove;
                                }
                            }

                            Log($"Applied open note sustain fix to chord at {chord.Time.TimeToString()}");
                        }
                    }

                    // Set correct name
                    currentChordTemplate.ChordName = currentChordTemplate.ChordName.Replace("FIXOPEN", "");
                    currentChordTemplate.DisplayName = currentChordTemplate.DisplayName.Replace("FIXOPEN", "");
                }
            }
        }
    }
}
