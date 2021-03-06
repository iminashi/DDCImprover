﻿using Rocksmith2014.XML;
using Rocksmith2014.XML.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DDCImprover.Core
{
    internal sealed class ArrangementChecker
    {
        private static readonly string[] StringNames =
        {
            "Low E",
            "A",
            "D",
            "G",
            "B",
            "High E"
        };

        private bool HasNoguitarSections { get; }
        private bool HasToneChanges { get; }

        private readonly Section[] noguitarSections;
        private readonly InstrumentalArrangement arrangement;
        private readonly IList<ImproverMessage> statusMessages;
        private readonly Action<string> Log;

        internal ArrangementChecker(InstrumentalArrangement arrangement, IList<ImproverMessage> statusMessages, Action<string> logAction)
        {
            this.arrangement = arrangement;
            this.statusMessages = statusMessages;
            Log = logAction;

            HasToneChanges = arrangement.Tones.Changes?.Count > 0;

            noguitarSections = arrangement.Sections.SkipLast().Where(s => s.Name == "noguitar").ToArray();
            HasNoguitarSections = noguitarSections.Length != 0;
        }

        public void RunAllChecks()
        {
            CheckCrowdEventPlacement();

            foreach (var level in arrangement.Levels)
            {
                CheckNotes(level);
                CheckChords(level);
                CheckHandshapes(level, arrangement.ChordTemplates);
                CheckAnchors(level);
            }
        }

        private void AddIssue(string message, int timeCode)
        {
            statusMessages.Add(new ImproverMessage(message, MessageType.Issue, timeCode));
            Log("Issue found: " + message);
        }

        internal void CheckCrowdEventPlacement()
        {
            var events = arrangement.Events;

            int? introApplauseStart = events.Find(e => e.Code == "E3")?.Time;
            int? applauseEnd = events.Find(e => e.Code == "E13")?.Time;
            Regex crowdSpeedRegex = new Regex("e[0-2]$");

            if (introApplauseStart is not null && applauseEnd is not null)
            {
                var badlyPlacedEvents =
                    from ev in events
                    where ev.Time >= introApplauseStart && ev.Time <= applauseEnd
                          && crowdSpeedRegex.IsMatch(ev.Code)
                    select ev;

                foreach (var ev in badlyPlacedEvents)
                {
                    AddIssue($"Crowd tempo event ({ev.Code}) between applause events at {ev.Time.TimeToString()}.", ev.Time);
                }
            }

            if (introApplauseStart is not null)
            {
                if (applauseEnd is null)
                {
                    AddIssue($"There is an intro applause event (E3) at {introApplauseStart.Value.TimeToString()} without an end event (E13).", introApplauseStart.Value);
                }
                else
                {
                    int startIndex = events.FindIndexByTime(introApplauseStart.Value);
                    for (int i = startIndex + 1; i < events.Count; i++)
                    {
                        var @event = events[i];
                        if ((@event.Code == "E3" || @event.Code == "D3") && @event.Time != introApplauseStart.Value)
                        {
                            AddIssue($"Expected an end applause event (E13) after intro applause event (E3), instead found {@event.Code}.", @event.Time);
                        }
                        else if (@event.Code == "E13")
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void CheckLinkNext(Note note, int currentIndex, Level level)
        {
            var notes = level.Notes;

            // Check if the note is linked to a chord
            if (level.Chords.Exists(c => c.Time == (note.Time + note.Sustain)
                && c.ChordNotes?.Exists(cn => cn.String == note.String) == true))
            {
                AddIssue($"Note incorrectly linked to a chord at {note.Time.TimeToString()}.", note.Time);
                return;
            }

            int nextNoteIndex = (currentIndex == -1) ?
                notes.FindIndex(n => n.Time > note.Time && n.String == note.String) :
                notes.FindIndex(currentIndex + 1, n => n.String == note.String);

            if (nextNoteIndex == -1)
            {
                AddIssue($"Unable to find next note for LinkNext note at {note.Time.TimeToString()}.", note.Time);
            }
            else
            {
                var nextNote = notes[nextNoteIndex];

                // Check if frets match
                if (note.Fret != nextNote.Fret)
                {
                    sbyte slideTo = note.SlideTo;
                    if (slideTo == -1)
                        slideTo = note.SlideUnpitchTo;

                    if (slideTo != nextNote.Fret)
                    {
                        int noteEndTime = note.Time + note.Sustain;
                        if (nextNote.Time - noteEndTime > 1) // EOF can add linknext to notes that shouldn't have it
                        {
                            AddIssue($"Incorrect LinkNext status on note at {note.Time.TimeToString()}, {StringNames[note.String]} string.", note.Time);
                        }
                        else
                        {
                            AddIssue($"LinkNext fret mismatch at {note.Time.TimeToString()}.", note.Time);
                        }
                    }
                } // Check if bendValues match
                else if (note.IsBend && note.BendValues is not null)
                {
                    float thisNoteLastBendValue = note.BendValues.Last().Step;
                    float nextNoteFirstBendValue = 0f;
                    // If the next note has bend values and the first one is at the same timecode as the note, compare to that bend value
                    if (nextNote.BendValues?.Count > 0 && nextNote.Time == nextNote.BendValues[0].Time)
                        nextNoteFirstBendValue = nextNote.BendValues[0].Step;

                    if (thisNoteLastBendValue != nextNoteFirstBendValue)
                    {
                        AddIssue($"LinkNext bend mismatch at {note.Time.TimeToString()}.", note.Time);
                    }
                }
            }
        }

        private bool IsInsideNoguitarSection(int noteTime)
        {
            foreach (var ngSection in noguitarSections)
            {
                int nextIndex = arrangement.Sections.IndexOf(ngSection) + 1;
                if (nextIndex >= arrangement.Sections.Count)
                    break;

                int startTime = ngSection.Time;
                int endTime = arrangement.Sections[nextIndex].Time;

                if (noteTime >= startTime && noteTime < endTime)
                    return true;
            }

            return false;
        }

        internal void CheckNotes(Level level)
        {
            for (int i = 0; i < level.Notes.Count; i++)
            {
                var note = level.Notes[i];

                // Check for notes with LinkNext and unpitched slide
                if(note.IsLinkNext && note.IsUnpitchedSlide)
                {
                    AddIssue($"Unpitched slide note with LinkNext at {note.Time.TimeToString()}.", note.Time);
                }

                // Check for notes with both harmonic and pinch harmonic
                if (note.IsHarmonic && note.IsPinchHarmonic)
                {
                    AddIssue($"Note set as both harmonic and pinch harmonic at {note.Time.TimeToString()}.", note.Time);
                }

                // Check 23rd and 24th fret notes without ignore
                if (note.Fret >= 23 && !note.IsIgnore)
                {
                    AddIssue($"Note on {note.Fret}{(note.Fret == 23 ? "rd" : "th")} fret without ignore status at {note.Time.TimeToString()}.", note.Time);
                }

                // Check 7th fret harmonic notes with sustain (that are not set as ignore)
                if (note.Fret == 7 && note.IsHarmonic && note.Sustain > 0 && !note.IsIgnore)
                {
                    AddIssue($"7th fret harmonic note with sustain at {note.Time.TimeToString()}.", note.Time);
                }

                // Check missing bendValues
                if (note.IsBend)
                {
                    int nonZeroBendValue = note.BendValues?.FindIndex(bv => bv.Step != 0.0f) ?? -1;
                    if (nonZeroBendValue == -1)
                    {
                        AddIssue($"Note missing a bend value at {note.Time.TimeToString()}.", note.Time);
                    }
                }

                // Check tone change placement
                if (HasToneChanges && arrangement.Tones.Changes!.Exists(t => t.Time == note.Time))
                {
                    AddIssue($"Tone change occurs on a note at {note.Time.TimeToString()}.", note.Time);
                }

                // Check linkNext notes
                if (note.IsLinkNext)
                {
                    CheckLinkNext(note, i, level);
                }

                // Check for notes inside noguitar sections
                if (HasNoguitarSections && IsInsideNoguitarSection(note.Time))
                {
                    AddIssue($"Note inside noguitar section at {note.Time.TimeToString()}.", note.Time);
                }
            }
        }

        internal void CheckChords(Level level)
        {
            for (int i = 0; i < level.Chords.Count; i++)
            {
                var chord = level.Chords[i];
                var chordNotes = chord.ChordNotes;

                if (chordNotes is not null)
                {
                    // Check for inconsistent chord note sustains
                    if(!chordNotes.TrueForAll(cn => cn.Sustain == chordNotes[0].Sustain))
                    {
                        AddIssue($"Chord with varying chord note sustains at {chord.Time.TimeToString()}.", chord.Time);
                    }

                    // Check 7th fret harmonic notes with sustain (and without ignore)
                    if (!chord.IsIgnore && chordNotes.Any(cn => cn.Sustain > 0 && cn.Fret == 7 && cn.IsHarmonic))
                    {
                        AddIssue($"7th fret harmonic note with sustain at {chord.Time.TimeToString()}.", chord.Time);
                    }

                    // Check for notes with LinkNext and unpitched slide
                    if (chordNotes.Any(cn => cn.IsLinkNext && cn.IsUnpitchedSlide))
                    {
                        AddIssue($"Chord note set as unpitched slide note with LinkNext at {chord.Time.TimeToString()}.", chord.Time);
                    }

                    // Check for notes with both harmonic and pinch harmonic
                    if (chordNotes.Any(cn => cn.IsHarmonic && cn.IsPinchHarmonic))
                    {
                        AddIssue($"Chord note set as both harmonic and pinch harmonic at {chord.Time.TimeToString()}.", chord.Time);
                    }

                    // Check 23rd and 24th fret chords without ignore
                    if (chordNotes.TrueForAll(cn => cn.Fret >= 23) && !chord.IsIgnore)
                    {
                        AddIssue($"Chord on 23rd/24th fret without ignore status at {chord.Time.TimeToString()}.", chord.Time);
                    }

                    foreach (var cn in chordNotes)
                    {
                        if (cn.IsLinkNext)
                            CheckLinkNext(cn, -1, level);
                    }
                }

                // Check tone change placement
                if (HasToneChanges && arrangement.Tones.Changes!.Exists(t => t.Time == chord.Time))
                {
                    AddIssue($"Tone change occurs on a chord at {chord.Time.TimeToString()}.", chord.Time);
                }

                // Check chords at the end of handshape (no handshape sustain)
                var handShape = level.HandShapes.Find(hs => hs.ChordId == chord.ChordId && chord.Time >= hs.StartTime && chord.Time <= hs.EndTime);

                if (handShape?.EndTime - chord.Time <= 5)
                {
                    AddIssue($"Chord without handshape sustain at {chord.Time.TimeToString()}.", chord.Time);
                }

                // Check for chords inside noguitar sections
                if (HasNoguitarSections && IsInsideNoguitarSection(chord.Time))
                {
                    AddIssue($"Chord inside noguitar section at {chord.Time.TimeToString()}.", chord.Time);
                }
            }
        }

        internal void CheckHandshapes(Level level, List<ChordTemplate> chordTemplates)
        {
            var handShapes = level.HandShapes;
            var anchors = level.Anchors;

            for (int i = 0; i < handShapes.Count; i++)
            {
                // Check anchor position relative to handshape fingering
                HandShape handShape = handShapes[i];
                HandShape? previous = (i == 0) ? null : handShapes[i - 1];
                HandShape? next = (i == handShapes.Count - 1) ? null : handShapes[i + 1];

                var activeAnchor = anchors.Last(a => a.Time <= handShape.StartTime);
                var chordTemplate = chordTemplates[handShape.ChordId];

                // Check only handshapes that do not use the 1st finger
                if (!chordTemplate.Fingers.Any(f => f == 1))
                {
                    bool chordOK = true;

                    for (int j = 0; j < 6; j++)
                    {
                        if (chordTemplate.Frets[j] == activeAnchor.Fret && chordTemplate.Fingers[j] != -1)
                        {
                            chordOK = false;
                        }
                    }

                    if (!chordOK)
                    {
                        if (previous is not null && IsSameAnchorWith1stFinger(previous, activeAnchor))
                        {
                            continue;
                        }

                        if (next is not null && IsSameAnchorWith1stFinger(next, activeAnchor))
                        {
                            continue;
                        }

                        AddIssue($"Handshape fingering does not match anchor position at {handShape.StartTime.TimeToString()}.", handShape.StartTime);
                    }
                }
            }

            // Logic to weed out some false positives
            bool IsSameAnchorWith1stFinger(HandShape neighbour, Anchor activeAnchor)
            {
                var neighbourAnchor = anchors.Last(a => a.Time <= neighbour.StartTime);
                var neighbourTemplate = chordTemplates[neighbour.ChordId];

                return neighbourTemplate.Fingers.Any(f => f == 1) && neighbourAnchor == activeAnchor;
            }
        }

        // Looks for anchors that are very close to a note but not exactly on a note
        internal void CheckAnchors(Level level)
        {
            var notes = level.Notes;
            var chords = level.Chords;

            int noteIndex = 0;
            int chordIndex = 0;

            foreach (var anchor in level.Anchors)
            {
                int closeNoteTime = 0;
                bool closeNoteFound = false;

                if (notes.Count > 0 && noteIndex < notes.Count)
                {
                    while (noteIndex < notes.Count)
                    {
                        if (Math.Abs(notes[noteIndex].Time - anchor.Time) <= 5)
                        {
                            closeNoteFound = true;
                            closeNoteTime = notes[noteIndex].Time;
                            break;
                        }

                        if (notes[noteIndex].Time > anchor.Time)
                            break;

                        noteIndex++;
                    }

                    // Check if it is an exact match
                    if (closeNoteFound && closeNoteTime == anchor.Time)
                        continue;
                }

                if (chords.Count > 0 && chordIndex < chords.Count)
                {
                    while (chordIndex < chords.Count)
                    {
                        if (Math.Abs(chords[chordIndex].Time - anchor.Time) <= 5)
                        {
                            closeNoteFound = true;
                            closeNoteTime = chords[chordIndex].Time;
                            break;
                        }

                        if (chords[chordIndex].Time > anchor.Time)
                            break;

                        chordIndex++;
                    }

                    // Check if it is an exact match
                    if (closeNoteFound && closeNoteTime == anchor.Time)
                        continue;
                }

                if (closeNoteFound)
                {
                    int distanceMs = anchor.Time - closeNoteTime;
                    string message = $"Anchor not on a note at {anchor.Time.TimeToString()}. Distance to closest note: {distanceMs} ms.";
                    AddIssue(message, anchor.Time);
                }
            }
        }
    }
}
