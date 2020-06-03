using Rocksmith2014Xml;

using System;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Removes beats that are past the end of the audio.
    /// </summary>
    internal sealed class ExtraneousBeatsRemover : IProcessorBlock
    {
        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            var lastBeat = arrangement.Ebeats[^1];
            var penultimateBeat = arrangement.Ebeats[^2];
            int audioEnd = arrangement.SongLength;
            int lastBeatTime = lastBeat.Time;
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

                arrangement.Ebeats.Remove(lastBeat);

                if (first)
                {
                    Log("Removing beats that are past audio end:");
                    first = false;
                }

                Log($"--Removed beat at {lastBeatTime.TimeToString()}.");

                lastBeat = penultimateBeat;
                lastBeatTime = lastBeat.Time;
                penultimateBeat = arrangement.Ebeats[^2];
            }

            // Move the last beat to the time audio ends
            lastBeat.Time = audioEnd;
        }
    }
}
