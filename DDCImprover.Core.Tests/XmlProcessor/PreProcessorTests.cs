using FluentAssertions;
using Rocksmith2014Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DDCImprover.Core.Tests.XmlProcessor
{
    public class PreProcessorTests
    {
        private readonly Configuration testConfig = new Configuration();
        private readonly XMLProcessor.XMLPreProcessor preProcessor;

        public PreProcessorTests()
        {
            Configuration.LogDirectory = @".\logs";
            testConfig.DDCExecutablePath = @".\ddc\ddc.exe";
            testConfig.EnableLogging = false;
            testConfig.RestoreNoguitarSectionAnchors = true;
            testConfig.RestoreFirstNoguitarSection = true;

            XMLProcessor.Preferences = testConfig;

            XMLProcessor processor = new XMLProcessor(@".\TestFiles\preTest_RS2.xml");
            processor.LoadXMLFile();
            preProcessor = new XMLProcessor.XMLPreProcessor(processor);
        }

        [Fact]
        public void EnablesUnpitchedSlidesInMetadata()
        {
            preProcessor.Process();

            preProcessor.Song.ArrangementProperties.UnpitchedSlides.Should().Be(1);
        }

        [Fact]
        public void CorrectsWrongCrowdEvents()
        {
            preProcessor.Song.Events.Add(new Event("E0", 10f));
            preProcessor.Song.Events.Add(new Event("E1", 20f));
            preProcessor.Song.Events.Add(new Event("E2", 38f));
            preProcessor.Song.Events.Add(new Event("E1", 77f));

            preProcessor.Process();

            preProcessor.Song.Events.Should().Contain(e => e.Code == "e0");
            preProcessor.Song.Events.Should().Contain(e => e.Code == "e1");
            preProcessor.Song.Events.Should().Contain(e => e.Code == "e2");
            preProcessor.Song.Events.Should().NotContain(e => e.Code == "E0");
            preProcessor.Song.Events.Should().NotContain(e => e.Code == "E1");
            preProcessor.Song.Events.Should().NotContain(e => e.Code == "E2");
        }

        [Fact]
        public void GeneratesToneEvents()
        {
            preProcessor.Song.Events.Should().NotContain(e => e.Code == "tone_a");
            preProcessor.Song.Events.Should().NotContain(e => e.Code == "tone_b");

            preProcessor.Process();

            preProcessor.Song.Events.Should().Contain(e => e.Code == "tone_a");
            preProcessor.Song.Events.Should().Contain(e => e.Code == "tone_b");
        }

        [Fact]
        public void PreservesFirstNGSectionTime()
        {
            preProcessor.Process();

            preProcessor.FirstNGSectionTime.Should().BeApproximately(4.5f, 0.001f);
        }

        [Fact]
        public void AddsTemporaryMeasures()
        {
            preProcessor.Process();

            preProcessor.Song.Ebeats.Should().Contain(new Ebeat(7.875f, XMLProcessor.TempMeasureNumber));
        }

        [Fact]
        public void AddsCrowdEvents()
        {
            preProcessor.Process();

            var events = preProcessor.Song.Events;

            // 1 TS event, 5 crowd events, 2 tone event
            events.Should().HaveCount(8);

            events.Should().Contain(e => e.Code == "e1");
            events.Should().Contain(e => e.Code == "E3");
            events.Should().Contain(e => e.Code == "D3");
            events.Should().Contain(e => e.Code == "E13");
        }
    }
}
