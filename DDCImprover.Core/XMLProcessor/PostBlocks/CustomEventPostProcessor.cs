using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicData;
using Rocksmith2014Xml;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Postprocesses custom events. Available:
    /// "so"
    /// </summary>
    internal sealed class CustomEventPostProcessor : IProcessorBlock
    {
        private readonly IList<ImproverMessage> _statusMessages;

        public CustomEventPostProcessor(IList<ImproverMessage> statusMessages)
        {
            _statusMessages = statusMessages;
        }

        public void Apply(RS2014Song song, Action<string> Log)
        {
            var events = song.Events;

            var removeBeatsEvent = events.FirstOrDefault(ev => ev.Code.Equals("removebeats", StringComparison.OrdinalIgnoreCase));
            if (removeBeatsEvent != null)
            {
                song.Ebeats.RemoveAll(b => b.Time >= removeBeatsEvent.Time);

                Log($"removebeats event found: Removed beats from {removeBeatsEvent.Time.TimeToString()} onward.");

                events.Remove(removeBeatsEvent);
            }

            var slideoutEvents = events.Where(ev => ev.Code.StartsWith("so", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var slideEvent in slideoutEvents)
            {
                float? parsedTime = TimeParser.Parse(slideEvent.Code);
                float slideTime = parsedTime ?? slideEvent.Time;

                // Find the max level for the phrase the slide is in
                var phraseIter = song.PhraseIterations.Last(pi => pi.Time <= slideTime);
                int diff = song.Phrases[phraseIter.PhraseId].MaxDifficulty;
                var level = song.Levels[diff];

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

                if (noteIndex != -1) // These are notes that follow a linknext chord
                {
                    do
                    {
                        var note = level.Notes[noteIndex];
                        if (note.IsUnpitchedSlide)
                            notes.Add(note);
                        noteIndex++;
                    } while (noteIndex < level.Notes.Count && Utils.TimeEqualToMilliseconds(level.Notes[noteIndex].Time, slideTime));

                    // Find the chord that is linked to the slide, its template and handshape
                    var linkNextChord = level.Chords.Last(c => c.Time < slideTime);
                    var linkNextChordHs = level.HandShapes.FirstOrDefault(hs => Utils.TimeEqualToMilliseconds(hs.StartTime, linkNextChord.Time));
                    originalChordTemplate = song.ChordTemplates[linkNextChord.ChordId];

                    // Shorten handshapes that EOF has set to include the slide out
                    if (linkNextChordHs != null && linkNextChordHs.EndTime > linkNextChord.Time + linkNextChord.ChordNotes[0].Sustain)
                    {
                        linkNextChordHs.EndTime = (float)Math.Round(
                            linkNextChord.Time + linkNextChord.ChordNotes[0].Sustain,
                            3,
                            MidpointRounding.AwayFromZero);
                    }
                }
                else // It is a normal chord with unpitched slide out
                {
                    var chord = level.Chords[chordIndex];
                    notes.AddRange(chord.ChordNotes.Where(cn => cn.IsUnpitchedSlide));

                    originalChordTemplate = song.ChordTemplates[chord.ChordId];

                    // The length of the handshape likely needs to be shortened
                    var chordHs = level.HandShapes.FirstOrDefault(hs => Utils.TimeEqualToMilliseconds(hs.StartTime, chord.Time));
                    chordHs.EndTime = (float)Math.Round(
                        chordHs.StartTime + ((chordHs.EndTime - chordHs.StartTime) / 3),
                        3,
                        MidpointRounding.AwayFromZero);
                }

                if (notes.Count == 0)
                {
                    string error = $"Invalid SlideOut event at {slideEvent.Time.TimeToString()}";
                    _statusMessages.Add(new ImproverMessage(error, MessageType.Error, slideEvent.Time));
                    Log(error);
                    continue;
                }

                // Create a new handshape at the slide end
                float endTime = notes[0].Time + notes[0].Sustain;
                float startTime = endTime - Math.Min(notes[0].Sustain / 3, 250f);
                int chordId = song.ChordTemplates.Count;
                level.HandShapes.InsertByTime(new HandShape(chordId, startTime, endTime));

                // Create a new chordtemplate for the handshape
                var soChordTemplate = new ChordTemplate();
                foreach (var note in notes)
                {
                    soChordTemplate.Frets[note.String] = note.SlideUnpitchTo;
                    soChordTemplate.Fingers[note.String] = originalChordTemplate.Fingers[note.String];
                }
                song.ChordTemplates.Add(soChordTemplate);

                Log($"Processed SlideOut event at {slideEvent.Time.TimeToString()}");
            }

            events.Remove(slideoutEvents);
        }
    }
}
