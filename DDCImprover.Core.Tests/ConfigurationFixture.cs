namespace DDCImprover.Core.Tests
{
    public class ConfigurationFixture
    {
        public Configuration Configuration { get; }

        public ConfigurationFixture()
        {
            Configuration = new Configuration
            {
                EnableLogging = false,
                RestoreNoguitarSectionAnchors = true,
                RestoreFirstNoguitarSection = true,
                PreserveENDPhraseLocation = true
            };
        }
    }
}
