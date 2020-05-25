using Rocksmith2014Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DDCImprover.Core
{
    internal static class DDRemover
    {
        public static async Task RemoveDD(RS2014Song song, bool matchPhrasesToSections, bool deleteTranscriptionTrack)
        {
            var trTrack = await GenerateTranscriptionTrack(song).ConfigureAwait(false);

            song.Levels.Clear();
            song.Levels.Add(trTrack);

            song.NewLinkedDiffs.Clear();
            song.LinkedDiffs?.Clear();

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

            if(deleteTranscriptionTrack)
            {
                song.TranscriptionTrack = new Level();
            }

            AddComment(song);
        }

        private static async Task<Level> GenerateTranscriptionTrack(RS2014Song song)
        {
            var notes = new List<Note>();
            var chords = new List<Chord>();
            var handshapes = new List<HandShape>();
            var anchors = new List<Anchor>();
            var tasks = Enumerable.Repeat(Task.CompletedTask, 4).ToArray();

            // Ignore the last phrase iteration (END)
            for (int i = 0; i < song.PhraseIterations.Count - 1; i++)
            {
                var phraseIteration = song.PhraseIterations[i];
                int phraseId = phraseIteration.PhraseId;
                int maxDifficulty = song.Phrases[phraseId].MaxDifficulty;

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

                tasks[0] = tasks[0].ContinueWith(_ => notes.AddRange(notesInPhraseIteration));
                tasks[1] = tasks[1].ContinueWith(_ => chords.AddRange(chordsInPhraseIteration));
                tasks[2] = tasks[2].ContinueWith(_ => handshapes.AddRange(handShapesInPhraseIteration));
                tasks[3] = tasks[3].ContinueWith(_ => anchors.AddRange(anchorsInPhraseIteration));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            var arr = new Level { Difficulty = 0 };
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
