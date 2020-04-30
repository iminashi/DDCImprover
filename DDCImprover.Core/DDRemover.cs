﻿using Rocksmith2014Xml;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DDCImprover.Core
{
    internal static class DDRemover
    {
        public static void RemoveDD(RS2014Song song, bool matchPhrasesToSections)
        {
            var trTrack = GenerateTranscriptionTrack(song);

            song.Levels.Clear();
            song.Levels.Add(trTrack);

            song.NewLinkedDiffs.Clear();

            if (matchPhrasesToSections)
            {
                song.Phrases.Clear();

                song.Phrases.Add(new Phrase { Name = "COUNT" });
                foreach (string sectionName in song.Sections.SkipLast().Select(s => s.Name).Distinct())
                {
                    song.Phrases.Add(new Phrase
                    {
                        Name = sectionName
                    });
                }
                song.Phrases.Add(new Phrase { Name = "END" });

                song.PhraseIterations.Clear();

                // Add COUNT phrase iteration
                song.PhraseIterations.Add(new PhraseIteration { Time = song.StartBeat, PhraseId = 0 });

                // Add phrase iterations
                foreach (Section section in song.Sections.SkipLast())
                {
                    song.PhraseIterations.Add(new PhraseIteration
                    {
                        Time = section.Time,
                        PhraseId = song.Phrases.FindIndex(p => p.Name == section.Name)
                    });
                }

                // Add END phrase iteration
                song.PhraseIterations.Add(new PhraseIteration
                {
                    Time = song.Sections.Last().Time,
                    PhraseId = song.Phrases.Count - 1
                });
            }
            else
            {
                foreach (var p in song.Phrases)
                {
                    p.MaxDifficulty = 0;
                }

                foreach (var pi in song.PhraseIterations)
                {
                    pi.HeroLevels = null;
                }
            }

            //TODO: Remove unused chord templates?

            AddComment(song);
        }

        private static Arrangement GenerateTranscriptionTrack(RS2014Song song)
        {
            var phrases = song.Phrases;
            var notes = new List<Note>();
            var chords = new List<Chord>();
            var handshapes = new List<HandShape>();
            var anchors = new List<Anchor>();

            // Ignore the last phrase iteration (END)
            for (int i = 0; i < song.PhraseIterations.Count - 1; i++)
            {
                var phraseIteration = song.PhraseIterations[i];
                int phraseId = phraseIteration.PhraseId;
                int maxDifficulty = phrases[phraseId].MaxDifficulty;

                float phraseStartTime = phraseIteration.Time;
                float phraseEndTime = song.PhraseIterations[i + 1].Time;
                var highestLevelForPhrase = song.Levels[maxDifficulty];

                var notesInPhraseIteration = highestLevelForPhrase.Notes
                    .Where(n => n.Time >= phraseStartTime && n.Time < phraseEndTime);

                var chordsInPhraseIteration = highestLevelForPhrase.Chords
                    .Where(c => c.Time >= phraseStartTime && c.Time < phraseEndTime);

                var handShapesInPhraseIteration = highestLevelForPhrase.HandShapes
                    .Where(hs => hs.Time >= phraseStartTime && hs.Time < phraseEndTime);

                var anchorsInPhraseIteration = highestLevelForPhrase.Anchors
                    .Where(a => a.Time >= phraseStartTime && a.Time < phraseEndTime);

                notes.AddRange(notesInPhraseIteration);
                chords.AddRange(chordsInPhraseIteration);
                handshapes.AddRange(handShapesInPhraseIteration);
                anchors.AddRange(anchorsInPhraseIteration);
            }

            var arr = new Arrangement { Difficulty = 0 };
            arr.Notes.AddRange(notes);
            arr.Chords.AddRange(chords);
            arr.Anchors.AddRange(anchors);
            arr.HandShapes.AddRange(handshapes);

            return arr;
        }

        private static void AddComment(RS2014Song song)
        {
            song.XmlComments.RemoveAll(c => c.CommentType == CommentType.DDCImprover);
            var ddcComment = song.XmlComments.Find(c => c.CommentType == CommentType.DDC);
            if (ddcComment != null)
            {
                song.XmlComments.RemoveAll(c => c.CommentType == CommentType.DDC);
                string ddcVer = ddcComment.Value.Substring(0, ddcComment.Value.IndexOf('-') - 1);
                song.XmlComments.Add(new RSXmlComment($" DDC Improver {Program.Version} removed DD generated by{ddcVer} "));
            }
            else
            {
                song.XmlComments.Add(new RSXmlComment($" DDC Improver {Program.Version} removed DD "));
            }
        }
    }
}
