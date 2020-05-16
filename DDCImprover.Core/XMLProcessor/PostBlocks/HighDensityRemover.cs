using Rocksmith2014Xml;
using System;
using System.Linq;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Removes chord notes and "high density" statuses from all chords that have it.
    /// </summary>
    internal sealed class HighDensityRemover : IProcessorBlock
    {
        public void Apply(RS2014Song song, Action<string> Log)
        {
            int hiDensRemoved = 0;

            void removeHighDensity(Chord chord, bool removeChordNotes)
            {
                if(chord.IsHighDensity)
                {
                    chord.IsHighDensity = false;
                    if(removeChordNotes)
                        chord.ChordNotes = null;
                    hiDensRemoved++;
                }
            }

            // Make sure that the version of the XML file is 8
            song.Version = 8;

            foreach (var level in song.Levels)
            {
                foreach (var hs in level.HandShapes)
                {
                    var chordsInHs =
                        from chord in level.Chords
                        where chord.Time >= hs.StartTime && chord.Time < hs.EndTime
                        select chord;

                    bool startsWithMute = false;
                    int chordNum = 1;

                    foreach (var chord in chordsInHs)
                    {
                        // If the handshape starts with a fret hand mute, we need to be careful
                        if (chordNum == 1 && chord.IsFretHandMute)
                        {
                            startsWithMute = true;
                            // Frethand-muted chords without techniques should not have chord notes
                            if (chord.ChordNotes?.All(cn => cn.Sustain == 0f) == true)
                                chord.ChordNotes = null;
                        }

                        if (startsWithMute && !chord.IsFretHandMute)
                        {
                            // Do not remove the chord notes
                            removeHighDensity(chord, false);
                            startsWithMute = false;
                        } else
                        {
                            removeHighDensity(chord, true);
                        }

                        chordNum++;
                    }
                }
            }

            if (hiDensRemoved > 0)
                Log($"{hiDensRemoved} high density statuses removed.");
        }
    }
}
