using Rocksmith2014Xml;
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

        private readonly bool songHasNoguitarSections;
        private readonly bool songHasToneChanges;
        private readonly Section[] noguitarSections;
        private readonly RS2014Song song;
        private readonly IList<ImproverMessage> statusMessages;
        private readonly Action<string> Log;

        internal ArrangementChecker(RS2014Song song, IList<ImproverMessage> statusMessages, Action<string> logAction)
        {
            this.song = song;
            this.statusMessages = statusMessages;
            Log = logAction;

            songHasToneChanges = song.Tones?.Count > 0;

            noguitarSections = song.Sections.SkipLast().Where(s => s.Name == "noguitar").ToArray();
            songHasNoguitarSections = noguitarSections.Length != 0;
        }

        private void AddIssue(string message, float timeCode)
        {
            statusMessages.Add(new ImproverMessage(message, MessageType.Issue, timeCode));
            Log("Issue found: " + message);
        }

        internal void CheckCrowdEventPlacement(EventCollection events)
        {
            float? introApplauseStart = events.FirstOrDefault(e => e.Code == "E3")?.Time;
            float? applauseEnd = events.FirstOrDefault(e => e.Code == "E13")?.Time;
            Regex crowdSpeedRegex = new Regex("e[0-2]$");

            if (introApplauseStart != null && applauseEnd != null)
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

            if (introApplauseStart != null)
            {
                if (applauseEnd == null)
                {
                    AddIssue($"There is an intro applause event (E3) at {introApplauseStart.Value.TimeToString()} without an end event (E13).", introApplauseStart.Value);
                }
                else
                {
                    int startIndex = events.FindIndexByTime(introApplauseStart.Value);
                    for (int i = startIndex + 1; i < events.Count; i++)
                    {
                        var @event = events[i];
                        if (@event.Code == "E3" || @event.Code == "D3")
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

        private void CheckLinkNext(Note note, int currentIndex, NoteCollection notes)
        {
            int nextNoteIndex = currentIndex == -1 ?
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
                        float noteEndTime = note.Time + note.Sustain;
                        if (nextNote.Time - noteEndTime > 0.001f) // EOF can add linknext to notes that shouldn't have it
                        {
                            AddIssue($"Incorrect LinkNext status on note at {note.Time.TimeToString()}, {StringNames[note.String]} string.", note.Time);
                        }
                        else
                        {
                            AddIssue($"LinkNext fret mismatch at {note.Time.TimeToString()}.", note.Time);
                        }
                    }
                } // Check if bendValues match
                else if (note.IsBend)
                {
                    float thisNoteLastBendValue = note.BendValues.Last().Step;
                    float nextNoteFirstBendValue = nextNote.BendValues?[0].Step ?? 0f;

                    if (thisNoteLastBendValue != nextNoteFirstBendValue)
                    {
                        AddIssue($"LinkNext bend mismatch at {note.Time.TimeToString()}.", note.Time);
                    }
                }
            }
        }

        private bool IsInsideNoguitarSection(float noteTime)
        {
            foreach (var ngSection in noguitarSections)
            {
                int nextIndex = song.Sections.IndexOf(ngSection) + 1;
                if (nextIndex >= song.Sections.Count)
                    break;

                float startTime = ngSection.Time;
                float endTime = song.Sections[nextIndex].Time;

                if (noteTime >= startTime && noteTime < endTime)
                    return true;
            }

            return false;
        }

        internal void CheckNotes(NoteCollection notes)
        {
            for (int i = 0; i < notes.Count; i++)
            {
                var note = notes[i];

                // Check for notes with both harmonic and pinch harmonic
                if (note.IsHarmonic && note.IsHarmonicPinch)
                {
                    AddIssue($"Note set as both harmonic and pinch harmonic at {note.Time.TimeToString()}.", note.Time);
                }

                // Check 23rd and 24th fret notes without ignore
                if (note.Fret >= 23 && !note.IsIgnore)
                {
                    AddIssue($"Note on {note.Fret}{(note.Fret == 23 ? "rd" : "th")} fret without ignore status at {note.Time.TimeToString()}.", note.Time);
                }

                // Check 7th fret harmonic notes with sustain (that are not set as ignore)
                if (note.Fret == 7 && note.IsHarmonic && note.Sustain > 0.0f && !note.IsIgnore)
                {
                    AddIssue($"7th fret harmonic note with sustain at {note.Time.TimeToString()}.", note.Time);
                }

                // Check missing bendValues
                if (note.IsBend)
                {
                    int nonZeroBendValue = note.BendValues.FindIndex(bv => bv.Step != 0.0f);
                    if (nonZeroBendValue == -1)
                    {
                        AddIssue($"Note missing a bend value at {note.Time.TimeToString()}.", note.Time);
                    }
                }

                // Check tone change placement
                if (songHasToneChanges && song.Tones.Exists(t => Utils.TimeEqualToMilliseconds(t.Time, note.Time)))
                {
                    AddIssue($"Tone change occurs on a note at {note.Time.TimeToString()}.", note.Time);
                }

                // Check linkNext notes
                if (note.IsLinkNext)
                {
                    CheckLinkNext(note, i, notes);
                }

                // Check for notes inside noguitar sections
                if (songHasNoguitarSections && IsInsideNoguitarSection(note.Time))
                {
                    AddIssue($"Note inside noguitar section at {note.Time.TimeToString()}.", note.Time);
                }
            }
        }

        internal void CheckChords(ChordCollection chords, NoteCollection notes, HandShapeCollection handshapes)
        {
            for (int i = 0; i < chords.Count; i++)
            {
                var chord = chords[i];
                var chordNotes = chord.ChordNotes;

                if (chordNotes != null)
                {
                    // Check 7th fret harmonic notes with sustain (and without ignore)
                    if (!chord.IsIgnore && chordNotes.Any(cn => cn.Sustain > 0f && cn.Fret == 7 && cn.IsHarmonic))
                    {
                        AddIssue($"7th fret harmonic note with sustain at {chord.Time.TimeToString()}.", chord.Time);
                    }

                    // Check for notes with both harmonic and pinch harmonic
                    if (chordNotes.Any(cn => cn.IsHarmonic && cn.IsHarmonicPinch))
                    {
                        AddIssue($"Chord note set as both harmonic and pinch harmonic at {chord.Time.TimeToString()}.", chord.Time);
                    }

                    // Check 23rd and 24th fret chords without ignore
                    if (chordNotes.All(cn => cn.Fret >= 23) && !chord.IsIgnore)
                    {
                        AddIssue($"Chord on 23rd/24th fret without ignore status at {chord.Time.TimeToString()}.", chord.Time);
                    }

                    foreach (var cn in chordNotes)
                    {
                        if (cn.IsLinkNext)
                            CheckLinkNext(cn, -1, notes);
                    }
                }

                // Check tone change placement
                if (songHasToneChanges && song.Tones.Exists(t => Utils.TimeEqualToMilliseconds(t.Time, chord.Time)))
                {
                    AddIssue($"Tone change occurs on a chord at {chord.Time.TimeToString()}.", chord.Time);
                }

                // Check chords at the end of handshape (no handshape sustain)
                var handShape = handshapes.Find(hs => hs.ChordId == chord.ChordId && chord.Time >= hs.StartTime && chord.Time <= hs.EndTime);

                if (handShape?.EndTime - chord.Time <= 0.005f)
                {
                    AddIssue($"Chord without handshape sustain at {chord.Time.TimeToString()}.", chord.Time);
                }

                // Check for chords inside noguitar sections
                if (songHasNoguitarSections && IsInsideNoguitarSection(chord.Time))
                {
                    AddIssue($"Chord inside noguitar section at {chord.Time.TimeToString()}.", chord.Time);
                }
            }
        }

        internal void CheckHandshapes(HandShapeCollection handShapes, ChordTemplateCollection chordTemplates, AnchorCollection anchors)
        {
            for (int i = 0; i < handShapes.Count; i++)
            {
                // Check anchor position relative to handshape fingering
                HandShape handShape = handShapes[i];
                HandShape previous = (i == 0) ? null : handShapes[i - 1];
                HandShape next = (i == handShapes.Count - 1) ? null : handShapes[i + 1];

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
                        if (previous != null && IsSameAnchorWith1stFinger(previous, activeAnchor))
                        {
                            continue;
                        }

                        if (next != null && IsSameAnchorWith1stFinger(next, activeAnchor))
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
        internal void CheckAnchors(AnchorCollection anchors, NoteCollection notes, ChordCollection chords)
        {
            int noteIndex = 0;
            int chordIndex = 0;

            foreach (var anchor in anchors)
            {
                float closeNoteTime = -1f;
                bool closeNoteFound = false;

                if (notes.Count > 0 && noteIndex < notes.Count)
                {
                    while (noteIndex < notes.Count)
                    {
                        if (Math.Abs(notes[noteIndex].Time - anchor.Time) <= 0.005)
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
                    if (closeNoteFound && Utils.TimeEqualToMilliseconds(closeNoteTime, anchor.Time))
                        continue;
                }

                if (!closeNoteFound && chords.Count > 0 && chordIndex < chords.Count)
                {
                    while (chordIndex < chords.Count)
                    {
                        if (Math.Abs(chords[chordIndex].Time - anchor.Time) <= 0.005)
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
                    if (closeNoteFound && Utils.TimeEqualToMilliseconds(closeNoteTime, anchor.Time))
                        continue;
                }

                // Ignore anchors that start a phrase
                if (!closeNoteFound && song.PhraseIterations.Any(pi => Utils.TimeEqualToMilliseconds(pi.Time, anchor.Time)))
                    continue;

                if (closeNoteFound)
                {
                    int distanceMs = (int)(Math.Round(anchor.Time - closeNoteTime, 3) * 1000);
                    string message = $"Anchor not on a note at {anchor.Time.TimeToString()}. Distance to closest note: {distanceMs} ms.";
                    AddIssue(message, anchor.Time);
                }
            }
        }
    }
}
