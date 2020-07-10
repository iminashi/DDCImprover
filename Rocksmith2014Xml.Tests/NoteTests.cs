using FluentAssertions;

using System.Collections.Generic;

using Xunit;

namespace Rocksmith2014Xml.Tests
{
    public static class NoteTests
    {
        [Fact]
        public static void NoteMaskAccessPropertiesSettersTest()
        {
            Note note = new Note();
            NoteMask mask = NoteMask.LinkNext | NoteMask.PalmMute;
            note.Mask = mask;

            note.IsAccent = true;
            note.Mask.Should().Be(mask | NoteMask.Accent);
            note.IsAccent = false;
            note.Mask.Should().Be(mask);

            mask = NoteMask.Harmonic | NoteMask.Ignore;
            note.Mask = mask;

            note.IsHammerOn = true;
            note.Mask.Should().Be(mask | NoteMask.HammerOn);
            note.IsHammerOn = false;
            note.Mask.Should().Be(mask);

            mask = NoteMask.Accent | NoteMask.HarmonicPinch;
            note.Mask = mask;

            note.IsHarmonic = true;
            note.Mask.Should().Be(mask | NoteMask.Harmonic);
            note.IsHarmonic = false;
            note.Mask.Should().Be(mask);

            mask = NoteMask.LinkNext | NoteMask.PickDirection;
            note.Mask = mask;

            note.IsHarmonicPinch = true;
            note.Mask.Should().Be(mask | NoteMask.HarmonicPinch);
            note.IsHarmonicPinch = false;
            note.Mask.Should().Be(mask);

            mask = NoteMask.None;
            note.Mask = mask;

            note.IsIgnore = true;
            note.Mask.Should().Be(mask | NoteMask.Ignore);
            note.IsIgnore = false;
            note.Mask.Should().Be(mask);

            mask = NoteMask.PullOff | NoteMask.Mute;
            note.Mask = mask;

            note.IsLinkNext = true;
            note.Mask.Should().Be(mask | NoteMask.LinkNext);
            note.IsLinkNext = false;
            note.Mask.Should().Be(mask);

            mask = NoteMask.PalmMute | NoteMask.Ignore | NoteMask.LinkNext;
            note.Mask = mask;

            note.IsMute = true;
            note.Mask.Should().Be(mask | NoteMask.Mute);
            note.IsMute = false;
            note.Mask.Should().Be(mask);

            mask = NoteMask.Mute | NoteMask.Tremolo | NoteMask.LinkNext;
            note.Mask = mask;

            note.IsPalmMute = true;
            note.Mask.Should().Be(mask | NoteMask.PalmMute);
            note.IsPalmMute = false;
            note.Mask.Should().Be(mask);

            mask = NoteMask.PickDirection;
            note.Mask = mask;

            note.IsPullOff = true;
            note.Mask.Should().Be(mask | NoteMask.PullOff);
            note.IsPullOff = false;
            note.Mask.Should().Be(mask);

            mask = NoteMask.PalmMute | NoteMask.Ignore | NoteMask.LinkNext;
            note.Mask = mask;

            note.IsTremolo = true;
            note.Mask.Should().Be(mask | NoteMask.Tremolo);
            note.IsTremolo = false;
            note.Mask.Should().Be(mask);
        }

