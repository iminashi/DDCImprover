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
        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            var ChordTemplates = arrangement.ChordTemplates;
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

                    foreach (var level in arrangement.Levels)
                    {
                        var chordsToFix = from chord in level.Chords
                                          where chord.ChordId == chordId
                                          select chord;

                        foreach (var chord in chordsToFix)
                        {
                            if (chord.ChordNotes is null)
                            {
                                Log("ERROR: FIXOPEN chord does not have chord notes at " + chord.Time.TimeToString());
                                continue;
                            }

                            var chordNotesToRemove = chord.ChordNotes.Where(cn => cn.Fret == 0).ToArray();
                            // Store sustain of chord
                            int initSustain = chordNotesToRemove[0].Sustain;

                            foreach (var chordNote in chordNotesToRemove)
                            {
                                int sustain = initSustain;

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
                                    Sustain = sustain,
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
                            const int amountToMove = 20;
                            var handShape = level.HandShapes.First(hs => hs.StartTime == chord.Time);
                            handShape.StartTime += amountToMove;
                            chord.Time += amountToMove;

                            foreach (var cn in chord.ChordNotes)
                            {
                                cn.Time += amountToMove;

                                // Reduce sustain by amount moved
                                if (cn.Sustain >= amountToMove)
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
