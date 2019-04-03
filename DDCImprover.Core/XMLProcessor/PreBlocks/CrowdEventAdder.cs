using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Rocksmith2014Xml;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Adds crowd events if they are not already present.
    /// </summary>
    internal sealed class CrowdEventAdder : IProcessorBlock
    {
        private const float IntroCrowdReactionDelay = 0.6f;
        private const float IntroApplauseLength = 2.5f;
        private const float OutroApplauseLength = 4.0f;
        private const float VenueFadeOutLength = 5.0f;

        private static float GetMinTime(IHasTimeCode first, IHasTimeCode second)
        {
            if (first is null)
            {
                return second.Time;
            }
            else if (second is null)
            {
                return first.Time;
            }
            else
            {
                return Math.Min(first.Time, second.Time);
            }
        }

        private void AddIntroApplauseEvent(RS2014Song song, Action<string> Log)
        {
            Arrangement firstPhraseLevel;

            if (song.Levels.Count == 1)
            {
                firstPhraseLevel = song.Levels[0];
            }
            else
            {
                // Find the first phrase that has difficulty levels
                int firstPhraseId = song.PhraseIterations
                    .First(pi => song.Phrases[pi.PhraseId].MaxDifficulty > 0)
                    .PhraseId;
                var firstPhrase = song.Phrases[firstPhraseId];
                firstPhraseLevel = song.Levels[firstPhrase.MaxDifficulty];
            }

            Note firstNote = null;
            if (firstPhraseLevel.Notes.Count > 0)
                firstNote = firstPhraseLevel.Notes[0];

            Chord firstChord = null;
            if (firstPhraseLevel.Chords.Count > 0)
                firstChord = firstPhraseLevel.Chords[0];

            float applauseStartTime = (float)Math.Round(GetMinTime(firstNote, firstChord) + IntroCrowdReactionDelay, 3);
            float applauseEndTime = (float)Math.Round(applauseStartTime + IntroApplauseLength, 3);

            var startEvent = new Event("E3", applauseStartTime);
            var stopEvent = new Event("E13", applauseEndTime);

            song.Events.InsertByTime(startEvent);
            song.Events.InsertByTime(stopEvent);

            Log($"Added intro applause event (E3) at time: {applauseStartTime.TimeToString()}.");
        }

        private void AddOutroApplauseEvent(RS2014Song song, Action<string> Log)
        {
            float audioEnd = song.SongLength;

            float applauseStartTime = (float)Math.Round(audioEnd - VenueFadeOutLength - OutroApplauseLength, 3);

            var startEvent = new Event("D3", applauseStartTime);
            var stopEvent = new Event("E13", audioEnd);

            song.Events.InsertByTime(startEvent);
            song.Events.InsertByTime(stopEvent);

            Log($"Added outro applause event (D3) at time: {applauseStartTime.TimeToString()}.");
        }

        public void Apply(RS2014Song song, Action<string> Log)
        {
            if (song.Events is null)
                song.Events = new EventCollection();

            var events = song.Events;

            // Add initial crowd tempo event only if there are no other tempo events present
            if (!events.Any(ev => Regex.IsMatch(ev.Code, "e[0-2]$")))
            {
                float averageTempo = song.AverageTempo;
                float startBeat = song.StartBeat;

                string crowdSpeed = (averageTempo < 90) ? "e0" :
                                    (averageTempo < 170) ? "e1" : "e2";

                events.InsertByTime(new Event(crowdSpeed, startBeat));

                Log($"Added initial crowd tempo event ({crowdSpeed}, song average tempo: {averageTempo.ToString("F3", NumberFormatInfo.InvariantInfo)})");
            }

            // Add intro applause
            if (!events.Any(ev => ev.Code == "E3"))
            {
                AddIntroApplauseEvent(song, Log);
            }

            // Add outro applause
            if (!events.Any(ev => ev.Code == "D3"))
            {
                AddOutroApplauseEvent(song, Log);
            }
        }
    }
}
