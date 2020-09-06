using FluentAssertions;

using Rocksmith2014.XML;

using System;
using System.Collections.Generic;

using Xunit;

namespace DDCImprover.Core.Tests
{
    public class ArrangementCheckerTests
    {
        private readonly List<ImproverMessage> messages;
        private readonly InstrumentalArrangement arrangement;
        private readonly Action<string> nullLog = _ => { };

        public ArrangementCheckerTests()
        {
            messages = new List<ImproverMessage>();
            arrangement = new InstrumentalArrangement();
            arrangement.Sections.Add(new Section("riff", 1000, 1));
            arrangement.Sections.Add(new Section("noguitar", 50000, 1));
            arrangement.Levels.Add(new Level());
        }

        [Fact]
        public void DetectsBendValueMismatches()
        {
            arrangement.Levels[0].Notes.Add(new Note
            {
                Sustain = 1000,
                IsLinkNext = true,
                BendValues = new List<BendValue>
                {
                    new BendValue(1000, 1f)
                }
            });
            arrangement.Levels[0].Notes.Add(new Note
            {
                Time = 1000,
                Sustain = 1000,
            });

            ArrangementChecker checker = new ArrangementChecker(arrangement, messages, nullLog);
            checker.CheckNotes(arrangement.Levels[0]);

            messages.Should().HaveCount(1);
            messages[0].Message.Should().Contain("mismatch");
        }

        [Fact]
        public void DoesNotProduceFalsePositiveForNoBendValueAtNoteTime()
        {
            arrangement.Levels[0].Notes.Add(new Note
            {
                Sustain = 1000,
                IsLinkNext = true,
                BendValues = new List<BendValue>
                {
                    new BendValue(0, 1f),
                    new BendValue(1000, 0f)
                }
            });
            arrangement.Levels[0].Notes.Add(new Note
            {
                Time = 1000,
                Sustain = 1000,
                BendValues = new List<BendValue>
                {
                    new BendValue(2000, 1f)
                }
            });

            ArrangementChecker checker = new ArrangementChecker(arrangement, messages, nullLog);
            checker.CheckNotes(arrangement.Levels[0]);

            messages.Should().HaveCount(0);
        }

        [Fact]
        public void DetectsLinknextSlideMismatch()
        {
            arrangement.Levels[0].Notes.Add(new Note
            {
                Fret = 1,
                Sustain = 1000,
                IsLinkNext = true,
                SlideTo = 5
            });
            arrangement.Levels[0].Notes.Add(new Note
            {
                Fret = 10,
                Time = 1000,
                Sustain = 1000,
            });

            ArrangementChecker checker = new ArrangementChecker(arrangement, messages, nullLog);
            checker.CheckNotes(arrangement.Levels[0]);

            messages.Should().HaveCount(1);
            messages[0].Message.Should().Contain("fret mismatch");
        }
    }
}
