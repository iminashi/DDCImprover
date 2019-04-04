using DDCImprover.Core.PreBlocks;
using FluentAssertions;
using Rocksmith2014Xml;
using System;
using Xunit;

namespace DDCImprover.Core.Tests.XmlProcessor
{
    public class PreProcessorBlocksTests
    {
        private readonly Configuration testConfig = new Configuration();
        private readonly RS2014Song testSong;
        private readonly Action<string> nullLog = s => { };

        public PreProcessorBlocksTests()
        {
            Configuration.LogDirectory = @".\logs";
            testConfig.DDCExecutablePath = @".\ddc\ddc.exe";
            testConfig.EnableLogging = false;
            testConfig.RestoreNoguitarSectionAnchors = true;
            testConfig.RestoreFirstNoguitarSection = true;

            XMLProcessor.Preferences = testConfig;

            testSong = RS2014Song.Load(@".\TestFiles\preTest_RS2.xml");
        }

        [Fact]
        public void UnpitchedSlideCheckerTest()
        {
            new UnpitchedSlideChecker().Apply(testSong, nullLog);

            testSong.ArrangementProperties.UnpitchedSlides.Should().Be(1);
        }

        [Fact]
        public void WrongCrowdEventsFixTest()
        {
            testSong.Events.Add(new Event("E0", 10f));
            testSong.Events.Add(new Event("E1", 20f));
            testSong.Events.Add(new Event("E2", 38f));
            testSong.Events.Add(new Event("E1", 77f));

            new EOFCrowdEventsFix().Apply(testSong, nullLog);

            testSong.Events.Should().Contain(e => e.Code == "e0");
            testSong.Events.Should().Contain(e => e.Code == "e1");
            testSong.Events.Should().Contain(e => e.Code == "e2");
            testSong.Events.Should().NotContain(e => e.Code == "E0");
            testSong.Events.Should().NotContain(e => e.Code == "E1");
            testSong.Events.Should().NotContain(e => e.Code == "E2");
        }

        [Fact]
        public void ToneEventGeneratorTest()
        {
            testSong.Events.Should().NotContain(e => e.Code == "tone_a");
            testSong.Events.Should().NotContain(e => e.Code == "tone_b");

            new ToneEventGenerator().Apply(testSong, nullLog);

            testSong.Events.Should().Contain(e => e.Code == "tone_a");
            testSong.Events.Should().Contain(e => e.Code == "tone_b");
        }

        [Fact]
        public void CrowdEventAdderTest()
        {
            var events = testSong.Events;
            int eventCountBefore = events.Count;

            new CrowdEventAdder().Apply(testSong, nullLog);

            // Should generate 5 crowd events: e1, E3, E13, D3, E13
            events.Should().HaveCount(eventCountBefore + 5);

            events.Should().Contain(e => e.Code == "e1");
            events.Should().Contain(e => e.Code == "E3");
            events.Should().Contain(e => e.Code == "D3");
            events.Should().Contain(e => e.Code == "E13");
        }

        [Fact]
        public void WeakBeatPhraseMovingTest()
        {
            float phraseOnAWeakBeatTime = 7.875f;
            var beatBefore = testSong.Ebeats.Find(eb => eb.Time == phraseOnAWeakBeatTime);
            beatBefore.Measure.Should().NotBe(XMLProcessor.TempMeasureNumber);
            testSong.PhraseIterations.Should().Contain(pi => pi.Time == phraseOnAWeakBeatTime);

            new WeakBeatPhraseMovingFix().Apply(testSong, nullLog);

            var beat = testSong.Ebeats.Find(eb => eb.Time == phraseOnAWeakBeatTime);
            beat.Measure.Should().Be(XMLProcessor.TempMeasureNumber);
        }
    }
}
