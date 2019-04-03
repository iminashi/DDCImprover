using Rocksmith2014Xml;
using System;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Enables LinkNext on any chord that has chordnotes with LinkNext.
    /// </summary>
    internal sealed class EOFLinkNextChordTechNoteFix : IProcessorBlock
    {
        public void Apply(RS2014Song song, Action<string> Log)
        {
            foreach (var chord in song.Levels.SelectMany(l => l.Chords))
            {
                if (chord.ChordNotes is null)
                    continue;

                if (chord.ChordNotes.Any(cn => cn.IsLinkNext) && !chord.IsLinkNext)
                {
                    chord.IsLinkNext = true;
                    Log($"Added LinkNext to chord at {chord.Time.TimeToString()}");
                }
            }
        }
    }
}
