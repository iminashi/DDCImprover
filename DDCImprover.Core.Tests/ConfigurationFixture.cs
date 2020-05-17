using System;
using System.Collections.Generic;
using System.Text;

namespace DDCImprover.Core.Tests
{
    public class ConfigurationFixture
    {
        public Configuration Configuration { get; private set; }

        public ConfigurationFixture()
        {
            Configuration = new Configuration
            {
                DDCExecutablePath = @".\ddc\ddc.exe",
                EnableLogging = false,
                RestoreNoguitarSectionAnchors = true,
                RestoreFirstNoguitarSection = true,
                PreserveENDPhraseLocation = true
            };
        }
    }
}
