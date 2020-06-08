using Rocksmith2014Xml;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DDCImprover.Core
{
    public static class DDRemover
    {
        public static async Task RemoveDD(InstrumentalArrangement arrangement, bool matchPhrasesToSections, bool deleteTranscriptionTrack)
        {
            var trTrack = await GenerateTranscriptionTrack(arrangement).ConfigureAwait(false);

            arrangement.Levels.Clear();
            arrangement.Levels.Add(trTrack);

            arrangement.NewLinkedDiffs.Clear();
            arrangement.LinkedDiffs?.Clear();

            if (matchPhrasesToSections)
            {
                arrangement.Phrases.Clear();

                arrangement.Phrases.Add(new Phrase { Name = "COUNT" });
                foreach (string sectionName in arrangement.Sections.SkipLast().Select(s => s.Name).Distinct())
                {
                    arrangement.Phrases.Add(new Phrase
                    {
                        Name = sectionName
                    });
                }
                arrangement.Phrases.Add(new Phrase { Name = "END" });

                arrangement.PhraseIterations.Clear();

                // Add COUNT phrase iteration
                arrangement.PhraseIterations.Add(new PhraseIteration(arrangement.StartBeat, 0));

                // Add phrase iterations
                foreach (Section section in arrangement.Sections.SkipLast())
                {
                    int phraseId = arrangement.Phrases.FindIndex(p => p.Name == section.Name);
                    arrangement.PhraseIterations.Add(new PhraseIteration(section.Time, phraseId));
                }

                // Add END phrase iteration
                arrangement.PhraseIterations.Add(new PhraseIteration(arrangement.Sections.Last().Time, arrangement.Phrases.Count - 1));
            }
            else
            {
                foreach (var p in arrangement.Phrases)
                {
                    p.MaxDifficulty = 0;
                }

                foreach (var pi in arrangement.PhraseIterations)
                {
                    pi.HeroLevels = null;
                }
            }

            // Remove any unused chord templates
            if (arrangement.ChordTemplates.Count > 0)
            {
                int highestChordId = 0;
                if (arrangement.Levels[0].Chords.Count > 0)
                    highestChordId = arrangement.Levels[0].Chords.Max(c => c.ChordId);

                int highestHandShapeId = 0;
                if (arrangement.Levels[0].HandShapes.Count > 0)
                    highestHandShapeId = arrangement.Levels[0].HandShapes.Max(hs => hs.ChordId);

                int highestId = Math.Max(highestChordId, highestHandShapeId);
                if (highestId < arrangement.ChordTemplates.Count - 1)
                    arrangement.ChordTemplates.RemoveRange(highestId + 1, arrangement.ChordTemplates.Count - 1 - highestId);
            }

            if (deleteTranscriptionTrack)
            {
                arrangement.TranscriptionTrack = new Level();
            }

            AddComment(arrangement);
        }

        private static async Task<Level> GenerateTranscriptionTrack(InstrumentalArrangement arrangement)
        {
            var notes = new List<Note>();
            var chords = new List<Chord>();
            var handshapes = new List<HandShape>();
            var anchors = new List<Anchor>();
            var tasks = Enumerable.Repeat(Task.CompletedTask, 4).ToArray();

            // Ignore the last phrase iteration (END)
            for (int i = 0; i < arrangement.PhraseIterations.Count - 1; i++)
            {
                var phraseIteration = arrangement.PhraseIterations[i];
                int phraseId = phraseIteration.PhraseId;
                int maxDifficulty = arrangement.Phrases[phraseId].MaxDifficulty;

                int phraseStartTime = phraseIteration.Time;
                int phraseEndTime = arrangement.PhraseIterations[i + 1].Time;
                var highestLevelForPhrase = arrangement.Levels[maxDifficulty];

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

            var level = new Level { Difficulty = 0 };
            level.Notes.AddRange(notes);
            level.Chords.AddRange(chords);
            level.Anchors.AddRange(anchors);
            level.HandShapes.AddRange(handshapes);

            return level;
        }

        private static void AddComment(InstrumentalArrangement arrangement)
        {
            arrangement.XmlComments.RemoveAll(c => c.CommentType == CommentType.DDCImprover);
            var ddcComment = arrangement.XmlComments.Find(c => c.CommentType == CommentType.DDC);
            if (ddcComment != null)
            {
                arrangement.XmlComments.RemoveAll(c => c.CommentType == CommentType.DDC);
                string ddcVer = ddcComment.Value.Substring(0, ddcComment.Value.IndexOf('-') - 1);
                arrangement.XmlComments.Add(new RSXmlComment($" DDC Improver {Program.Version} removed DD generated by{ddcVer} "));
            }
            else
            {
                arrangement.XmlComments.Add(new RSXmlComment($" DDC Improver {Program.Version} removed DD "));
            }
        }
    }
}
