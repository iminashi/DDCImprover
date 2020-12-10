using Rocksmith2014.XML;

using System;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Shortens handshapes of chords that EOF has set to include the slide-to notes.
    /// </summary>
    internal sealed class EOFChordSlideHandshapeLengthFix : IProcessorBlock
    {
        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            foreach (var level in arrangement.Levels)
            {
                foreach (var chord in level.Chords.Where(c => c.IsLinkNext))
                {
                    if (chord.ChordNotes is not null && chord.ChordNotes.Any(cn => cn.IsSlide))
                    {
                        var handshape = level.HandShapes.Find(hs => hs.StartTime == chord.Time);
                        if (handshape is not null && (handshape.EndTime > handshape.StartTime + chord.ChordNotes[0].Sustain))
                        {
                            handshape.EndTime = handshape.StartTime + chord.ChordNotes[0].Sustain;
                            Log($"Adjusted handshape length for chord slide at {chord.Time.TimeToString()}");
                        }
                    }
                }
            }
        }
    }
}
