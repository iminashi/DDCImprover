using Rocksmith2014.XML;

using System;
using System.Linq;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Checks if DDC has moved the END phrase and restores its original position.
    /// </summary>
    internal sealed class ENDPhraseProcessor : IProcessorBlock
    {
        private readonly int _oldLastPhraseTime;

        public ENDPhraseProcessor(int oldLastPhraseTime)
        {
            _oldLastPhraseTime = oldLastPhraseTime;
        }

        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            int endPhraseId = arrangement.Phrases.FindIndex(p => p.Name.Equals("END", StringComparison.OrdinalIgnoreCase));
            var endPhraseIter = arrangement.PhraseIterations.First(pi => pi.PhraseId == endPhraseId);
            int newEndPhraseTime = endPhraseIter.Time;

            if (newEndPhraseTime != _oldLastPhraseTime)
            {
                Log($"DDC has moved END phrase from {_oldLastPhraseTime.TimeToString()} to {newEndPhraseTime.TimeToString()}.");

                // Restore correct time to last section and phrase iteration
                if (XMLProcessor.Preferences.PreserveENDPhraseLocation)
                {
                    // Check if DDC has added an empty phrase to where we want to move the END phrase
                    var ddcAddedPhraseIteration = arrangement.PhraseIterations.Find(pi => pi.Time == _oldLastPhraseTime);
                    if (ddcAddedPhraseIteration is not null)
                    {
                        arrangement.PhraseIterations.Remove(ddcAddedPhraseIteration);

                        Log($"--Removed phrase added by DDC at {_oldLastPhraseTime.TimeToString()}.");
                    }

                    endPhraseIter.Time = _oldLastPhraseTime;
                    arrangement.Sections.Last().Time = _oldLastPhraseTime;

                    Log("--Restored END phrase location.");
                }
            }
        }
    }
}
