using Rocksmith2014Xml;
using Rocksmith2014Xml.Extensions;

using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Adds crowd events if they are not already present.
    /// </summary>
    internal sealed class CrowdEventAdder : IProcessorBlock
    {
        private const int IntroCrowdReactionDelay = 600; // 0.6 s
        private const int IntroApplauseLength = 2500; // 2.5 s
        private const int OutroApplauseLength = 4000; // 4 s
        private const int VenueFadeOutLength = 5000; // 5 s

        private static int GetMinTime(IHasTimeCode? first, IHasTimeCode? second)
        {
            if (first is null)
            {
                return second?.Time ?? throw new InvalidOperationException("Trying to compare two null values");
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

        private void AddIntroApplauseEvent(InstrumentalArrangement arrangement, Action<string> Log)
        {
            Level firstPhraseLevel;

            if (arrangement.Levels.Count == 1)
            {
                firstPhraseLevel = arrangement.Levels[0];
            }
            else
            {
                // Find the first phrase that has difficulty levels
                int firstPhraseId = arrangement.PhraseIterations
                    .First(pi => arrangement.Phrases[pi.PhraseId].MaxDifficulty > 0)
                    .PhraseId;
                var firstPhrase = arrangement.Phrases[firstPhraseId];
                firstPhraseLevel = arrangement.Levels[firstPhrase.MaxDifficulty];
            }

            Note? firstNote = null;
            if (firstPhraseLevel.Notes.Count > 0)
                firstNote = firstPhraseLevel.Notes[0];

            Chord? firstChord = null;
            if (firstPhraseLevel.Chords.Count > 0)
                firstChord = firstPhraseLevel.Chords[0];

            int applauseStartTime = GetMinTime(firstNote, firstChord) + IntroCrowdReactionDelay;
            int applauseEndTime = applauseStartTime + IntroApplauseLength;

            var startEvent = new Event("E3", applauseStartTime);
            var stopEvent = new Event("E13", applauseEndTime);

            arrangement.Events.InsertByTime(startEvent);
            arrangement.Events.InsertByTime(stopEvent);

            Log($"Added intro applause event (E3) at time: {applauseStartTime.TimeToString()}.");
        }

        private void AddOutroApplauseEvent(InstrumentalArrangement arrangement, Action<string> Log)
        {
            int audioEnd = arrangement.SongLength;

            int applauseStartTime = audioEnd - VenueFadeOutLength - OutroApplauseLength;

            var startEvent = new Event("D3", applauseStartTime);
            var stopEvent = new Event("E13", audioEnd);

            arrangement.Events.InsertByTime(startEvent);
            arrangement.Events.InsertByTime(stopEvent);

            Log($"Added outro applause event (D3) at time: {applauseStartTime.TimeToString()}.");
        }

        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            var events = arrangement.Events;

            // Add initial crowd tempo event only if there are no other tempo events present
            if (!events.Any(ev => Regex.IsMatch(ev.Code, "e[0-2]$")))
            {
                float averageTempo = arrangement.AverageTempo;
                int startBeat = arrangement.StartBeat;

                string crowdSpeed = (averageTempo < 90f) ? "e0" :
                                    (averageTempo < 170f) ? "e1" : "e2";

                events.InsertByTime(new Event(crowdSpeed, startBeat));

                Log($"Added initial crowd tempo event ({crowdSpeed}, song average tempo: {averageTempo.ToString("F3", NumberFormatInfo.InvariantInfo)})");
            }

            // Add intro applause
            if (!events.Any(ev => ev.Code == "E3"))
            {
                AddIntroApplauseEvent(arrangement, Log);
            }

            // Add outro applause
            if (!events.Any(ev => ev.Code == "D3"))
            {
                AddOutroApplauseEvent(arrangement, Log);
            }
        }
    }
}
