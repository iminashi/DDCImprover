﻿using DDCImprover.Core.PreBlocks;
using FluentAssertions;
using Rocksmith2014.XML;
using System;
using System.Collections.Generic;
using Xunit;

namespace DDCImprover.Core.Tests.XmlProcessor
{
    public class PreProcessorBlocksTests
    {
        private readonly InstrumentalArrangement testArrangement;
        private readonly Action<string> nullLog = _ => { };
        private readonly PhraseMover phraseMover = new PhraseMover(new List<ImproverMessage>(), new List<Ebeat>());

        public PreProcessorBlocksTests()
        {
            testArrangement = InstrumentalArrangement.Load(@".\TestFiles\preTest_RS2.xml");
        }

        [Fact]
        public void UnpitchedSlideChecker_EnablesUnpitchedSlides()
        {
            testArrangement.MetaData.ArrangementProperties.UnpitchedSlides.Should().Be(false);

            new UnpitchedSlideChecker().Apply(testArrangement, nullLog);

            testArrangement.MetaData.ArrangementProperties.UnpitchedSlides.Should().Be(true);
        }

        [Fact]
        public void WrongCrowdEventsFix_FixesWrongEvents()
        {
            testArrangement.Events.Add(new Event("E0", 10000));
            testArrangement.Events.Add(new Event("E1", 20000));
            testArrangement.Events.Add(new Event("E2", 38000));
            testArrangement.Events.Add(new Event("E1", 77000));

            new EOFCrowdEventsFix().Apply(testArrangement, nullLog);

            testArrangement.Events.Should().Contain(e => e.Code == "e0");
            testArrangement.Events.Should().Contain(e => e.Code == "e1");
            testArrangement.Events.Should().Contain(e => e.Code == "e2");
            testArrangement.Events.Should().NotContain(e => e.Code == "E0");
            testArrangement.Events.Should().NotContain(e => e.Code == "E1");
            testArrangement.Events.Should().NotContain(e => e.Code == "E2");
        }

        [Fact]
        public void ToneEventGenerator_GeneratesToneEvents()
        {
            testArrangement.Events.Should().NotContain(e => e.Code == "tone_a");
            testArrangement.Events.Should().NotContain(e => e.Code == "tone_b");
            testArrangement.Events.Should().NotContain(e => e.Code == "tone_c");
            testArrangement.Events.Should().NotContain(e => e.Code == "tone_d");

            new ToneEventGenerator().Apply(testArrangement, nullLog);

            testArrangement.Events.Should().Contain(e => e.Code == "tone_a");
            testArrangement.Events.Should().Contain(e => e.Code == "tone_b");
            testArrangement.Events.Should().Contain(e => e.Code == "tone_c");
            testArrangement.Events.Should().Contain(e => e.Code == "tone_d");
        }

        [Fact]
        public void CrowdEventAdder_AddsCrowdEvents()
        {
            var events = testArrangement.Events;
            int eventCountBefore = events.Count;

            new CrowdEventAdder().Apply(testArrangement, nullLog);

            // Should generate 5 crowd events: e1, E3, E13, D3, E13
            events.Should().HaveCount(eventCountBefore + 5);

            events.Should().Contain(e => e.Code == "e1");
            events.Should().Contain(e => e.Code == "E3");
            events.Should().Contain(e => e.Code == "D3");
            events.Should().Contain(e => e.Code == "E13");
        }

        [Fact]
        public void WeakBeatPhraseMovingFix_MakesWeakBeatStrong()
        {
            const uint phraseOnAWeakBeatTime = 7875;
            var beatBefore = testArrangement.Ebeats.Find(eb => eb.Time == phraseOnAWeakBeatTime);
            beatBefore.Measure.Should().NotBe(XMLProcessor.TempMeasureNumber);
            testArrangement.PhraseIterations.Should().Contain(pi => pi.Time == phraseOnAWeakBeatTime);

            new WeakBeatPhraseMovingFix().Apply(testArrangement, nullLog);

            var beat = testArrangement.Ebeats.Find(eb => eb.Time == phraseOnAWeakBeatTime);
            beat.Measure.Should().Be(XMLProcessor.TempMeasureNumber);
        }

        [Fact]
        public void HandShapeAdjuster_ShortensHandshape()
        {
            var testHandshape = new HandShape(0, 10000, 11999);
            testArrangement.Levels[0].HandShapes.Add(testHandshape);
            testArrangement.Levels[0].HandShapes.Add(new HandShape(0, 12000, 13000));

            new HandShapeAdjuster().Apply(testArrangement, nullLog);

            testHandshape.EndTime.Should().BeLessThan(11999);
        }

        private PhraseIteration AddTestPhrase(string name, int time)
        {
            testArrangement.Phrases.Add(new Phrase(name, 0, PhraseMask.None));
            var phraseIter = new PhraseIteration(time, testArrangement.Phrases.Count - 1);
            testArrangement.PhraseIterations.Add(phraseIter);

            return phraseIter;
        }

        [Fact]
        public void PhraseMoverMoveTo_MovesPhraseToCorrectPlace()
        {
            var testPhraseIter = AddTestPhrase("moveto18s300", 15000);

            phraseMover.Apply(testArrangement, nullLog);

            testPhraseIter.Time.Should().Be(18300);
        }

        [Fact]
        public void PhraseMoverRelative_MovesPhraseToCorrectPlace()
        {
            const int noteTime = 16666;

            var testPhraseIter = AddTestPhrase("moveR1", 15000);
            testArrangement.Levels[0].Notes.Add(new Note { Time = noteTime, String = 2 });

            phraseMover.Apply(testArrangement, nullLog);

            testPhraseIter.Time.Should().Be(noteTime);
        }

        [Fact]
        public void PhraseMoverRelative_MovesSectionAlso()
        {
            const int noteTime = 16666;
            const int phraseTime = 15000;

            AddTestPhrase("moveR1", phraseTime);
            var testSection = new Section("riff", phraseTime, 1);
            testArrangement.Sections.Add(testSection);
            testArrangement.Levels[0].Notes.Add(new Note { Time = noteTime, String = 2 });

            phraseMover.Apply(testArrangement, nullLog);

            testSection.Time.Should().Be(noteTime);
        }

        [Fact]
        public void PhraseMoverRelative_MovesAnchorAlso()
        {
            const int noteTime = 16666;
            const int phraseTime = 15000;

            AddTestPhrase("mover1", phraseTime);
            testArrangement.Levels[0].Anchors.Add(new Anchor(1, phraseTime, 4));
            testArrangement.Levels[0].Notes.Add(new Note { Time = noteTime, String = 2 });

            phraseMover.Apply(testArrangement, nullLog);

            testArrangement.Levels[0].Anchors.Should().Contain(a => a.Time == noteTime);
        }

        [Fact]
        public void Width3Event_ChangesAnchorWidth()
        {
            testArrangement.Events.Add(new Event("w3", testArrangement.Levels[0].Anchors[0].Time));

            new CustomEventsPreProcessor().Apply(testArrangement, nullLog);

            testArrangement.Events.Should().NotContain(ev => ev.Code == "w3");
            testArrangement.Levels[0].Anchors[0].Width.Should().Be(3);
        }
    }
}
