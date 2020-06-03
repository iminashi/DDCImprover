using Rocksmith2014Xml;
using System;

namespace DDCImprover.Core
{
    internal interface IProcessorBlock
    {
        void Apply(InstrumentalArrangement arrangement, Action<string> Log);
    }
}
