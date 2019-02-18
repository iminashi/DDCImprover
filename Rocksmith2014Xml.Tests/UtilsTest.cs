using FluentAssertions;
using Xunit;

namespace Rocksmith2014Xml.Tests
{
    public class UtilsTest
    {
        [Theory]
        [InlineData(0.000f, 0.000f)]
        [InlineData(0.001f, 0.001f)]
        [InlineData(0.004f, 0.004f)]
        [InlineData(12.345f, 12.345f)]
        [InlineData(44.873f, 44.873f)]
        [InlineData(87.999f, 87.999f)]
        [InlineData(526.820f, 526.820f)]
        [InlineData(700.021f, 700.021f)]
        [InlineData(1234.505f, 1234.505f)]
        public void SameTimesAreEqual(float time1, float time2)
        {
            Utils.TimeEqualToMilliseconds(time1, time2).Should().BeTrue();
        }

        [Theory]
        [InlineData(0.00001f, 0.000f)]
        [InlineData(0.0012f, 0.001f)]
        [InlineData(0.00209f, 0.002f)]
        [InlineData(0.004f, 0.00439f)]
        [InlineData(526.820005f, 526.820f)]
        [InlineData(700.0217f, 700.021f)]
        [InlineData(1234.505f, 1234.5051f)]
        public void SameTimesAreEqualUpToMilliseconds(float time1, float time2)
        {
            Utils.TimeEqualToMilliseconds(time1, time2).Should().BeTrue();
        }

        [Theory]
        [InlineData(0.000f, 0.001f)]
        [InlineData(0.001f, 0.000f)]
        [InlineData(0.004f, 0.005f)]
        [InlineData(12.345f, 12.346f)]
        [InlineData(44.873f, 44.874f)]
        [InlineData(87.999f, 88.000f)]
        [InlineData(526.820f, 526.819f)]
        [InlineData(526.819f, 526.820f)]
        [InlineData(700.021f, 700.022f)]
        [InlineData(1234.505f, 1234.504f)]
        public void DifferentTimesAreNotEqual(float time1, float time2)
        {
            Utils.TimeEqualToMilliseconds(time1, time2).Should().BeFalse();
        }
    }
}
