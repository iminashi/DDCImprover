using Rocksmith2014Xml;
using System;

namespace DDCImprover.Core
{
    internal interface IProcessorBlock
    {
        void Apply(RS2014Song song, Action<string> Log);
    }
}
