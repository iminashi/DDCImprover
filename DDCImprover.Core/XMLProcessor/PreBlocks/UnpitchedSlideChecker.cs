using Rocksmith2014.XML;

using System;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Enables "unpitched slides" in arrangement properties if unpitched slides exist in the arrangement.
    /// </summary>
    internal sealed class UnpitchedSlideChecker : IProcessorBlock
    {
        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            if (arrangement.MetaData.ArrangementProperties.UnpitchedSlides)
                return;

            // TODO: Could optimize by looking at hardest level only
            var notes = arrangement.Levels.SelectMany(l => l.Notes);

            if (notes.Any(n => n.IsUnpitchedSlide))
            {
                arrangement.MetaData.ArrangementProperties.UnpitchedSlides = true;
            }
            else
            {
                foreach (var chord in arrangement.Levels.SelectMany(l => l.Chords))
                {
                    if (chord.ChordNotes is null)
                        continue;

                    if (chord.ChordNotes.Any(n => n.IsUnpitchedSlide))
                    {
                        arrangement.MetaData.ArrangementProperties.UnpitchedSlides = true;

                        break;
                    }
                }
            }

            if (arrangement.MetaData.ArrangementProperties.UnpitchedSlides)
                Log("Enabled unpitched slides in arrangement properties.");
        }
    }
}
