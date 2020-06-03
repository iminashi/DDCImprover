using Rocksmith2014Xml;

using System;
using System.Collections.Generic;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Generates (pointless) tone events (tone_a etc.) for tone changes.
    /// </summary>
    internal sealed class ToneEventGenerator : IProcessorBlock
    {
        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            var toneChanges = arrangement.ToneChanges;

            if (toneChanges is null || toneChanges.Count == 0 || arrangement.Events.Any(ev => ev.Code.StartsWith("tone_")))
                return;

            var toneCodes = new Dictionary<string, string>();
            if (arrangement.ToneA != null) toneCodes.Add(arrangement.ToneA, "tone_a");
            if (arrangement.ToneB != null) toneCodes.Add(arrangement.ToneB, "tone_b");
            if (arrangement.ToneC != null) toneCodes.Add(arrangement.ToneC, "tone_c");
            if (arrangement.ToneD != null) toneCodes.Add(arrangement.ToneD, "tone_d");

            var toneEvents = from tone in toneChanges.Where(t => toneCodes.ContainsKey(t.Name))
                             select new Event(toneCodes[tone.Name], tone.Time);

            var events = arrangement.Events;
            var newEvents = events.Union(toneEvents).OrderBy(e => e.Time).ToArray();

            arrangement.Events.Clear();
            arrangement.Events.AddRange(newEvents);

            Log("Generated tone events.");
        }
    }
}
