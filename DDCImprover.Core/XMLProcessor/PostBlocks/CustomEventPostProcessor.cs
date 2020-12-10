using Rocksmith2014.XML;
using Rocksmith2014.XML.Extensions;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Postprocesses custom events. Available:
    /// "so", "removebeats"
    /// </summary>
    internal sealed class CustomEventPostProcessor : IProcessorBlock
    {
        private readonly IList<ImproverMessage> _statusMessages;

        public CustomEventPostProcessor(IList<ImproverMessage> statusMessages)
        {
            _statusMessages = statusMessages;
        }

        public void Apply(InstrumentalArrangement arrangement, Action<string> Log)
        {
            var events = arrangement.Events;

            var removeBeatsEvent = events.Find(ev => ev.Code.Equals("removebeats", StringComparison.OrdinalIgnoreCase));
            if (!(removeBeatsEvent is null))
            {
                arrangement.Ebeats.RemoveAll(b => b.Time >= removeBeatsEvent.Time);

                Log($"removebeats event found: Removed beats from {removeBeatsEvent.Time.TimeToString()} onward.");

                events.Remove(removeBeatsEvent);
            }

            var slideoutEvents = events.Where(ev => ev.Code.StartsWith("so", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var slideEvent in slideoutEvents)
            {
                int slideTime = slideEvent.Time;

                // Find the max level for the phrase the slide is in
                var phraseIter = arrangement.PhraseIterations.Last(pi => pi.Time <= slideTime);
                int diff = arrangement.Phrases[phraseIter.PhraseId].MaxDifficulty;
                var level = arrangement.Levels[diff];

                // If a number was given after the event code, get the time of the chord or note that is right of the event by that number
                if (slideEvent.Code.Length > 2
                    && int.TryParse(slideEvent.Code.Substring(2), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out int rightBy))
                {
                    slideTime = TimeParser.FindTimeOfNthNoteFrom(level, slideTime, rightBy);
                }

                int noteIndex = level.Notes.FindIndexByTime(slideTime);
                int chordIndex = level.Chords.FindIndexByTime(slideTime);
                if (noteIndex == -1 && chordIndex == -1)
                {
                    string error = $"Could not find the notes or chord for SlideOut event at {slideEvent.Time.TimeToString()}";
                    _statusMessages.Add(new ImproverMessage(error, MessageType.Error, slideEvent.Time));
                    Log(error);
                    continue;
                }

                var notes = new List<Note>();
                ChordTemplate originalChordTemplate;

                if (noteIndex != -1) // These are notes that follow a LinkNext chord
                {
                    do
                    {
                        var note = level.Notes[noteIndex];
                        if (note.IsUnpitchedSlide)
                            notes.Add(note);
                        noteIndex++;
                    } while (noteIndex < level.Notes.Count && level.Notes[noteIndex].Time == slideTime);

                    // Find the chord that is linked to the slide, its template and handshape
                    var linkNextChord = level.Chords.Last(c => c.Time < slideTime);
                    var linkNextChordHs = level.HandShapes.Find(hs => hs.StartTime == linkNextChord.Time);
                    originalChordTemplate = arrangement.ChordTemplates[linkNextChord.ChordId];

                    // Shorten handshapes that EOF has set to include the slide out
                    // If chord notes is null here, there is an error in the XML file
                    if (linkNextChordHs is not null && linkNextChordHs.EndTime > linkNextChord.Time + linkNextChord.ChordNotes![0].Sustain)
                    {
                        linkNextChordHs.EndTime = linkNextChord.Time + linkNextChord.ChordNotes[0].Sustain;
                    }
                }
                else // It is a normal chord with unpitched slide out
                {
                    var chord = level.Chords[chordIndex];
                    if (chord.ChordNotes is null)
                    {
                        string error = $"Chord missing chord notes for SlideOut event at {slideEvent.Time.TimeToString()}";
                        _statusMessages.Add(new ImproverMessage(error, MessageType.Error, slideEvent.Time));
                        Log(error);
                        continue;
                    }

                    notes.AddRange(chord.ChordNotes.Where(cn => cn.IsUnpitchedSlide));

                    originalChordTemplate = arrangement.ChordTemplates[chord.ChordId];

                    // The length of the handshape likely needs to be shortened
                    var chordHs = level.HandShapes.Find(hs => hs.StartTime == chord.Time);
                    if (chordHs is not null)
                    {
                        chordHs.EndTime = chordHs.StartTime + ((chordHs.EndTime - chordHs.StartTime) / 3);
                    }
                }

                if (notes.Count == 0)
                {
                    string error = $"Invalid SlideOut event at {slideEvent.Time.TimeToString()}";
                    _statusMessages.Add(new ImproverMessage(error, MessageType.Error, slideEvent.Time));
                    Log(error);
                    continue;
                }

                // Create a new handshape at the slide end
                int endTime = notes[0].Time + notes[0].Sustain;
                int startTime = endTime - (notes[0].Sustain / 3);
                short chordId = (short)arrangement.ChordTemplates.Count;
                level.HandShapes.InsertByTime(new HandShape(chordId, startTime, endTime));

                // Create a new chord template for the handshape
                var soChordTemplate = new ChordTemplate();
                foreach (var note in notes)
                {
                    soChordTemplate.Frets[note.String] = note.SlideUnpitchTo;
                    soChordTemplate.Fingers[note.String] = originalChordTemplate.Fingers[note.String];
                }
                arrangement.ChordTemplates.Add(soChordTemplate);

                Log($"Processed SlideOut event at {slideEvent.Time.TimeToString()}, target at {slideTime.TimeToString()}.");
            }

            foreach (var ev in slideoutEvents)
            {
                events.Remove(ev);
            }
        }
    }
}
