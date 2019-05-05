using DDCImprover.Core.PreBlocks;
using FluentAssertions;
using Rocksmith2014Xml;
using System;
using System.Collections.Generic;
using Xunit;

namespace DDCImprover.Core.Tests.XmlProcessor
{
    public class PreProcessorBlocksTests
    {
        private readonly RS2014Song testSong;
        private readonly Action<string> nullLog = _ => { };
        private readonly PhraseMover phraseMover = new PhraseMover(new List<ImproverMessage>(), new List<Ebeat>());

        public PreProcessorBlocksTests()
        {
            testSong = RS2014Song.Load(@".\TestFiles\preTest_RS2.xml");
        }

        [Fact]
        public void UnpitchedSlideCheckerTest()
        {
            testSong.ArrangementProperties.UnpitchedSlides.Should().Be(0);

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
            const float phraseOnAWeakBeatTime = 7.875f;
            var beatBefore = testSong.Ebeats.Find(eb => eb.Time == phraseOnAWeakBeatTime);
            beatBefore.Measure.Should().NotBe(XMLProcessor.TempMeasureNumber);
            testSong.PhraseIterations.Should().Contain(pi => pi.Time == phraseOnAWeakBeatTime);

            new WeakBeatPhraseMovingFix().Apply(testSong, nullLog);

            var beat = testSong.Ebeats.Find(eb => eb.Time == phraseOnAWeakBeatTime);
            beat.Measure.Should().Be(XMLProcessor.TempMeasureNumber);
        }

        [Fact]
        public void HandShapeAdjusterTest()
        {
            var testHandshape = new HandShape(0, 10f, 11.999f);
            testSong.Levels[0].HandShapes.Add(testHandshape);
            testSong.Levels[0].HandShapes.Add(new HandShape(0, 12f, 13f));

            new HandShapeAdjuster().Apply(testSong, nullLog);

            testHandshape.EndTime.Should().BeLessThan(11.999f);
        }

        private PhraseIteration AddTestPhrase(string name, float time)
        {
            testSong.Phrases.Add(new Phrase(name, 0, PhraseMask.None));
            var phraseIter = new PhraseIteration
            {
                PhraseId = testSong.Phrases.Count - 1,
                Time = time
            };
            testSong.PhraseIterations.Add(phraseIter);

            return phraseIter;
        }

        [Fact]
        public void PhraseMover_MoveTo_Test()
        {
            var testPhraseIter = AddTestPhrase("moveto18s300", 15f);

            phraseMover.Apply(testSong, nullLog);

            testPhraseIter.Time.Should().BeApproximately(18.3f, 0.001f);
        }

        [Fact]
        public void PhraseMover_MoveRelative_Test()
        {
            const float noteTime = 16.666f;

            var testPhraseIter = AddTestPhrase("moveR1", 15f);
            testSong.Levels[0].Notes.Add(new Note { Time = noteTime, String = 2 });

            phraseMover.Apply(testSong, nullLog);

            testPhraseIter.Time.Should().BeApproximately(noteTime, 0.001f);
        }

        [Fact]
        public void PhraseMoverMovesSectionAlso()
        {
            const float noteTime = 16.666f;
            const float phraseTime = 15f;

            AddTestPhrase("moveR1", phraseTime);
            var testSection = new Section("riff", phraseTime, 1);
            testSong.Sections.Add(testSection);
            testSong.Levels[0].Notes.Add(new Note { Time = noteTime, String = 2 });

            phraseMover.Apply(testSong, nullLog);

            testSection.Time.Should().BeApproximately(noteTime, 0.001f);
        }

        [Fact]
        public void PhraseMoverMovesAnchorAlso()
        {
            const float noteTime = 16.666f;
            const float phraseTime = 15f;

            AddTestPhrase("mover1", phraseTime);
            testSong.Levels[0].Anchors.Add(new Anchor(1, phraseTime, 4));
            testSong.Levels[0].Notes.Add(new Note { Time = noteTime, String = 2 });

            phraseMover.Apply(testSong, nullLog);

            testSong.Levels[0].Anchors.Should().Contain(a => a.Time == noteTime);
        }

        [Fact]
        public void CustomEvent_RemoveBeats_Test()
        {
            testSong.Events.Add(new Event("removebeats", 112.5f));

            new CustomEventsPreProcessor().Apply(testSong, nullLog);

            testSong.Events.Should().NotContain(ev => ev.Code == "removebeats");
            testSong.Ebeats.Should().NotContain(eb => eb.Time >= 112.5f);
        }

        [Fact]
        public void CustomEvent_Width3_Test()
        {
            testSong.Events.Add(new Event("w3", testSong.Levels[0].Anchors[0].Time));

            new CustomEventsPreProcessor().Apply(testSong, nullLog);

            testSong.Events.Should().NotContain(ev => ev.Code == "w3");
            testSong.Levels[0].Anchors[0].Width.Should().Be(3f);
        }
    }
}
