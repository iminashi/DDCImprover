using Rocksmith2014Xml;
using System;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Shortens handshapes of chords that EOF has set to include the slide-to notes.
    /// </summary>
    internal sealed class EOFChordSlideHandshapeLengthFix : IProcessorBlock
    {
        public void Apply(RS2014Song song, Action<string> Log)
        {
            foreach (var level in song.Levels)
            {
                foreach (var chord in level.Chords.Where(c => c.IsLinkNext))
                {
                    if (chord.ChordNotes.Any(cn => cn.IsSlide))
                    {
                        var handshape = level.HandShapes.FirstOrDefault(hs => Utils.TimeEqualToMilliseconds(hs.StartTime, chord.Time));
                        if (!(handshape is null) && !(chord.ChordNotes is null)
                            && (handshape.EndTime > handshape.StartTime + chord.ChordNotes[0].Sustain))
                        {
                            handshape.EndTime = (float)Math.Round(handshape.StartTime + chord.ChordNotes[0].Sustain, 3, MidpointRounding.AwayFromZero);
                            Log($"Adjusted handshape length for chord slide at {chord.Time.TimeToString()}");
                        }
                    }
                }
            }
        }
    }
}
