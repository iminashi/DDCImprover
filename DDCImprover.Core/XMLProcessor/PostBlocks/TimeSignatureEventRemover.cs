using Rocksmith2014Xml;

using System;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Removes TS events added by EOF.
    /// </summary>
    internal sealed class TimeSignatureEventRemover : IProcessorBlock
    {
        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            int eventsRemoved = arrangement.Events.RemoveAll(ev => ev.Code.StartsWith("TS"));

            if (eventsRemoved > 0)
            {
                Log($"{eventsRemoved} time signature event{(eventsRemoved == 1 ? "" : "s")} removed.");
            }
        }
    }
}
