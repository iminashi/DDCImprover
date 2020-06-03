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
        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            int hiDensRemoved = 0;

            void removeHighDensity(Chord chord, bool removeChordNotes)
            {
                if (chord.IsHighDensity)
                {
                    chord.IsHighDensity = false;
                    if (removeChordNotes)
                    {
                        // Set the chord as ignored if it has any harmonics in it
                        if (chord.ChordNotes?.Count > 0 && chord.ChordNotes.Any(cn => cn.IsHarmonic))
                            chord.IsIgnore = true;

                        chord.ChordNotes = null;
                    }
                    hiDensRemoved++;
                }
            }

            // Make sure that the version of the XML file is 8
            arrangement.Version = 8;

            foreach (var level in arrangement.Levels)
            {
                foreach (var hs in level.HandShapes)
                {
                    var chordsInHs =
                        from chord in level.Chords
                        where chord.Time >= hs.StartTime && chord.Time < hs.EndTime
                        select chord;

                    bool startsWithMute = false;
                    int chordNum = 0;

                    foreach (var chord in chordsInHs)
                    {
                        chordNum++;

                        // If the handshape starts with a fret hand mute, we need to be careful
                        if (chordNum == 1 && chord.IsFretHandMute)
                        {
                            startsWithMute = true;
                            // Frethand-muted chords without techniques should not have chord notes
                            if (chord.ChordNotes?.All(cn => cn.Sustain == 0) == true)
                                chord.ChordNotes = null;
                        }
                        else if (chordNum == 1)
                        {
                            // Do not remove the chord notes even if the first chord somehow has "high density"
                            removeHighDensity(chord, false);
                            continue;
                        }

                        if (startsWithMute && !chord.IsFretHandMute)
                        {
                            // Do not remove the chord notes on the first non-muted chord after muted chord(s)
                            removeHighDensity(chord, false);
                            startsWithMute = false;
                        }
                        else
                        {
                            removeHighDensity(chord, true);
                        }
                    }
                }
            }

            if (hiDensRemoved > 0)
                Log($"{hiDensRemoved} high density statuses removed.");
        }
    }
}
