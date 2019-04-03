using Rocksmith2014Xml;
using System;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Enables 'unpitchedSlides' in arrangement properties if unpitched slides found.
    /// </summary>
    internal sealed class UnpitchedSlideChecker : IProcessorBlock
    {
        public void Apply(RS2014Song song, Action<string> Log)
        {
            if (song.ArrangementProperties.UnpitchedSlides == 1)
                return;

            // TODO: Could optimize by looking at hardest level only
            var notes = song.Levels.SelectMany(l => l.Notes);

            if (notes.Any(n => n.IsUnpitchedSlide))
            {
                song.ArrangementProperties.UnpitchedSlides = 1;
            }
            else
            {
                foreach (var chord in song.Levels.SelectMany(l => l.Chords))
                {
                    if (chord.ChordNotes is null)
                        continue;

                    if (chord.ChordNotes.Any(n => n.IsUnpitchedSlide))
                    {
                        song.ArrangementProperties.UnpitchedSlides = 1;

                        break;
                    }
                }
            }

            if (song.ArrangementProperties.UnpitchedSlides == 1)
                Log("Enabled unpitched slides in arrangement properties.");
        }
    }
}
