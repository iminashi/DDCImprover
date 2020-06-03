using Rocksmith2014Xml;
using Rocksmith2014Xml.Extensions;

using System;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Shortens the lengths of a handshapes that are too close to the next one.
    /// </summary>
    internal sealed class HandShapeAdjuster : IProcessorBlock
    {
        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            foreach (var level in arrangement.Levels)
            {
                var handShapes = level.HandShapes;

                for (int i = 1; i < handShapes.Count; i++)
                {
                    uint followingStartTime = handShapes[i].StartTime;
                    uint followingEndTime = handShapes[i].EndTime;

                    var precedingHandshape = handShapes[i - 1];
                    uint precedingStartTime = precedingHandshape.StartTime;
                    uint precedingEndTime = precedingHandshape.EndTime;

                    // Ignore nested handshapes
                    if (precedingEndTime >= followingEndTime)
                    {
                        Log($"Skipped nested handshape starting at {precedingStartTime.TimeToString()}.");
                        continue;
                    }

                    int beat1Index = arrangement.Ebeats.FindIndex(b => b.Time > precedingEndTime);
                    var beat1 = arrangement.Ebeats[beat1Index - 1];
                    var beat2 = arrangement.Ebeats[beat1Index];

                    uint note32nd = (beat2.Time - beat1.Time) / 8;
                    bool shortenBy16thNote = false;

                    // Check if the chord that starts the handshape is a LinkNext slide
                    var startChord = level.Chords?.FindByTime(precedingStartTime);
                    if (startChord?.IsLinkNext == true && startChord?.ChordNotes?.Any(cn => cn.IsSlide) == true)
                    {
                        // Check if the handshape length is an 8th note or longer
                        if (precedingEndTime - precedingStartTime > note32nd * 4)
                        {
                            shortenBy16thNote = true;
                        }
                    }

                    uint minDistance = shortenBy16thNote ? note32nd * 2 : note32nd;

                    // Shorten the min. distance required for 32nd notes or smaller
                    if (precedingEndTime - precedingStartTime <= note32nd)
                        minDistance = (beat2.Time - beat1.Time) / 12;

                    // The following handshape might begin before the preceding one ends (floating point rounding errors?)
                    uint currentDistance = (followingStartTime < precedingEndTime) ? 0 : followingStartTime - precedingEndTime;

                    if (currentDistance < minDistance)
                    {
                        uint newEndTime = followingStartTime - minDistance;
                        int safetyCount = 0;

                        // Shorten the distance for very small note values
                        while (newEndTime <= precedingStartTime && ++safetyCount < 3)
                        {
                            minDistance /= 2;
                            newEndTime = followingStartTime - minDistance;
#if DEBUG
                            Log("Reduced handshape min. distance by half.");
#endif
                        }

                        handShapes[i - 1].EndTime = newEndTime;

                        // Skip logging < 5ms adjustments
                        if (minDistance - currentDistance > 5)
                        {
                            var message = $"Adjusted the length of handshape starting at {precedingHandshape.StartTime.TimeToString()} (Distance: {currentDistance}ms -> {minDistance}ms)";
                            if (shortenBy16thNote)
                                message += " (Chord slide)";

                            Log(message);
                        }
                    }
                }
            }
        }
    }
}
