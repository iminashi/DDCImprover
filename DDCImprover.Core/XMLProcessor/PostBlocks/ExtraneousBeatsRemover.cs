using Rocksmith2014Xml;
using System;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Removes beats that are past the end of the audio.
    /// </summary>
    internal sealed class ExtraneousBeatsRemover : IProcessorBlock
    {
        public void Apply(RS2014Song song, Action<string> Log)
        {
            var lastBeat = song.Ebeats[song.Ebeats.Count - 1];
            var penultimateBeat = song.Ebeats[song.Ebeats.Count - 2];
            float audioEnd = song.SongLength;
            float lastBeatTime = lastBeat.Time;
            int beatsRemoved = 0;
            bool first = true;

            if (lastBeatTime < audioEnd)
                return;

            while (lastBeatTime > audioEnd)
            {
                // If the second-to-last beat is not past audio end, check which beat is closer to the end
                if (penultimateBeat.Time < audioEnd)
                {
                    // If the last beat is closer, keep it
                    if (audioEnd - penultimateBeat.Time > lastBeatTime - audioEnd)
                        break;
                }

                song.Ebeats.Remove(lastBeat);
                beatsRemoved++;

                if (first)
                {
                    Log("Removing beats that are past audio end:");
                    first = false;
                }

                Log($"--Removed beat at {lastBeatTime.TimeToString()}.");

                lastBeat = penultimateBeat;
                lastBeatTime = lastBeat.Time;
                penultimateBeat = song.Ebeats[song.Ebeats.Count - 2];
            }

            // Move the last beat to the time audio ends
            lastBeat.Time = audioEnd;
        }
    }
}
