using Rocksmith2014Xml;

using System;
using System.Linq;

namespace DDCImprover.Core.PostBlocks
{
    internal sealed class FirstNoguitarSectionRestorer : IProcessorBlock
    {
        private readonly int _firstNGSectionTime;

        public FirstNoguitarSectionRestorer(int firstNGSectionTime)
        {
            _firstNGSectionTime = firstNGSectionTime;
        }

        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            // Increase the number attribute of any following noguitar sections
            foreach (var section in arrangement.Sections.Where(s => s.Name == "noguitar"))
            {
                ++section.Number;
            }

            // Add the removed noguitar section back
            var newFirstSection = new Section("noguitar", _firstNGSectionTime, 1);
            arrangement.Sections.Insert(0, newFirstSection);

            // Add a new NG phrase as the last phrase
            arrangement.Phrases.Add(new Phrase("NG", maxDifficulty: 0, PhraseMask.None));

            // Recreate the removed phrase iteration (with the phrase id of the new NG phrase)
            var newNGPhraseIteration = new PhraseIteration(_firstNGSectionTime, arrangement.Phrases.Count - 1);

            // Add after the first phraseIteration (COUNT)
            arrangement.PhraseIterations.Insert(1, newNGPhraseIteration);

            Log("Restored first noguitar section.");
        }
    }
}
