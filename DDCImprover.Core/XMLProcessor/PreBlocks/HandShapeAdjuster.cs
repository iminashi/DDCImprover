using Rocksmith2014Xml;
using System;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Shortens the lengths of a handshapes that are too close to the next one.
    /// </summary>
    internal sealed class HandShapeAdjuster : IProcessorBlock
    {
        public void Apply(RS2014Song song, Action<string> Log)
        {
            foreach (var level in song.Levels)
            {
                var handShapes = level.HandShapes;

                for (int i = 1; i < handShapes.Count; i++)
                {
                    float followingStartTime = handShapes[i].StartTime;
                    float followingEndTime = handShapes[i].EndTime;

                    var precedingHandshape = handShapes[i - 1];
                    float precedingStartTime = precedingHandshape.StartTime;
                    float precedingEndTime = precedingHandshape.EndTime;

                    // Ignore nested handshapes
                    if (precedingEndTime >= followingEndTime || followingStartTime - precedingEndTime < -0.001f)
                    {
                        Log($"Skipped nested handshape starting at {precedingStartTime.TimeToString()}.");
                        continue;
                    }

                    int beat1Index = song.Ebeats.FindIndex(b => b.Time > precedingEndTime);
                    var beat1 = song.Ebeats[beat1Index - 1];
                    var beat2 = song.Ebeats[beat1Index];

                    double note32nd = Math.Round((beat2.Time - beat1.Time) / 8, 3, MidpointRounding.AwayFromZero);
                    bool shortenBy16thNote = false;

                    // Check if chord that starts the handshape is a linknext slide
                    var startChord = level.Chords?.FindByTime(precedingStartTime);
                    if (startChord?.IsLinkNext == true && startChord?.ChordNotes?.Any(cn => cn.IsSlide) == true)
                    {
                        // Check if the handshape length is an 8th note or longer
                        if ((note32nd * 4) - (precedingEndTime - precedingStartTime) < 0.003)
                        {
                            shortenBy16thNote = true;
                        }
                    }

                    double minDistance = shortenBy16thNote ? note32nd * 2 : note32nd;

                    // Shorten the min. distance required for 32nd notes or smaller
                    if (precedingEndTime - precedingStartTime <= note32nd)
                        minDistance = Math.Round((beat2.Time - beat1.Time) / 12, 3, MidpointRounding.AwayFromZero);

                    double currentDistance = Math.Round(followingStartTime - precedingEndTime, 3);

                    if (currentDistance < minDistance)
                    {
                        double newEndTime = Math.Round(followingStartTime - minDistance, 3, MidpointRounding.AwayFromZero);
                        int safetyCount = 0;

                        // Shorten distance for very small note values
                        while (newEndTime <= precedingStartTime && ++safetyCount < 3)
                        {
                            minDistance = Math.Round(minDistance / 2, 3, MidpointRounding.AwayFromZero);
                            newEndTime = Math.Round(followingStartTime - minDistance, 3, MidpointRounding.AwayFromZero);
#if DEBUG
                            Log("Reduced handshape min. distance by half.");
#endif
                        }

                        handShapes[i - 1] = new HandShape(precedingHandshape.ChordId, precedingHandshape.StartTime, (float)newEndTime);

                        // Skip logging < 5ms adjustments
                        if (minDistance - currentDistance > 0.005)
                        {
                            string oldDistanceMs = ((int)(currentDistance * 1000)).ToString();
                            string newDistanceMs = ((int)(minDistance * 1000)).ToString();
                            var message = $"Adjusted the length of handshape starting at {precedingHandshape.StartTime.TimeToString()} (Distance: {oldDistanceMs}ms -> {newDistanceMs}ms)";
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
