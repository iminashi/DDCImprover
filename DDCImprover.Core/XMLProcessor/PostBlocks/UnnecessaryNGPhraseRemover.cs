using Rocksmith2014.XML;

using System;
using System.Linq;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Removes DDC's noguitar phrase if no phrase iterations using it are found.
    /// </summary>
    internal sealed class UnnecessaryNGPhraseRemover : IProcessorBlock
    {
        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            const int ngPhraseId = 1;

            if (arrangement.Phrases[ngPhraseId].MaxDifficulty != 0)
                return;

            var ngPhrasesIterations = from pi in arrangement.PhraseIterations
                                      where pi.PhraseId == ngPhraseId
                                      select pi;

            if (!ngPhrasesIterations.Any())
            {
                Log("Removed unnecessary noguitar phrase.");

                arrangement.Phrases.RemoveAt(ngPhraseId);

                // Set correct phrase IDs for phrase iterations
                foreach (var pi in arrangement.PhraseIterations)
                {
                    if (pi.PhraseId > ngPhraseId)
                    {
                        --pi.PhraseId;
                    }
                }

                // Set correct phrase IDs for NLDs
                foreach (var nld in arrangement.NewLinkedDiffs)
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
