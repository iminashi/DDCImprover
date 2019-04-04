using DDCImprover.Core.PreBlocks;
using FluentAssertions;
using Rocksmith2014Xml;
using System;
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
    }
}
