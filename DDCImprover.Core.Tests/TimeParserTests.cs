using FluentAssertions;

using Rocksmith2014Xml;

using Xunit;

namespace DDCImprover.Core.Tests
{
    public class TimeParserTests
    {
        [Theory]
        [InlineData("0m0s0", 0)]
        [InlineData("1m18s500", (1 * 60 * 1000) + 18500)]
        [InlineData("0m50s002", 50002)]
        [InlineData("5m22s123", (5 * 60 * 1000) + 22123)]
        public void TimeParser_ParsesCorrectlyMinSecs(string input, int expected)
        {
            int? result = TimeParser.Parse(input);
            result.Should().HaveValue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("0s0", 0)]
        [InlineData("18s500", 18500)]
        [InlineData("50s002", 50002)]
        [InlineData("22s123", 22123)]
        [InlineData("350s089", 350089)]
        public void TimeParser_ParsesCorrectlySecs(string input, int expected)
        {
            int? result = TimeParser.Parse(input);
            result.Should().HaveValue();
            result.Should().Be(expected);
        }

        [Fact]
        public void FindTimeOfNthNoteFrom_FindsCorrectNote()
        {
            const int noteTime = 1200;

            var testArr = new InstrumentalArrangement();
            testArr.Levels.Add(new Level());
            testArr.Levels[0].Notes.Add(new Note
            {
                Time = noteTime
            });

            int result = TimeParser.FindTimeOfNthNoteFrom(testArr.Levels[0], 0, 1);

            result.Should().Be(noteTime);
        }

        [Fact]
        public void FindTimeOfNthNoteFrom_FindsCorrectChord()
        {
            const int chordTime = 1800;

            var testArr = new InstrumentalArrangement();
            testArr.Levels.Add(new Level());
            testArr.Levels[0].Notes.Add(new Note
            {
                Time = chordTime - 500
            });
            testArr.Levels[0].Chords.Add(new Chord
            {
                Time = chordTime
            });

            int result = TimeParser.FindTimeOfNthNoteFrom(testArr.Levels[0], 0, 2);

            result.Should().Be(chordTime);
        }

        [Fact]
        public void FindTimeOfNthNoteFrom_SkipsSplitChordCorrectly()
        {
            const int chordTime = 1800;

            var testArr = new InstrumentalArrangement();
            testArr.Levels.Add(new Level());
            testArr.Levels[0].Notes.Add(new Note
            {
                Time = chordTime - 500,
                String = 1
            });
            testArr.Levels[0].Notes.Add(new Note
            {
                Time = chordTime - 500,
                String = 2
            });
            testArr.Levels[0].Notes.Add(new Note
            {
                Time = chordTime - 500,
                String = 3
            });
            testArr.Levels[0].Chords.Add(new Chord
            {
                Time = chordTime
            });

            int result = TimeParser.FindTimeOfNthNoteFrom(testArr.Levels[0], 0, 2);

            result.Should().Be(chordTime);
        }
    }
}