        [Fact]
        public static void NoteMaskAccessPropertiesGettersTest()
        {
            Note note = new Note
            {
                Mask = NoteMask.None
            };

            note.IsAccent.Should().BeFalse();
            note.IsHammerOn.Should().BeFalse();
            note.IsHarmonic.Should().BeFalse();
            note.IsHarmonicPinch.Should().BeFalse();
            note.IsIgnore.Should().BeFalse();
            note.IsLinkNext.Should().BeFalse();
            note.IsMute.Should().BeFalse();
            note.IsPalmMute.Should().BeFalse();
            note.IsPullOff.Should().BeFalse();
            note.IsTremolo.Should().BeFalse();
            note.IsPluck.Should().BeFalse();
            note.IsSlap.Should().BeFalse();
            note.IsRightHand.Should().BeFalse();

            note.Mask = NoteMask.Accent | NoteMask.HammerOn | NoteMask.Harmonic | NoteMask.HarmonicPinch | NoteMask.Ignore;

            note.IsAccent.Should().BeTrue();
            note.IsHammerOn.Should().BeTrue();
            note.IsHarmonic.Should().BeTrue();
            note.IsHarmonicPinch.Should().BeTrue();
            note.IsIgnore.Should().BeTrue();
            note.IsLinkNext.Should().BeFalse();
            note.IsMute.Should().BeFalse();
            note.IsPalmMute.Should().BeFalse();
            note.IsPullOff.Should().BeFalse();
            note.IsTremolo.Should().BeFalse();
            note.IsPluck.Should().BeFalse();
            note.IsSlap.Should().BeFalse();
            note.IsRightHand.Should().BeFalse();

            note.Mask = NoteMask.LinkNext | NoteMask.Mute | NoteMask.PalmMute | NoteMask.PickDirection | NoteMask.PullOff | NoteMask.Tremolo;

            note.IsAccent.Should().BeFalse();
            note.IsHammerOn.Should().BeFalse();
            note.IsHarmonic.Should().BeFalse();
            note.IsHarmonicPinch.Should().BeFalse();
            note.IsIgnore.Should().BeFalse();
            note.IsLinkNext.Should().BeTrue();
            note.IsMute.Should().BeTrue();
            note.IsPalmMute.Should().BeTrue();
            note.IsPullOff.Should().BeTrue();
            note.IsTremolo.Should().BeTrue();
            note.IsPluck.Should().BeFalse();
            note.IsSlap.Should().BeFalse();
            note.IsRightHand.Should().BeFalse();

            note.Mask = NoteMask.Slap | NoteMask.Pluck | NoteMask.RightHand;

            note.IsAccent.Should().BeFalse();
            note.IsHammerOn.Should().BeFalse();
            note.IsHarmonic.Should().BeFalse();
            note.IsHarmonicPinch.Should().BeFalse();
            note.IsIgnore.Should().BeFalse();
            note.IsLinkNext.Should().BeFalse();
            note.IsMute.Should().BeFalse();
            note.IsPalmMute.Should().BeFalse();
            note.IsPullOff.Should().BeFalse();
            note.IsTremolo.Should().BeFalse();
            note.IsPluck.Should().BeTrue();
            note.IsSlap.Should().BeTrue();
            note.IsRightHand.Should().BeTrue();
        }

        [Fact]
        public static void OtherGettersTest()
        {
            Note note = new Note();

            note.IsBend.Should().BeFalse();
            note.IsSlide.Should().BeFalse();
            note.IsUnpitchedSlide.Should().BeFalse();
            note.IsVibrato.Should().BeFalse();
            note.IsTap.Should().BeFalse();

            note.BendValues = new List<BendValue> { new BendValue(100, 1.5f) };

            note.IsBend.Should().BeTrue();
            note.IsSlide.Should().BeFalse();
            note.IsUnpitchedSlide.Should().BeFalse();
            note.IsVibrato.Should().BeFalse();

            note.SlideTo = 14;

            note.IsBend.Should().BeTrue();
            note.IsSlide.Should().BeTrue();
            note.IsUnpitchedSlide.Should().BeFalse();
            note.IsVibrato.Should().BeFalse();

            note.SlideUnpitchTo = 12;

            note.IsBend.Should().BeTrue();
            note.IsSlide.Should().BeTrue();
            note.IsUnpitchedSlide.Should().BeTrue();
            note.IsVibrato.Should().BeFalse();

            note.Vibrato = 80;

            note.IsBend.Should().BeTrue();
            note.IsSlide.Should().BeTrue();
            note.IsUnpitchedSlide.Should().BeTrue();
            note.IsVibrato.Should().BeTrue();

            note.Tap = 2;

            note.IsTap.Should().BeTrue();
        }

        [Fact]
        public static void CopyConstructorCopiesAllValues()
        {
            Note note1 = new Note
            {
                Fret = 22,
                LeftHand = 3,
                Mask = NoteMask.Accent | NoteMask.Ignore | NoteMask.LinkNext | NoteMask.Mute | NoteMask.Slap,
                SlideTo = 7,
                SlideUnpitchTo = 9,
                String = 4,
                Sustain = 99000,
                Tap = 2,
                Time = 33000,
                Vibrato = 80,
                BendValues = new List<BendValue>
                {
                    new BendValue(34000, 3f),
                    new BendValue(35000, 4f)
                }
            };

            Note note2 = new Note(note1);

            note1.Should().NotBeSameAs(note2);
            note1.BendValues.Should().NotBeSameAs(note2.BendValues);

            note1.BendValues.Should().BeEquivalentTo(note2.BendValues);

            Assert.Equal(note1.Fret, note2.Fret);
            Assert.Equal(note1.LeftHand, note2.LeftHand);
            Assert.Equal(note1.Mask, note2.Mask);
            Assert.Equal(note1.SlideTo, note2.SlideTo);
            Assert.Equal(note1.SlideUnpitchTo, note2.SlideUnpitchTo);
            Assert.Equal(note1.String, note2.String);
            Assert.Equal(note1.Sustain, note2.Sustain);
            Assert.Equal(note1.Tap, note2.Tap);
            Assert.Equal(note1.Time, note2.Time);
            Assert.Equal(note1.Vibrato, note2.Vibrato);
        }
    }
}
