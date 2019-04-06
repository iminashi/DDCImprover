using Rocksmith2014Xml;
using System;

namespace DDCImprover.Core
{
    internal class ProcessorContext
    {
        private readonly RS2014Song song;
        private readonly Action<string> logAction;

        public ProcessorContext(RS2014Song song, Action<string> logAction)
        {
            this.song = song;
            this.logAction = logAction;
        }

        internal ProcessorContext ApplyFix(IProcessorBlock block)
        {
            block.Apply(song, logAction);
            return this;
        }

        internal ProcessorContext ApplyFixIf(bool condition, IProcessorBlock block)
        {
            if (condition)
            {
                block.Apply(song, logAction);
            }

            return this;
        }
    }
}