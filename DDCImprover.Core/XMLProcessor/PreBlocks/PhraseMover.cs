using Rocksmith2014Xml;
using Rocksmith2014Xml.Extensions;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DDCImprover.Core.PreBlocks
{
    /// <summary>
    /// Moves phrases named "moveto###s###" or "moveR#", adding a new temporary beat at the destination timecode.
    /// </summary>
    internal sealed class PhraseMover : IProcessorBlock
    {
        private readonly IList<ImproverMessage> _statusMessages;
        private readonly IList<Ebeat> _addedBeats;

        public PhraseMover(IList<ImproverMessage> statusMessages, IList<Ebeat> addedBeats)
        {
            _statusMessages = statusMessages;
            _addedBeats = addedBeats;
        }

        private void MoveToParseFailure(int phraseTime, Action<string> Log)
        {
            string errorMessage = $"Unable to read time for 'moveto' phrase at {phraseTime.TimeToString()}. (Usage examples: moveto5m10s200, moveto10s520)";
            _statusMessages.Add(new ImproverMessage(errorMessage, MessageType.Warning));
            Log(errorMessage);
        }

        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            var phrasesToMove = arrangement.Phrases
                 .Where(p => p.Name.StartsWith("moveto", StringComparison.OrdinalIgnoreCase)
                          || p.Name.StartsWith("moveR", StringComparison.OrdinalIgnoreCase))
                 .ToList();

            if (phrasesToMove.Count > 0)
                Log("Processing 'move' phrases:");
            else
                return;

            foreach (var phraseToMove in phrasesToMove)
            {
                // Find phrase iterations by matching phrase index
                int phraseId = arrangement.Phrases.IndexOf(phraseToMove);
                foreach (var phraseIterationToMove in arrangement.PhraseIterations.Where(pi => pi.PhraseId == phraseId))
                {
                    int phraseTime = phraseIterationToMove.Time;
                    int movetoTime;
                    string phraseToMoveName = phraseToMove.Name;

                    // Relative phrase moving right
                    if (phraseToMoveName.StartsWith("moveR", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(phraseToMoveName.Substring("moveR".Length), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out int moveRightBy))
                        {
                            var level = arrangement.Levels[phraseToMove.MaxDifficulty];
                            var noteTimes = level.Notes
                                .Where(n => n.Time >= phraseTime)
                                .Select(n => n.Time)
                                .Distinct() // Notes on the same timecode (e.g. split chords) count as one
                                .Take(moveRightBy);

                            var chordTimes = level.Chords
                                .Where(c => c.Time >= phraseTime)
                                .Select(c => c.Time)
                                .Distinct()
                                .Take(moveRightBy);

                            var noteAndChordTimes = noteTimes.Concat(chordTimes).OrderBy(time => time);

                            movetoTime = noteAndChordTimes.Skip(moveRightBy - 1).First();
                        }
                        else
                        {
                            string errorMessage = $"Unable to read value for 'moveR' phrase at {phraseTime.TimeToString()}. (Usage example: moveR2)";
                            _statusMessages.Add(new ImproverMessage(errorMessage, MessageType.Warning));
                            Log(errorMessage);

                            continue;
                        }
                    }
                    else // Parse the absolute time to move to from the phrase name
                    {
                        int? parsedTime = TimeParser.Parse(phraseToMoveName);
                        if (parsedTime.HasValue)
                        {
                            movetoTime = parsedTime.Value;
                        }
                        else
                        {
                            MoveToParseFailure(phraseTime, Log);
                            continue;
                        }
                    }

                    // Check if anchor(s) should be moved
                    foreach (var level in arrangement.Levels)
                    {
                        if (level.Difficulty > phraseToMove.MaxDifficulty)
                            break;

                        var anchors = level.Anchors;
                        int originalAnchorIndex = anchors.FindIndexByTime(phraseTime);
                        int movetoAnchorIndex = anchors.FindIndexByTime(movetoTime);

                        // If there is an anchor at the original position, but not at the new position, move it
                        if (originalAnchorIndex != -1 && movetoAnchorIndex == -1)
                        {
                            var originalAnchor = anchors[originalAnchorIndex];
                            anchors.Insert(originalAnchorIndex + 1, new Anchor(originalAnchor.Fret, movetoTime, originalAnchor.Width));

                            // Remove anchor at original phrase position if no note or chord present
                            if (level.Notes.FindIndexByTime(phraseTime) == -1
                               && level.Chords.FindIndexByTime(phraseTime) == -1)
                            {
                                anchors.RemoveAt(originalAnchorIndex);
                                Log($"--Moved anchor from {phraseTime.TimeToString()} to {movetoTime.TimeToString()}");
                            }
                            else
                            {
                                Log($"--Added anchor at {movetoTime.TimeToString()}");
                            }
                        }
                    }

                    // Move phraseIteration
                    phraseIterationToMove.Time = movetoTime;

                    // Move section (if present)
                    var sectionToMove = arrangement.Sections.FindByTime(phraseTime);
                    if (sectionToMove != null)
                    {
                        sectionToMove.Time = movetoTime;
                        Log($"--Moved phrase and section from {phraseTime.TimeToString()} to {movetoTime.TimeToString()}");
                    }
                    else
                    {
                        Log($"--Moved phrase from {phraseTime.TimeToString()} to {movetoTime.TimeToString()}");
                    }

                    // Add new temporary beat
                    var beatToAdd = new Ebeat(movetoTime, XMLProcessor.TempMeasureNumber);

                    var insertIndex = arrangement.Ebeats.FindIndex(b => b.Time > movetoTime);
                    arrangement.Ebeats.Insert(insertIndex, beatToAdd);

                    // Set the beat for later removal
                    _addedBeats.Add(beatToAdd);
                }
            }
        }
    }
}
