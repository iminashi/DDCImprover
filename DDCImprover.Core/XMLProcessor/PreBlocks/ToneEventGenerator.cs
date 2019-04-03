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
        public void Apply(RS2014Song song, Action<string> Log)
        {
            var toneChanges = song.Tones;

            if (toneChanges is null || toneChanges.Count == 0 || song.Events.Any(ev => ev.Code.StartsWith("tone_")))
                return;

            var toneCodes = new Dictionary<string, string>();
            if (song.ToneA != null)
                toneCodes.Add(song.ToneA, "tone_a");
            if (song.ToneB != null)
                toneCodes.Add(song.ToneB, "tone_b");
            if (song.ToneC != null)
                toneCodes.Add(song.ToneC, "tone_c");
            if (song.ToneD != null)
                toneCodes.Add(song.ToneD, "tone_d");

            var toneEvents = from tone in toneChanges
                             select new Event(toneCodes[tone.Name], tone.Time);

            var events = song.Events;
            var newEvents = events.Union(toneEvents).OrderBy(e => e.Time).ToArray();

            song.Events.Clear();
            song.Events.AddRange(newEvents);

            Log("Generated tone events.");
        }
    }
}
