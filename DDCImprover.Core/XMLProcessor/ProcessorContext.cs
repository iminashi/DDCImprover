using Rocksmith2014.XML;

using System;

namespace DDCImprover.Core
{
    internal class ProcessorContext
    {
        private readonly InstrumentalArrangement arrangement;
        private readonly Action<string> logAction;

        public ProcessorContext(InstrumentalArrangement arrangement, Action<string> logAction)
        {
            this.arrangement = arrangement;
            this.logAction = logAction;
        }

        internal ProcessorContext ApplyFix(IProcessorBlock block)
        {
            block.Apply(arrangement, logAction);
            return this;
        }

        internal ProcessorContext ApplyFixIf(bool condition, IProcessorBlock block)
        {
            if (condition)
            {
                block.Apply(arrangement, logAction);
            }

            return this;
        }
    }
}