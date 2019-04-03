using DDCImprover.Core.PreBlocks;
using Rocksmith2014Xml;
using System;

namespace DDCImprover.Core
{
    internal class PreProcessorContext
    {
        private readonly RS2014Song song;
        private readonly Action<string> logAction;

        public PreProcessorContext(RS2014Song song, Action<string> logAction)
        {
            this.song = song;
            this.logAction = logAction;
        }

        internal PreProcessorContext ApplyFix(IProcessorBlock block)
        {
            block.Apply(song, logAction);
            return this;
        }

        internal PreProcessorContext ApplyFixIf(bool condition, IProcessorBlock block)
        {
            if (condition)
            {
                block.Apply(song, logAction);
            }

            return this;
        }
    }
}