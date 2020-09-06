using Rocksmith2014.XML;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Renames chord names to match ODLC and processes chord name commands.
    /// </summary>
    internal sealed class ChordNameProcessor : IProcessorBlock
    {
        private readonly IList<ImproverMessage> _statusMessages;

        public ChordNameProcessor(IList<ImproverMessage> statusMessages)
        {
            _statusMessages = statusMessages;
        }

        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            var chordRenamed = new Dictionary<string, bool>();

            Log("Processing chord names...");

            for (int i = 0; i < arrangement.ChordTemplates.Count; i++)
            {
                var currentChordTemplate = arrangement.ChordTemplates[i];
                string chordName = currentChordTemplate.Name;

                // One fret handshape fret moving
                if (chordName.StartsWith("OF"))
                {
                    Match match = Regex.Match(chordName, @"\d+$");
                    if (match.Success)
                    {
                        sbyte newFretNumber = sbyte.Parse(match.Value, NumberFormatInfo.InvariantInfo);

                        for (int fretIndex = 0; fretIndex < 6; fretIndex++)
                        {
                            if (currentChordTemplate.Frets[fretIndex] == 0)
                            {
                                // Remove unnecessary open string notes
                                currentChordTemplate.Frets[fretIndex] = -1;
                            }
                            else if (currentChordTemplate.Frets[fretIndex] != -1)
                            {
                                currentChordTemplate.Frets[fretIndex] = newFretNumber;
                            }
                        }

                        Log($"Adjusted fret number of one fret chord: {currentChordTemplate.Name}");

                        // Remove chord name
                        currentChordTemplate.Name = "";
                        currentChordTemplate.DisplayName = "";
                    }
                    else
                    {
                        const string errorMessage = "Unable to read fret value from OF chord.";
                        _statusMessages.Add(new ImproverMessage(errorMessage, MessageType.Warning));
                        Log(errorMessage);
                    }
                }

                string oldChordName = currentChordTemplate.Name;
                string oldDisplayName = currentChordTemplate.DisplayName;
                string newChordName = oldChordName;
                string newDisplayName = oldDisplayName;

                if (string.IsNullOrWhiteSpace(oldChordName))
                {
                    newChordName = string.Empty;
                }
                else
                {
                    if (oldChordName.Contains("min"))
                        newChordName = oldChordName.Replace("min", "m");
                    if (oldChordName.Contains("CONV"))
                        newChordName = oldChordName.Replace("CONV", "");
                    if (oldChordName.Contains("-nop"))
                        newChordName = oldChordName.Replace("-nop", "");
                    if (oldChordName.Contains("-arp"))
                        newChordName = oldChordName.Replace("-arp", "");
                }

                if (string.IsNullOrWhiteSpace(oldDisplayName))
                {
                    newDisplayName = string.Empty;
                }
                else
                {
                    if (oldDisplayName.Contains("min"))
                        newDisplayName = oldDisplayName.Replace("min", "m");
                    if (oldDisplayName.Contains("CONV"))
                        newDisplayName = oldDisplayName.Replace("CONV", "-arp");
                }

                // Log message for changed chord names that are not empty
                if (newChordName != oldChordName && newChordName.Length != 0 && !chordRenamed.ContainsKey(oldChordName))
                {
                    if (oldChordName.Contains("CONV") || oldChordName.Contains("-arp"))
                        Log($"--Converted {newChordName} handshape into an arpeggio.");
                    else
                        Log($"--Renamed \"{oldChordName}\" to \"{newChordName}\"");

                    // Display renamed chords with the same name only once
                    chordRenamed[oldChordName] = true;
                }

                currentChordTemplate.Name = newChordName;
                currentChordTemplate.DisplayName = newDisplayName;
            }
        }
    }
}
