using Moq;
using Rocksmith2014.XML;
using System;
using Xunit;

namespace DDCImprover.Core.Tests.XmlProcessor
{
    public class ProcessorContextTests
    {
        private readonly Action<string> nullLog = _ => { };
        private readonly InstrumentalArrangement testArrangement = new InstrumentalArrangement();
        private readonly ProcessorContext context;

        public ProcessorContextTests()
        {
            context = new ProcessorContext(testArrangement, nullLog);
        }

        [Fact]
        public void ApplyFix_CallsCorrectInterfaceMethod()
        {
            var mock = new Mock<IProcessorBlock>();
            mock.Setup(block => block.Apply(testArrangement, nullLog));

            context.ApplyFix(mock.Object);

            mock.Verify(block => block.Apply(testArrangement, nullLog), Times.Once);
        }

        [Fact]
        public void ApplyFixIf_CallsInterfaceMethodIfConditionTrue()
        {
            var mock = new Mock<IProcessorBlock>();
            mock.Setup(block => block.Apply(testArrangement, nullLog));

            context.ApplyFixIf(true, mock.Object);

            mock.Verify(block => block.Apply(testArrangement, nullLog), Times.Once);
        }

        [Fact]
        public void ApplyFixIf_DoesNotCallInterfaceMethodIfConditionFalse()
        {
            var mock = new Mock<IProcessorBlock>();
            mock.Setup(block => block.Apply(testArrangement, nullLog));

            context.ApplyFixIf(false, mock.Object);

            mock.Verify(block => block.Apply(testArrangement, nullLog), Times.Never);
        }
    }
}
