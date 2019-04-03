using Rocksmith2014Xml;
using System;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Changes weak beats that start a phrase into strong beats.
    /// </summary>
    internal sealed class WeakBeatPhraseMovingFix : IProcessorBlock
    {
        public void Apply(RS2014Song song, Action<string> Log)
        {
            var weakBeatsWithPhrases =
                from ebeat in song.Ebeats
                join phraseIter in song.PhraseIterations on ebeat.Time equals phraseIter.Time
                where ebeat.Measure == -1
                select ebeat;

            foreach (var beat in weakBeatsWithPhrases)
            {
                beat.Measure = XMLProcessor.TempMeasureNumber;

                Log($"Applied workaround for beat at {beat.Time.TimeToString()}.");
            }
        }
    }
}
