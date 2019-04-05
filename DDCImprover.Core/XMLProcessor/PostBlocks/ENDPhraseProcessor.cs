using Rocksmith2014Xml;
using System;
using System.Linq;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Checks if DDC has moved the END phrase and restores its original position.
    /// </summary>
    internal sealed class ENDPhraseProcessor : IProcessorBlock
    {
        private readonly float _oldLastPhraseTime;

        public ENDPhraseProcessor(float oldLastPhraseTime)
        {
            _oldLastPhraseTime = oldLastPhraseTime;
        }

        public void Apply(RS2014Song song, Action<string> Log)
        {
            int endPhraseId = song.Phrases.FindIndex(p => p.Name.Equals("END", StringComparison.OrdinalIgnoreCase));
            var endPhraseIter = song.PhraseIterations.First(pi => pi.PhraseId == endPhraseId);
            var newENDPhraseTime = endPhraseIter.Time;

            if (!Utils.TimeEqualToMilliseconds(newENDPhraseTime, _oldLastPhraseTime))
            {
                Log($"DDC has moved END phrase from {_oldLastPhraseTime.TimeToString()} to {newENDPhraseTime.TimeToString()}.");

                // Restore correct time to last section and phrase iteration
                if (XMLProcessor.Preferences.PreserveENDPhraseLocation)
                {
                    // Check if DDC has added an empty phrase to where we want to move the END phrase
                    var ddcAddedPhraseIteration = song.PhraseIterations.FirstOrDefault(pi => Utils.TimeEqualToMilliseconds(pi.Time, _oldLastPhraseTime));
                    if (ddcAddedPhraseIteration != null)
                    {
                        song.PhraseIterations.Remove(ddcAddedPhraseIteration);

                        Log($"--Removed phrase added by DDC at {_oldLastPhraseTime.TimeToString()}.");
                    }

                    endPhraseIter.Time = _oldLastPhraseTime;
                    song.Sections.Last().Time = _oldLastPhraseTime;

                    Log("--Restored END phrase location.");
                }
            }
        }
    }
}
