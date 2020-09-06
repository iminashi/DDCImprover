using Rocksmith2014.XML;

using System;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Processes custom events. Available:
    /// "w3"
    /// </summary>
    internal sealed class CustomEventsPreProcessor : IProcessorBlock
    {
        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            var events = arrangement.Events;

            var width3events = events.Where(ev => ev.Code.Equals("w3", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var w3event in width3events)
            {
                var modifiedAnchors = arrangement.Levels
                    .SelectMany(lvl => lvl.Anchors)
                    .Where(a => a.Time == w3event.Time);

                foreach (var anchor in modifiedAnchors)
                {
                    anchor.Width = 3;
                    Log($"Changed width of anchor at {anchor.Time.TimeToString()} to 3.");
                }

                events.Remove(w3event);
            }
        }
    }
}
