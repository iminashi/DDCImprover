using Rocksmith2014.XML;

using System;
using System.Collections.Generic;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Removes temporary beats used for phrase moving.
    /// </summary>
    internal sealed class TemporaryBeatRemover : IProcessorBlock
    {
        private readonly IList<Ebeat> _addedBeats;

        public TemporaryBeatRemover(IList<Ebeat> addedBeats)
        {
            _addedBeats = addedBeats;
        }

        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            foreach (var addedBeat in _addedBeats)
            {
                var beatToRemove = arrangement.Ebeats.Find(beat => beat == addedBeat);
                if (beatToRemove is not null)
                {
                    arrangement.Ebeats.Remove(beatToRemove);
                }
                else
                {
                    throw new Exception("Could not find the added beat in the arrangement's beat list.");
                }
            }
        }
    }
}
