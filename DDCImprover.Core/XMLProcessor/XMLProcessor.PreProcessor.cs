using Rocksmith2014Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DDCImprover.Core
{
    public partial class XMLProcessor
    {
        internal class XMLPreProcessor
        {
            private const float IntroCrowdReactionDelay = 0.6f;
            private const float IntroApplauseLength = 2.5f;
            private const float OutroApplauseLength = 4.0f;
            private const float VenueFadeOutLength = 5.0f;

            private readonly XMLProcessor Parent;
            internal readonly RS2014Song Song;

            public PhraseIterationCollection PhraseIterations;

            public readonly int PhraseIterationCount;
            //public float FirstPhraseTime { get; private set; }
            public float LastPhraseTime;
            public float? FirstNGSectionTime { get; private set; }

            private static readonly string[] wrongCrowdEvents = { "E0", "E1", "E2" };

            internal XMLPreProcessor(XMLProcessor parent)
            {
                Parent = parent;

                Song = Parent.originalSong;
                PhraseIterations = Song.PhraseIterations;

                PhraseIterationCount = Song.PhraseIterations.Count;
                LastPhraseTime = Song.PhraseIterations[Song.PhraseIterations.Count - 1].Time;
            }

            private void Log(string str) => Parent.Log(str);

            internal void Process()
            {
                Log("-------------------- Preprocessing Started --------------------");

                FixEOFLinkNextChordTechNoteIssue();

                FixCrowdEvents();

                if (Preferences.AddCrowdEvents)
                    AddCrowdEvents();

                //if (Parent.IsNonDDFile)
                //    ApplyFixForFirstPhraseMoving();

                ProcessMovetoPhrases();

                if (Parent.isNonDDFile)
                    ApplyFixForWeakBeatPhraseMoving();

                CheckUnpitchedSlides();

                if (Preferences.RestoreFirstNoguitarSection && Parent.isNonDDFile)
                    GetFirstNGSectionTime();

                if (Preferences.RestoreNoguitarSectionAnchors && Parent.isNonDDFile)
                    StoreNGSectionAnchors();

                if (Preferences.AdjustHandshapes)
                    AdjustHandshapes();

                if (Preferences.FixChordNames)
                    ProcessFixOpenChord();

                if (Preferences.CheckXML)
                {
                    Lint();

                    if (Parent.StatusMessages.Count > 0)
                    {
                        Parent.StatusMessages.Sort();
                    }
                }

                GenerateToneEvents();
                SetRightHandOnTapNotes();

                ProcessCustomEvents();

#if DEBUG
                RemoveChordNoteIgnores();
#endif

                Log("------------------- Preprocessing Completed --------------------");
            }

            /// <summary>
            /// Processes custom events. Available:
            /// "removebeats", "w3"
            /// </summary>
            private void ProcessCustomEvents()
            {
                var events = Song.Events;

                var removeBeatsEvent = events.FirstOrDefault(ev => ev.Code == "removebeats");
                if (removeBeatsEvent != null)
                {
                    Song.Ebeats.RemoveAll(b => b.Time >= removeBeatsEvent.Time);

                    Log($"removebeats event found: Removed beats from {removeBeatsEvent.Time.TimeToString()} onward.");

                    events.Remove(removeBeatsEvent);
                }

                var width3events = events.Where(ev => ev.Code == "w3").ToList();
                foreach (var w3event in width3events)
                {
                    var modifiedAnchors = Song.Levels
                        .SelectMany(lvl => lvl.Anchors)
                        .Where(a => Utils.TimeEqualToMilliseconds(a.Time, w3event.Time));

                    foreach(var anchor in modifiedAnchors)
                    {
                        anchor.Width = 3.0f;
                        Log($"Changed width of anchor at {anchor.Time.TimeToString()} to 3.");
                    }

                    events.Remove(w3event);
                }
            }

            private void SetRightHandOnTapNotes()
            {
                int rightHandCount = 0;

                foreach (var note in Song.Levels.SelectMany(lev => lev.Notes))
                {
                    if (note.IsTap && note.RightHand == -1)
                    {
                        note.RightHand = 1;
                        rightHandCount++;
                    }
                }

                if (rightHandCount != 0)
                    Log($"Set 'righthand' to 1 on {rightHandCount} tap note{(rightHandCount == 1 ? "" : "s")}.");
            }

            private void RemoveChordNoteIgnores()
            {
                int removeCount = 0;

                foreach (var chord in Song.Levels.SelectMany(lev => lev.Chords))
                {
                    if (chord.ChordNotes is null)
                        continue;

                    foreach (var chordNote in chord.ChordNotes)
                    {
                        if (chordNote.IsIgnore)
                        {
                            chordNote.IsIgnore = false;
                            removeCount++;
                        }
                    }
                }

                if (removeCount != 0)
                    Log($"Removed unnecessary ignore from {removeCount} chordNote{(removeCount == 1 ? "" : "s")}.");
            }

            private static float GetMinTime(IHasTimeCode first, IHasTimeCode second)
            {
                if (first is null)
                {
                    return second.Time;
                }
                else if (second is null)
                {
                    return first.Time;
                }
                else
                {
                    return Math.Min(first.Time, second.Time);
                }
            }

            private void AddIntroApplauseEvent(EventCollection events)
            {
                Arrangement firstPhraseLevel;

                if (Parent.isNonDDFile)
                {
                    firstPhraseLevel = Song.Levels[0];
                }
                else
                {
                    // Find the first phrase that has difficulty levels
                    int firstPhraseId = Song.PhraseIterations
                        .First(pi => Song.Phrases[pi.PhraseId].MaxDifficulty > 0)
                        .PhraseId;
                    var firstPhrase = Song.Phrases[firstPhraseId];
                    firstPhraseLevel = Song.Levels[firstPhrase.MaxDifficulty];
                }

                Note firstNote = null;
                if (firstPhraseLevel.Notes.Count > 0)
                    firstNote = firstPhraseLevel.Notes[0];

                Chord firstChord = null;
                if (firstPhraseLevel.Chords.Count > 0)
                    firstChord = firstPhraseLevel.Chords[0];

                float applauseStartTime = (float)Math.Round(GetMinTime(firstNote, firstChord) + IntroCrowdReactionDelay, 3);
                float applauseEndTime = (float)Math.Round(applauseStartTime + IntroApplauseLength, 3);

                var startEvent = new Event("E3", applauseStartTime);
                var stopEvent = new Event("E13", applauseEndTime);

                events.InsertByTime(startEvent);
                events.InsertByTime(stopEvent);

                Log($"Added intro applause event (E3) at time: {applauseStartTime.TimeToString()}.");
            }

            private void AddOutroApplauseEvent(EventCollection events)
            {
                float audioEnd = Song.SongLength;

                float applauseStartTime = (float)Math.Round(audioEnd - VenueFadeOutLength - OutroApplauseLength, 3);

                var startEvent = new Event("D3", applauseStartTime);
                var stopEvent = new Event("E13", audioEnd);

                events.InsertByTime(startEvent);
                events.InsertByTime(stopEvent);

                Log($"Added outro applause event (D3) at time: {applauseStartTime.TimeToString()}.");
            }

            private void AddCrowdEvents()
            {
                if (Song.Events is null)
                    Song.Events = new EventCollection();

                var events = Song.Events;

                // Add initial crowd tempo event only if there are no other tempo events present
                if (!events.Any(ev => Regex.IsMatch(ev.Code, "e[0-2]$")))
                {
                    float averageTempo = Song.AverageTempo;
                    float startBeat = Song.StartBeat;

                    string crowdSpeed = (averageTempo < 90) ? "e0" :
                                        (averageTempo < 170) ? "e1" : "e2";

                    events.InsertByTime(new Event(crowdSpeed, startBeat));

                    Log($"Added initial crowd tempo event ({crowdSpeed}, song average tempo: {averageTempo.ToString("F3", NumberFormatInfo.InvariantInfo)})");
                }

                // Add intro applause
                if (!events.Any(ev => ev.Code == "E3"))
                {
                    AddIntroApplauseEvent(events);
                }

                // Add outro applause
                if (!events.Any(ev => ev.Code == "D3"))
                {
                    AddOutroApplauseEvent(events);
                }
            }

            private void FixCrowdEvents()
            {
                if (Song.Events?.Count > 0)
                {
                    foreach (var @event in Song.Events)
                    {
                        if (wrongCrowdEvents.Contains(@event.Code))
                        {
                            string correctEvent = @event.Code.ToLower();
                            Log($"Corrected wrong crowd event: {@event.Code} -> {correctEvent}");

                            @event.Code = correctEvent;
                        }
                    }
                }
            }

            /// <summary>
            /// Changes first beat measure number to "0" if the first phrase does not start at 0.000.
            /// </summary>
            /*private void ApplyFixForFirstPhraseMoving()
            {
                float firstBeatTime = Song.Ebeats[0].Time;

                if (firstBeatTime > 0.0)
                {
                    if (Song.Ebeats[0].Measure == 1)
                    {
                        Song.Ebeats[0].Measure = 0;
                    }

                    Log("Applied fix for DDC moving first beat.");
                }

                FirstPhraseTime = Song.PhraseIterations[0].Time;
            }*/

            /// <summary>
            /// Changes weak beats that start a phrase into strong beats.
            /// </summary>
            private void ApplyFixForWeakBeatPhraseMoving()
            {
                var weakBeatsWithPhrases =
                    from ebeat in Song.Ebeats
                    join phraseIter in Song.PhraseIterations on ebeat.Time equals phraseIter.Time
                    where ebeat.Measure == -1
                    select ebeat;

                foreach (var beat in weakBeatsWithPhrases)
                {
                    beat.Measure = TempMeasureNumber;

                    Log($"Applied workaround for beat at {beat.Time.TimeToString()}.");
                }
            }

            private void GetFirstNGSectionTime()
            {
                var firstSection = Song.Sections[0];

                if (firstSection.Name == "noguitar")
                {
                    FirstNGSectionTime = firstSection.Time;
                }
            }

            private void MoveToParseFailure(float phraseTime)
            {
                string errorMessage = $"Unable to read time for 'moveto' phrase at {phraseTime.TimeToString()}. (Usage examples: moveto5m10s200, moveto10s520)";
                Parent.StatusMessages.Add(new ImproverMessage(errorMessage, MessageType.Warning));
                Log(errorMessage);
            }

            /// <summary>
            /// Moves phrases named "moveto###s###" or "moveR#", adding a new temporary beat to the destination timecode.
            /// </summary>
            private void ProcessMovetoPhrases()
            {
                var phrasesToMove = Song.Phrases
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
                    int phraseId = Song.Phrases.IndexOf(phraseToMove);
                    foreach (var phraseIterationToMove in Song.PhraseIterations.Where(pi => pi.PhraseId == phraseId))
                    {
                        float phraseTime = phraseIterationToMove.Time;
                        float movetoTime;
                        string phraseToMoveName = phraseToMove.Name;

                        // Relative phrase moving right
                        if (phraseToMoveName.StartsWith("moveR", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(phraseToMoveName.Substring("moveR".Length), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out int moveRightBy))
                            {
                                var level = Song.Levels[phraseToMove.MaxDifficulty];
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

                                var noteAndChordTimes = noteTimes.Concat(chordTimes).OrderBy(x => x);

                                movetoTime = noteAndChordTimes.Skip(moveRightBy - 1).First();
                            }
                            else
                            {
                                string errorMessage = $"Unable to read value for 'moveR' phrase at {phraseTime.TimeToString()}. (Usage example: moveR2)";
                                Parent.StatusMessages.Add(new ImproverMessage(errorMessage, MessageType.Warning));
                                Log(errorMessage);

                                continue;
                            }
                        }
                        else // Parse the absolute time to move to from the phrase name
                        {
                            float? parsedTime = TimeParser.Parse(phraseToMoveName);
                            if (parsedTime.HasValue)
                            {
                                movetoTime = parsedTime.Value;
                            }
                            else
                            {
                                MoveToParseFailure(phraseTime);
                                continue;
                            }
                        }

                        // Check if anchor(s) should be moved
                        foreach (var level in Song.Levels)
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
                        var sectionToMove = Song.Sections.FindByTime(phraseTime);
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
                        var beatToAdd = new Ebeat(movetoTime, TempMeasureNumber);

                        var insertIndex = Song.Ebeats.FindIndex(b => b.Time > movetoTime);
                        Song.Ebeats.Insert(insertIndex, beatToAdd);

                        // Set the beat for later removal
                        Parent.addedBeats.Add(beatToAdd);
                    }
                }
            }

            /// <summary>
            /// Fix for DDC removing anchors from the beginning of noguitar sections.
            /// </summary>
            private void StoreNGSectionAnchors()
            {
                float firstBeatTime = Song.Ebeats[0].Time;

                // Check for anchor at the beginning of the beatmap
                var firstBeatAnchor = Song.Levels[0].Anchors.FindByTime(firstBeatTime);
                if (firstBeatAnchor != null)
                {
                    Log($"Stored the anchor found at the beginning of the beatmap at {firstBeatTime.TimeToString()}.");

                    Parent.NGAnchors.Add(firstBeatAnchor);
                }

                // Check NG sections
                var ngSectionAnchors =
                   from section in Song.Sections
                   join anchor in Song.Levels[0].Anchors on section.Time equals anchor.Time
                   where section.Name == "noguitar"
                   select anchor;

                foreach (var ngAnchor in ngSectionAnchors)
                {
                    Log($"Stored an anchor found at the beginning of a noguitar section at {ngAnchor.Time.TimeToString()}.");

                    Parent.NGAnchors.Add(ngAnchor);
                }
            }

            /// <summary>
            /// Set enables unpitchedSlides in arrangement properties if unpitched slides found.
            /// </summary>
            private void CheckUnpitchedSlides()
            {
                if (Song.ArrangementProperties.UnpitchedSlides == 1)
                    return;

                var notes = Song.Levels.SelectMany(l => l.Notes);

                if (notes.Any(n => n.IsUnpitchedSlide))
                {
                    Song.ArrangementProperties.UnpitchedSlides = 1;
                }
                else
                {
                    foreach (var chord in Song.Levels.SelectMany(l => l.Chords))
                    {
                        if (chord.ChordNotes is null)
                            continue;

                        if (chord.ChordNotes.Any(n => n.IsUnpitchedSlide))
                        {
                            Song.ArrangementProperties.UnpitchedSlides = 1;

                            break;
                        }
                    }
                }

                if (Song.ArrangementProperties.UnpitchedSlides == 1)
                    Log("Enabled unpitched slides in arrangement properties.");
            }

            /// <summary>
            /// Shortens the lengths of a handshapes that are too close to the next one.
            /// </summary>
            private void AdjustHandshapes()
            {
                foreach (var level in Song.Levels)
                {
                    var handShapes = level.HandShapes;

                    for (int i = 1; i < handShapes.Count; i++)
                    {
                        float followingStartTime = handShapes[i].StartTime;
                        float followingEndTime = handShapes[i].EndTime;

                        var precedingHandshape = handShapes[i - 1];
                        float precedingStartTime = precedingHandshape.StartTime;
                        float precedingEndTime = precedingHandshape.EndTime;

                        // Ignore nested handshapes
                        if (precedingEndTime >= followingEndTime || followingStartTime - precedingEndTime < -0.001f)
                        {
                            Log($"Skipped nested handshape starting at {precedingStartTime.TimeToString()}.");
                            continue;
                        }

                        int beat1Index = Song.Ebeats.FindIndex(b => b.Time > precedingEndTime);
                        var beat1 = Song.Ebeats[beat1Index - 1];
                        var beat2 = Song.Ebeats[beat1Index];

                        double note32nd = Math.Round((beat2.Time - beat1.Time) / 8, 3, MidpointRounding.AwayFromZero);
                        bool shortenBy16thNote = false;

                        // Check if chord that starts the handshape is a linknext slide
                        var startChord = level.Chords?.FindByTime(precedingStartTime);
                        if (startChord?.IsLinkNext == true && startChord?.ChordNotes?.Any(cn => cn.IsSlide) == true)
                        {
                            // Check if the handshape length is an 8th note or longer
                            if ((note32nd * 4) - (precedingEndTime - precedingStartTime) < 0.003)
                            {
                                shortenBy16thNote = true;
                            }
                        }

                        double minDistance = shortenBy16thNote ? note32nd * 2 : note32nd;

                        // Shorten the min. distance required for 32nd notes or smaller
                        if (precedingEndTime - precedingStartTime <= note32nd)
                            minDistance = Math.Round((beat2.Time - beat1.Time) / 12, 3, MidpointRounding.AwayFromZero);

                        double currentDistance = Math.Round(followingStartTime - precedingEndTime, 3);

                        if (currentDistance < minDistance)
                        {
                            double newEndTime = Math.Round(followingStartTime - minDistance, 3, MidpointRounding.AwayFromZero);
                            int safetyCount = 0;

                            // Shorten distance for very small note values
                            while (newEndTime <= precedingStartTime && ++safetyCount < 3)
                            {
                                minDistance = Math.Round(minDistance / 2, 3, MidpointRounding.AwayFromZero);
                                newEndTime = Math.Round(followingStartTime - minDistance, 3, MidpointRounding.AwayFromZero);
#if DEBUG
                                Log("Reduced handshape min. distance by half.");
#endif
                            }

                            handShapes[i - 1] = new HandShape(precedingHandshape.ChordId, precedingHandshape.StartTime, (float)newEndTime);

                            // Skip logging < 5ms adjustments
                            if (minDistance - currentDistance > 0.005)
                            {
                                string oldDistanceMs = ((int)(currentDistance * 1000)).ToString();
                                string newDistanceMs = ((int)(minDistance * 1000)).ToString();
                                var message = $"Adjusted the length of handshape starting at {precedingHandshape.StartTime.TimeToString()} (Distance: {oldDistanceMs}ms -> {newDistanceMs}ms)";
                                if (shortenBy16thNote)
                                    message += " (Chord slide)";

                                Log(message);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Generates tone events (tone_a etc.) for tone changes.
            /// </summary>
            private void GenerateToneEvents()
            {
                var toneChanges = Song.Tones;

                if (toneChanges is null || toneChanges.Count == 0 || Song.Events.Any(ev => ev.Code.StartsWith("tone_")))
                    return;

                var toneCodes = new Dictionary<string, string>();
                if (Song.ToneA != null)
                    toneCodes.Add(Song.ToneA, "tone_a");
                if (Song.ToneB != null)
                    toneCodes.Add(Song.ToneB, "tone_b");
                if (Song.ToneC != null)
                    toneCodes.Add(Song.ToneC, "tone_c");
                if (Song.ToneD != null)
                    toneCodes.Add(Song.ToneD, "tone_d");

                var toneEvents = from tone in toneChanges
                                 select new Event(toneCodes[tone.Name], tone.Time);

                var events = Song.Events;
                var newEvents = events.Union(toneEvents).OrderBy(e => e.Time).ToArray();

                Song.Events.Clear();
                Song.Events.AddRange(newEvents);

                Log("Generated tone events.");
            }

            /// <summary>
            /// Fix for EOF issue with chords that have LinkNext tech notes.
            /// </summary>
            private void FixEOFLinkNextChordTechNoteIssue()
            {
                foreach (var chord in Song.Levels.SelectMany(l => l.Chords))
                {
                    if (chord.ChordNotes is null)
                        continue;

                    if (chord.ChordNotes.Any(cn => cn.IsLinkNext) && !chord.IsLinkNext)
                    {
                        chord.IsLinkNext = true;
                        Log($"Added LinkNext to chord at {chord.Time.TimeToString()}");
                    }
                }
            }

            /// <summary>
            /// Processes FIXOPEN chord command.
            /// </summary>
            private void ProcessFixOpenChord()
            {
                var ChordTemplates = Song.ChordTemplates;
                for (int chordId = 0; chordId < ChordTemplates.Count; chordId++)
                {
                    var currentChordTemplate = ChordTemplates[chordId];
                    string chordName = currentChordTemplate.ChordName;

                    if (chordName.Contains("FIXOPEN"))
                    {
                        // Remove open notes from the chord template
                        for (int i = 0; i < 5; ++i)
                        {
                            if (currentChordTemplate.Frets[i] == 0)
                                currentChordTemplate.Frets[i] = -1;
                        }

                        foreach (var level in Song.Levels)
                        {
                            var chordsToFix = from chord in level.Chords
                                              where chord.ChordId == chordId
                                              select chord;

                            foreach (var chord in chordsToFix)
                            {
                                var chordNotesToRemove = chord.ChordNotes.Where(cn => cn.Fret == 0).ToArray();
                                // Store sustain of chord
                                double initSustain = chordNotesToRemove[0].Sustain;

                                foreach (var chordNote in chordNotesToRemove)
                                {
                                    double sustain = initSustain;

                                    var noteToRemove =
                                        (from note in level.Notes
                                         where note.Fret == 0
                                               && note.String == chordNote.String
                                               && note.Time > chord.Time
                                         select note).FirstOrDefault();

                                    while (noteToRemove != null)
                                    {
                                        sustain += noteToRemove.Sustain;

                                        if (noteToRemove.IsLinkNext)
                                        {
                                            level.Notes.Remove(noteToRemove);

                                            int nextIndex = level.Notes.FindIndex(n => n.Time > chordNote.Time && n.String == chordNote.String);
                                            noteToRemove = nextIndex == -1 ? null : level.Notes[nextIndex];
                                        }
                                        else
                                        {
                                            level.Notes.Remove(noteToRemove);
                                            break;
                                        }
                                    }

                                    // Add new note
                                    var newNote = new Note(chordNote)
                                    {
                                        Sustain = (float)Math.Round(sustain, 3, MidpointRounding.AwayFromZero),
                                        IsLinkNext = false
                                    };

                                    int insertIndex = level.Notes.FindIndex(n => n.Time > chord.Time);
                                    if (insertIndex != -1)
                                    {
                                        level.Notes.Insert(insertIndex, newNote);
                                    }
                                    else
                                    {
                                        // Should not be able to get here
                                        level.Notes.Insert(0, newNote);
                                    }

                                    // Remove open notes from chord
                                    chord.ChordNotes.Remove(chordNote);
                                }

                                // Move chord and handshape forward 20 ms (hack for DDC)
                                const float amountToMove = 0.020f;
                                var handShape = level.HandShapes.First(hs => Utils.TimeEqualToMilliseconds(hs.StartTime, chord.Time));
                                handShape.StartTime += amountToMove;
                                chord.Time += amountToMove;

                                foreach (var cn in chord.ChordNotes)
                                {
                                    cn.Time += amountToMove;

                                    // Reduce sustain by amount moved
                                    if (cn.Sustain != 0.0f)
                                    {
                                        cn.Sustain -= amountToMove;
                                    }
                                }

                                Log($"Applied open note sustain fix to chord at {chord.Time.TimeToString()}");
                            }
                        }

                        // Set correct name
                        currentChordTemplate.ChordName = currentChordTemplate.ChordName.Replace("FIXOPEN", "");
                        currentChordTemplate.DisplayName = currentChordTemplate.DisplayName.Replace("FIXOPEN", "");
                    }
                }
            }

            /// <summary>
            /// Checks the XML for problems.
            /// </summary>
            private void Lint()
            {
                var arrangementChecker = new ArrangementChecker(Song, Parent.StatusMessages, Log);
                arrangementChecker.RunAllChecks();

                /*// Check for duplicate notes
                var overlappingNotes =
                    from note in Song.Levels[0].Notes
                    group note by note.Time into noteGroup
                    where noteGroup.Count() > 1
                    select noteGroup;

                foreach (var grp in overlappingNotes)
                {
                    HashSet<sbyte> strings = new HashSet<sbyte>();
                    foreach (var note in grp)
                    {
                        if(!strings.Add(note.String))
                            AddIssue($"Duplicate note at {note.Time.TimeToString()}.", note.Time);
                    }
                }*/
            }
        }
    }
}