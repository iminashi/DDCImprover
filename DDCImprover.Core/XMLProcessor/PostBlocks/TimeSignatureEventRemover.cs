using System;
using System.Collections.Generic;
using System.Text;
using Rocksmith2014Xml;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Removes TS events added by EOF.
    /// </summary>
    internal sealed class TimeSignatureEventRemover : IProcessorBlock
    {
        public void Apply(RS2014Song song, Action<string> Log)
        {
            int eventsRemoved = song.Events.RemoveAll(ev => ev.Code.StartsWith("TS"));

            if (eventsRemoved > 0)
            {
                Log($"{eventsRemoved} time signature event{(eventsRemoved == 1 ? "" : "s")} removed.");
            }
        }
    }
}
