using FluentAssertions;
using Rocksmith2014Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DDCImprover.Core.Tests
{
    public class ArrangementCheckerTests
    {
        private readonly List<ImproverMessage> messages;
        private readonly RS2014Song song;

        public ArrangementCheckerTests()
        {
            messages = new List<ImproverMessage>();
            song = new RS2014Song();
            song.Sections.Add(new Section("riff", 1f, 1));
            song.Sections.Add(new Section("noguitar", 50f, 1));
            song.Levels.Add(new Level());
        }

        [Fact]
        public void DetectsBendValueMismatches()
        {
            song.Levels[0].Notes.Add(new Note
            {
                Sustain = 1f,
                IsLinkNext = true,
                Bend = 1,
                BendValues = new BendValueCollection
                {
                    new BendValue(1f, 1f)
                }
            });
            song.Levels[0].Notes.Add(new Note
            {
                Time = 1f,
                Sustain = 1f,
            });

            ArrangementChecker checker = new ArrangementChecker(song, messages, _ => { });
            checker.CheckNotes(song.Levels[0]);

            messages.Should().HaveCount(1);
            messages[0].Message.Should().Contain("mismatch");
        }

        [Fact]
        public void DoesNotProduceFalsePositiveForNoBendValueAtNoteTime()
        {
            song.Levels[0].Notes.Add(new Note
            {
                Sustain = 1f,
                IsLinkNext = true,
                Bend = 1,
                BendValues = new BendValueCollection
                {
                    new BendValue(0f, 1f),
                    new BendValue(1f, 0f)
                }
            });
            song.Levels[0].Notes.Add(new Note
            {
                Time = 1f,
                Sustain = 1f,
                Bend = 1,
                BendValues = new BendValueCollection
                {
                    new BendValue(2f, 1f)
                }
            });

            ArrangementChecker checker = new ArrangementChecker(song, messages, _ => { });
            checker.CheckNotes(song.Levels[0]);

            messages.Should().HaveCount(0);
        }
    }
}
