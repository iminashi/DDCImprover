using Rocksmith2014.XML;

using System;
using System.Collections.Generic;
using System.Linq;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Removes notes with no sustain after LinkNext slides.
    /// </summary>
    internal sealed class AnchorPlaceholderNoteRemover : IProcessorBlock
    {
        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            int notesRemoved = 0;

            foreach (var level in arrangement.Levels)
            {
                var notesToRemove = new List<Note>();
                var notes = level.Notes;
                var chords = level.Chords;

                for (int i = 0; i < notes.Count; i++)
                {
                    Note note = notes[i];

                    if (note.IsLinkNext)
                    {
                        // Find the next note on the same string
                        int j = i + 1;
                        while (j < notes.Count && notes[j].String != note.String)
                        {
                            j++;
                        }

                        if (j == notes.Count)
                            break;

                        // Remove the note if it has no sustain
                        if (notes[j].Sustain == 0)
                        {
                            notesToRemove.Add(notes[j]);
                            note.IsLinkNext = false;
                        }
                    }
                }

                for (int i = 0; i < chords.Count; i++)
                {
                    Chord chord = chords[i];

                    if (chord.IsLinkNext && chord.ChordNotes.Any(n => n.IsSlide))
                    {
                        // Find the first note after the chord
                        int noteIndex = notes.FindIndex(n => n.Time > chord.Time);

                        if (noteIndex == -1)
                            continue;

                        // Get all the chord notes that have LinkNext slides
                        var chordNotes = chord.ChordNotes.Where(n => n.IsLinkNext && n.IsSlide).ToDictionary(n => n.String);
                        while (chordNotes.Count > 0 && noteIndex < notes.Count)
                        {
                            var note = notes[noteIndex];
                            sbyte nString = note.String;
                            if (chordNotes.ContainsKey(nString))
                            {
                                if (note.Sustain == 0)
                                {
                                    chordNotes[nString].IsLinkNext = false;
                                    notesToRemove.Add(note);
                                }
                                chordNotes.Remove(nString);
                            }
                            ++noteIndex;
                        }
                        chord.IsLinkNext = chord.ChordNotes.Any(n => n.IsLinkNext);
                    }
                }

                notesRemoved += notesToRemove.Count;
                foreach (var note in notesToRemove)
                {
                    notes.Remove(note);
                }
            }

            if (notesRemoved > 0)
                Log($"Removed {notesRemoved} anchor placeholder note{(notesRemoved == 1 ? "" : "s")}.");
        }
    }
}
