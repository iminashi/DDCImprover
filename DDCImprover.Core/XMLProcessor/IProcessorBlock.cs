using Rocksmith2014.XML;

using System;

namespace DDCImprover.Core
{
    internal interface IProcessorBlock
    {
        void Apply(InstrumentalArrangement arrangement, Action<string> Log);
    }
}
