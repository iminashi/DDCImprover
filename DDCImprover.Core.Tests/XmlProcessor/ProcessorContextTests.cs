using Moq;
using Rocksmith2014Xml;
using System;
using Xunit;

namespace DDCImprover.Core.Tests.XmlProcessor
{
    public class ProcessorContextTests
    {
        private readonly Action<string> nullLog = s => { };
        private readonly RS2014Song testSong = new RS2014Song();
        private readonly ProcessorContext context;

        public ProcessorContextTests()
        {
            context = new ProcessorContext(testSong, nullLog);
        }

        [Fact]
        public void ApplyFix_CallsCorrectInterfaceMethod()
        {
            var mock = new Mock<IProcessorBlock>();
            mock.Setup(block => block.Apply(testSong, nullLog));

            context.ApplyFix(mock.Object);

            mock.Verify(block => block.Apply(testSong, nullLog), Times.Once);
        }

        [Fact]
        public void ApplyFixIf_CallsInterfaceMethodIfConditionTrue()
        {
            var mock = new Mock<IProcessorBlock>();
            mock.Setup(block => block.Apply(testSong, nullLog));

            context.ApplyFixIf(true, mock.Object);

            mock.Verify(block => block.Apply(testSong, nullLog), Times.Once);
        }

        [Fact]
        public void ApplyFixIf_DoesNotCallInterfaceMethodIfConditionFalse()
        {
            var mock = new Mock<IProcessorBlock>();
            mock.Setup(block => block.Apply(testSong, nullLog));

            context.ApplyFixIf(false, mock.Object);

            mock.Verify(block => block.Apply(testSong, nullLog), Times.Never);
        }
    }
}
