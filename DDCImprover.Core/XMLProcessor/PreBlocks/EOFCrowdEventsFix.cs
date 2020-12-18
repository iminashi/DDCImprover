using Rocksmith2014.XML;

using System;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Changes incorrect crowd event codes to lower case.
    /// </summary>
    internal sealed class EOFCrowdEventsFix : IProcessorBlock
    {
        private static readonly string[] wrongCrowdEvents = { "E0", "E1", "E2" };

        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            foreach (var @event in arrangement.Events)
            {
                if (wrongCrowdEvents.Contains(@event.Code))
                {
                    string correctEvent = @event.Code.ToLowerInvariant();
                    Log($"Corrected wrong crowd event: {@event.Code} -> {correctEvent}");

                    @event.Code = correctEvent;
                }
            }
        }
    }
}
