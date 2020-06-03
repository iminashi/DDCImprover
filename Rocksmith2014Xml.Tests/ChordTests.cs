using FluentAssertions;

using Xunit;

namespace Rocksmith2014Xml.Tests
{
    public static class ChordTests
    {
        [Fact]
        public static void ChordMaskAccessProperiesSettersTest()
        {
            Chord chord = new Chord();
            ChordMask mask = ChordMask.HighDensity | ChordMask.UpStrum;
            chord.Mask = mask;

            chord.IsAccent = true;
            chord.Mask.Should().Be(mask | ChordMask.Accent);
            chord.IsAccent = false;
            chord.Mask.Should().Be(mask);

            mask = ChordMask.None;
            chord.Mask = mask;

            chord.IsFretHandMute = true;
            chord.Mask.Should().Be(mask | ChordMask.FretHandMute);
            chord.IsFretHandMute = false;
            chord.Mask.Should().Be(mask);

            mask = ChordMask.FretHandMute | ChordMask.Accent;
            chord.Mask = mask;

            chord.IsHighDensity = true;
            chord.Mask.Should().Be(mask | ChordMask.HighDensity);
            chord.IsHighDensity = false;
            chord.Mask.Should().Be(mask);

            mask |= ChordMask.HighDensity;
            chord.Mask = mask;

            chord.IsHopo = true;
            chord.Mask.Should().Be(mask | ChordMask.Hopo);
            chord.IsHopo = false;
            chord.Mask.Should().Be(mask);

            mask |= ChordMask.Hopo;
            chord.Mask = mask;

            chord.IsIgnore = true;
            chord.Mask.Should().Be(mask | ChordMask.Ignore);
            chord.IsIgnore = false;
            chord.Mask.Should().Be(mask);

            mask |= ChordMask.Ignore;
            chord.Mask = mask;

            chord.IsLinkNext = true;
            chord.Mask.Should().Be(mask | ChordMask.LinkNext);
            chord.IsLinkNext = false;
            chord.Mask.Should().Be(mask);

            mask |= ChordMask.LinkNext;
            chord.Mask = mask;

            chord.IsPalmMute = true;
            chord.Mask.Should().Be(mask | ChordMask.PalmMute);
            chord.IsPalmMute = false;
            chord.Mask.Should().Be(mask);

            mask |= ChordMask.PalmMute;
            chord.Mask = mask;

            chord.IsUpStrum = true;
            chord.Mask.Should().Be(mask | ChordMask.UpStrum);
            chord.IsUpStrum = false;
            chord.Mask.Should().Be(mask);
        }

        [Fact]
        public static void ChordMaskAccessProperiesGettersTest()
        {
            Chord chord = new Chord
            {
                Mask = ChordMask.None
            };

            chord.IsAccent.Should().BeFalse();
            chord.IsFretHandMute.Should().BeFalse();
            chord.IsHighDensity.Should().BeFalse();
            chord.IsHopo.Should().BeFalse();
            chord.IsIgnore.Should().BeFalse();
            chord.IsLinkNext.Should().BeFalse();
            chord.IsPalmMute.Should().BeFalse();
            chord.IsUpStrum.Should().BeFalse();

            chord.Mask = ChordMask.Accent | ChordMask.FretHandMute | ChordMask.HighDensity;

            chord.IsAccent.Should().BeTrue();
            chord.IsFretHandMute.Should().BeTrue();
            chord.IsHighDensity.Should().BeTrue();
            chord.IsHopo.Should().BeFalse();
            chord.IsIgnore.Should().BeFalse();
            chord.IsLinkNext.Should().BeFalse();
            chord.IsPalmMute.Should().BeFalse();
            chord.IsUpStrum.Should().BeFalse();

            chord.Mask = ChordMask.Hopo | ChordMask.Ignore | ChordMask.LinkNext;

            chord.IsAccent.Should().BeFalse();
            chord.IsFretHandMute.Should().BeFalse();
            chord.IsHighDensity.Should().BeFalse();
            chord.IsHopo.Should().BeTrue();
            chord.IsIgnore.Should().BeTrue();
            chord.IsLinkNext.Should().BeTrue();
            chord.IsPalmMute.Should().BeFalse();
            chord.IsUpStrum.Should().BeFalse();

            chord.Mask = ChordMask.PalmMute | ChordMask.UpStrum;

            chord.IsAccent.Should().BeFalse();
            chord.IsFretHandMute.Should().BeFalse();
            chord.IsHighDensity.Should().BeFalse();
            chord.IsHopo.Should().BeFalse();
            chord.IsIgnore.Should().BeFalse();
            chord.IsLinkNext.Should().BeFalse();
            chord.IsPalmMute.Should().BeTrue();
            chord.IsUpStrum.Should().BeTrue();
        }
    }
}
