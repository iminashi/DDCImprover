using FluentAssertions;
using Xunit;

namespace DDCImprover.Core.Tests.XmlProcessor
{
    public class PreProcessorTests : IClassFixture<ConfigurationFixture>
    {
        private readonly XMLPreProcessor preProcessor;

        public PreProcessorTests(ConfigurationFixture fixture)
        {
            XMLProcessor.Preferences = fixture.Configuration;

            XMLProcessor processor = new XMLProcessor(@".\TestFiles\preTest_RS2.xml");
            processor.LoadXMLFile();
            preProcessor = new XMLPreProcessor(processor, _ => { });
        }

        [Fact]
        public void PreservesFirstNGSectionTime()
        {
            preProcessor.Process();

            preProcessor.FirstNGSectionTime.Should().Be(4500);
        }
    }
}
