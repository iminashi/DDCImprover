using Rocksmith2014Xml;
using System;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Processes custom events. Available:
    /// "removebeats", "w3"
    /// </summary>
    internal sealed class CustomEventsPreProcessor : IProcessorBlock
    {
        public void Apply(RS2014Song song, Action<string> Log)
        {
            var events = song.Events;

            var removeBeatsEvent = events.FirstOrDefault(ev => ev.Code.Equals("removebeats", StringComparison.OrdinalIgnoreCase));
            if (removeBeatsEvent != null)
            {
                song.Ebeats.RemoveAll(b => b.Time >= removeBeatsEvent.Time);

                Log($"removebeats event found: Removed beats from {removeBeatsEvent.Time.TimeToString()} onward.");

                events.Remove(removeBeatsEvent);
            }

            var width3events = events.Where(ev => ev.Code.Equals("w3", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var w3event in width3events)
            {
                var modifiedAnchors = song.Levels
                    .SelectMany(lvl => lvl.Anchors)
                    .Where(a => Utils.TimeEqualToMilliseconds(a.Time, w3event.Time));

                foreach (var anchor in modifiedAnchors)
                {
                    anchor.Width = 3.0f;
                    Log($"Changed width of anchor at {anchor.Time.TimeToString()} to 3.");
                }

                events.Remove(w3event);
            }
        }
    }
}
