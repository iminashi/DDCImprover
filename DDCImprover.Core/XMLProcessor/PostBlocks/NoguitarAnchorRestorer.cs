using Rocksmith2014.XML;
using Rocksmith2014.XML.Extensions;

using System;
using System.Collections.Generic;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Restores anchors at the beginning of noguitar sections.
    /// </summary>
    internal sealed class NoguitarAnchorRestorer : IProcessorBlock
    {
        private readonly IList<Anchor> _ngAnchors;

        public NoguitarAnchorRestorer(IList<Anchor> ngAnchors)
        {
            _ngAnchors = ngAnchors;
        }

        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            Log("Restoring noguitar section anchors:");

            var firstLevelAnchors = arrangement.Levels[0].Anchors;

            foreach (Anchor anchor in _ngAnchors)
            {
                // Add the anchor to the first difficulty level
                if (firstLevelAnchors.FindIndexByTime(anchor.Time) == -1)
                {
                    firstLevelAnchors.InsertByTime(anchor);
                }

                Log($"--Restored anchor at time {anchor.Time.TimeToString()}");
            }
        }
    }
}
