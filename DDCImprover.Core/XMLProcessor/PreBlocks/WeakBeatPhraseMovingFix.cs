﻿using Rocksmith2014.XML;

using System;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Changes weak beats that start a phrase into strong beats.
    /// </summary>
    internal sealed class WeakBeatPhraseMovingFix : IProcessorBlock
    {
        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            var weakBeatsWithPhrases =
                from ebeat in arrangement.Ebeats
                join phraseIter in arrangement.PhraseIterations on ebeat.Time equals phraseIter.Time
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
