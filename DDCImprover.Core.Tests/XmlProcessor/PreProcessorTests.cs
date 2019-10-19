using FluentAssertions;
using Xunit;

namespace DDCImprover.Core.Tests.XmlProcessor
{
    public class PreProcessorTests
    {
        private readonly Configuration testConfig = new Configuration();
        private readonly XMLPreProcessor preProcessor;

        public PreProcessorTests()
        {
            testConfig.DDCExecutablePath = @".\ddc\ddc.exe";
            testConfig.EnableLogging = false;
            testConfig.RestoreNoguitarSectionAnchors = true;
            testConfig.RestoreFirstNoguitarSection = true;

            XMLProcessor.Preferences = testConfig;

            XMLProcessor processor = new XMLProcessor(@".\TestFiles\preTest_RS2.xml");
            processor.LoadXMLFile();
            preProcessor = new XMLPreProcessor(processor, _ => { });
        }

        [Fact]
        public void PreservesFirstNGSectionTime()
        {
            preProcessor.Process();

            preProcessor.FirstNGSectionTime.Should().BeApproximately(4.5f, 0.001f);
        }
    }
}
