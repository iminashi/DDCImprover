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
            var toneChanges = arrangement.Tones.Changes;

            if (toneChanges is null || toneChanges.Count == 0 || arrangement.Events.Any(ev => ev.Code.StartsWith("tone_")))
                return;

            var toneCodes = new Dictionary<string, string>();
            for (int i = 0; i < arrangement.Tones.Names.Length; i++)
            {
                var t = arrangement.Tones.Names[i];
                if (!string.IsNullOrEmpty(t))
                {
                    toneCodes.Add(t, "tone_" + (char)('a' + i));
                }
            }

            var toneEvents = from tone in toneChanges.Where(t => toneCodes.ContainsKey(t.Name))
                             select new Event(toneCodes[tone.Name], tone.Time);

            arrangement.Events =
                arrangement.Events
                .Union(toneEvents)
                .OrderBy(e => e.Time)
                .ToList();

            Log("Generated tone events.");
        }
    }
}
