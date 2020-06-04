using FluentAssertions;

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
    }
}
