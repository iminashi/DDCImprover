using System;
using System.Linq;
using Rocksmith2014Xml;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Removes DDC's noguitar phrase if no phrase iterations using it are found.
    /// </summary>
    internal sealed class UnnecessaryNGPhraseRemover : IProcessorBlock
    {
        public void Apply(RS2014Song song, Action<string> Log)
        {
            const int ngPhraseId = 1;

            if (song.Phrases[ngPhraseId].MaxDifficulty != 0)
                return;

            var ngPhrasesIterations = from pi in song.PhraseIterations
                                      where pi.PhraseId == ngPhraseId
                                      select pi;

            if (!ngPhrasesIterations.Any())
            {
                Log("Removed unnecessary noguitar phrase.");

                song.Phrases.RemoveAt(ngPhraseId);

                // Set correct phrase IDs for phrase iterations
                foreach (var pi in song.PhraseIterations)
                {
                    if (pi.PhraseId > ngPhraseId)
                    {
                        --pi.PhraseId;
                    }
                }

                // Set correct phrase IDs for NLDs
                foreach (var nld in song.NewLinkedDiffs)
                {
                    for (int i = 0; i < nld.PhraseCount; i++)
                    {
                        if (nld.PhraseIds[i] > ngPhraseId)
                        {
                            nld.PhraseIds[i]--;
                        }
                    }
                }
            }
        }
    }
}
