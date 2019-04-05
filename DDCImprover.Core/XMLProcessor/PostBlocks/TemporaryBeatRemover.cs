using System;
using System.Collections.Generic;
using System.Text;
using Rocksmith2014Xml;

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

        public void Apply(RS2014Song song, Action<string> Log)
        {
            if (_addedBeats.Count > 0)
            {
                foreach (var addedBeat in _addedBeats)
                {
                    var beatToRemove = song.Ebeats.Find(beat => beat == addedBeat);
                    song.Ebeats.Remove(beatToRemove);
                }
            }
        }
    }
}
