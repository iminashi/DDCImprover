using DDCImprover.Core.PostBlocks;
using FluentAssertions;
using Rocksmith2014Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DDCImprover.Core.Tests.XmlProcessor
{
    public class PostProcessorBlocksTests
    {
        private readonly RS2014Song testSong;
        private readonly Action<string> nullLog = s => { };

        public PostProcessorBlocksTests()
        {
            XMLProcessor.Preferences.PreserveENDPhraseLocation = true;

            testSong = RS2014Song.Load(@".\TestFiles\postTest_RS2.xml");
        }

        [Fact]
        public void ExtraneousBeatsRemoverTest()
        {
            float audioEnd = testSong.SongLength;
            testSong.Ebeats.Should().Contain(b => b.Time > audioEnd);

            new ExtraneousBeatsRemover().Apply(testSong, nullLog);

            testSong.Ebeats.Should().NotContain(b => b.Time > audioEnd);
        }

        /*[Fact]
        public void ChordNameProcessorTest()
        {
            //TODO
        }

        [Fact]
        public void OneLevelPhraseFixerTest()
        {
            //TODO
        }*/

        [Fact]
        public void TimeSignatureEventRemoverTest()
        {
            testSong.Events.Should().Contain(e => e.Code.StartsWith("TS"));

            new TimeSignatureEventRemover().Apply(testSong, nullLog);

            testSong.Events.Should().NotContain(e => e.Code.StartsWith("TS"));
        }

        [Fact]
        public void TemporaryBeatRemoverTest()
        {
            var testBeat = new Ebeat(222.222f, 1);
            var addedBeats = new List<Ebeat> { testBeat };
            testSong.Ebeats.Add(testBeat);

            new TemporaryBeatRemover(addedBeats).Apply(testSong, nullLog);

            testSong.Ebeats.Should().NotContain(testBeat);
        }

        [Fact]
        public void FirstNoguitarSectionRestorerTest()
        {
            float firstNGSectionTime = 4.5f;

            new FirstNoguitarSectionRestorer(firstNGSectionTime).Apply(testSong, nullLog);

            testSong.Sections.Should().Contain(s => s.Time == firstNGSectionTime);
            testSong.Phrases.Should().Contain(p => p.Name == "NG");
            testSong.PhraseIterations.Should().Contain(pi => pi.Time == firstNGSectionTime);
        }

        [Fact]
        public void ENDPhraseProcessorTest()
        {
            int endPhraseId = testSong.Phrases.FindIndex(p => p.Name.Equals("END", StringComparison.OrdinalIgnoreCase));
            var endPhraseIter = testSong.PhraseIterations.First(pi => pi.PhraseId == endPhraseId);
            float oldTime = endPhraseIter.Time + 2f;

            new ENDPhraseProcessor(oldTime).Apply(testSong, nullLog);

            endPhraseIter.Time.Should().Be(oldTime);
        }

        [Fact]
        public void NoguitarAnchorRestorerTest()
        {
            var testAnchor = new Anchor(1, 4.5f);
            var ngAnchors = new List<Anchor>
            {
                testAnchor
            };

            new NoguitarAnchorRestorer(ngAnchors).Apply(testSong, nullLog);

            testSong.Levels[0].Anchors.Should().Contain(testAnchor);
        }
    }
}
