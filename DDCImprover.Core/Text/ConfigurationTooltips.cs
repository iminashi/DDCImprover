namespace DDCImprover.Core.Text
{
    public static class ConfigurationTooltips
    {
        public static string PhraseLength = "Recommended: 256";
        public static string MaxThreads = "Sets the number files processed simultaneously.";
        public static string AdjustHandshapes = "Shortens the length of handshapes that are too close to the next handshape.";
        public static string CheckXML = "Looks for certain issues in the XML and displays a notification if found.";
        public static string RestoreNoguitarSection = "If the arrangement begins with a 'noguitar' section, restore it.";
        public static string RestoreNoguitarAnchors = "Restores anchors (FHP) set at the beginning of noguitar sections and at the beginning of the beatmap.";
        public static string RemoveBeats = "Removes beats that come after the audio has ended.";
        public static string RestoreEndPhrase = "Restores END phrase to the original position if DDC has moved it.";
        public static string FixOneLevel = "Adds a second difficulty level for phrases that have only one.";
        public static string FixChordNames = "Renames chords to match ODLC and processes commands in chord names.";
        public static string RemovePlaceholders = "Removes notes that come after a LinkNext slide and have zero sustain.";
        public static string AddCrowdEvents = "Automatically adds crowd events if they are not already present.";
        public static string TimesInSeconds = "If unchecked, displays times in minutes, seconds and milliseconds.";
        public static string CheckArrIdReset = "Displays a warning for any phrases that have less levels compared to previous DD generation.";
        public static string EnableLogging = "Saves logs to the 'logs' subfolder in the program folder.";
        public static string OverwriteFile = "Overwrites the original file with the processed file.";
    }
}
