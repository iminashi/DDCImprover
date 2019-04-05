using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rocksmith2014Xml;

namespace DDCImprover.Core.PostBlocks
{
    internal sealed class FirstNoguitarSectionRestorer : IProcessorBlock
    {
        private readonly float _firstNGSectionTime;

        public FirstNoguitarSectionRestorer(float firstNGSectionTime)
        {
            _firstNGSectionTime = firstNGSectionTime;
        }

        public void Apply(RS2014Song song, Action<string> Log)
        {
            // Increase the number attribute of any following noguitar sections
            foreach (var section in song.Sections.Where(s => s.Name == "noguitar"))
            {
                ++section.Number;
            }

            // Add removed noguitar section back
            var newFirstSection = new Section("noguitar", _firstNGSectionTime, 1);
            song.Sections.Insert(0, newFirstSection);

            // Add a new NG phrase as the last phrase
            var newNGPhrase = new Phrase("NG", maxDifficulty: 0, PhraseMask.None);

            song.Phrases.Add(newNGPhrase);

            // Recreate removed phrase iteration (with phraseId of new NG phrase)
            var newNGPhraseIteration = new PhraseIteration
            {
                Time = _firstNGSectionTime,
                PhraseId = song.Phrases.Count - 1,
                Variation = string.Empty
            };

            // Add after the first phraseIteration (COUNT)
            song.PhraseIterations.Insert(1, newNGPhraseIteration);

            Log("Restored first noguitar section.");
        }
    }
}
