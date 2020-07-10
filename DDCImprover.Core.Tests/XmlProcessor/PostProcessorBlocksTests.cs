using DDCImprover.Core.PostBlocks;

using FluentAssertions;

using Rocksmith2014Xml;

using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace DDCImprover.Core.Tests.XmlProcessor
{
    public class PostProcessorBlocksTests : IClassFixture<ConfigurationFixture>
    {
        private readonly InstrumentalArrangement testArrangement;
        private readonly Action<string> nullLog = _ => { };

        public PostProcessorBlocksTests(ConfigurationFixture fixture)
        {
            XMLProcessor.Preferences = fixture.Configuration;

            testArrangement = InstrumentalArrangement.Load(@".\TestFiles\postTest_RS2.xml");
        }

        [Fact]
        public void ExtraneousBeatsRemover_RemovesExtraBeats()
        {
            int audioEnd = testArrangement.SongLength;
            testArrangement.Ebeats.Should().Contain(b => b.Time > audioEnd);

            new ExtraneousBeatsRemover().Apply(testArrangement, nullLog);

            testArrangement.Ebeats.Should().NotContain(b => b.Time > audioEnd);
        }

        [Fact]
        public void ChordNameProcessor_CorrectsEmptyChordNames()
        {
            var testChordTemp1 = new ChordTemplate
            {
                ChordName = " ",
                DisplayName = " "
            };
            var testChordTemp2 = new ChordTemplate
            {
                ChordName = "   ",
                DisplayName = "   "
            };
            testArrangement.ChordTemplates.Add(testChordTemp1);
            testArrangement.ChordTemplates.Add(testChordTemp2);

            new ChordNameProcessor(new List<ImproverMessage>()).Apply(testArrangement, nullLog);

            testChordTemp1.ChordName.Should().Be("");
            testChordTemp1.DisplayName.Should().Be("");
            testChordTemp2.ChordName.Should().Be("");
            testChordTemp2.DisplayName.Should().Be("");
        }

        [Fact]
        public void ChordNameProcessor_CorrectsMinorChordNames()
        {
            var testChordTemp = new ChordTemplate
            {
                ChordName = "Amin",
                DisplayName = "Amin"
            };
            testArrangement.ChordTemplates.Add(testChordTemp);

            new ChordNameProcessor(new List<ImproverMessage>()).Apply(testArrangement, nullLog);

            testChordTemp.ChordName.Should().Be("Am");
            testChordTemp.DisplayName.Should().Be("Am");
        }

        [Fact]
        public void ChordNameProcessor_HandlesArpAndNopNames()
        {
            var testChordTemp1 = new ChordTemplate
            {
                ChordName = "D9-arp",
                DisplayName = "D9-arp"
            };

            var testChordTemp2 = new ChordTemplate
            {
                ChordName = "F5(no 3)-nop",
                DisplayName = "F5(no 3)-nop"
            };
            testArrangement.ChordTemplates.Add(testChordTemp1);
            testArrangement.ChordTemplates.Add(testChordTemp2);

            new ChordNameProcessor(new List<ImproverMessage>()).Apply(testArrangement, nullLog);

            testChordTemp1.ChordName.Should().Be("D9");
            testChordTemp1.DisplayName.Should().Be("D9-arp");

            testChordTemp2.ChordName.Should().Be("F5(no 3)");
            testChordTemp2.DisplayName.Should().Be("F5(no 3)-nop");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(12)]
        [InlineData(21)]
        public void ChordNameProcessor_HandlesOFChords(int number)
        {
            var testChordTemp = new ChordTemplate
            {
                ChordName = $"OF{number}",
                DisplayName = $"OF{number}",
                Fingers = new sbyte[] { 1, -1, -1, -1, -1, -1 },
                Frets = new sbyte[] { 8, -1, -1, -1, -1, -1 }
            };
            testArrangement.ChordTemplates.Add(testChordTemp);

            new ChordNameProcessor(new List<ImproverMessage>()).Apply(testArrangement, nullLog);

            testChordTemp.ChordName.Should().Be("");
            testChordTemp.DisplayName.Should().Be("");
            testChordTemp.Frets[0].Should().Be((sbyte)number);
        }

        [Fact]
        public void OneLevelPhraseFixer_AddsASecondDifficultyLevel()
        {
            testArrangement.PhraseIterations.Last().Time = testArrangement.SongLength;
            testArrangement.PhraseIterations.Last().PhraseId++;

            var testPhrase = new Phrase("test", 0, PhraseMask.None);
            testArrangement.Phrases.Insert(testArrangement.Phrases.Count - 1, testPhrase);
            testArrangement.PhraseIterations.Insert(testArrangement.PhraseIterations.Count - 1,
                new PhraseIteration(10000, testArrangement.Phrases.Count - 2));
            testArrangement.Levels[0].Notes.Add(
                new Note
                {
                    Sustain = 3000,
                    Fret = 3,
                    Time = 10000
                });

            new OneLevelPhraseFixer().Apply(testArrangement, nullLog);

            testPhrase.MaxDifficulty.Should().Be(1);
        }

        [Fact]
        public void TimeSignatureEventRemover_RemovesTimeSignatureEvents()
        {
            testArrangement.Events.Should().Contain(e => e.Code.StartsWith("TS"));

            new TimeSignatureEventRemover().Apply(testArrangement, nullLog);

            testArrangement.Events.Should().NotContain(e => e.Code.StartsWith("TS"));
        }

        [Fact]
        public void TemporaryBeatRemover_RemovesAddedBeats()
        {
            var testBeat = new Ebeat(222222, 1);
            var addedBeats = new List<Ebeat> { testBeat };
            testArrangement.Ebeats.Add(testBeat);

            new TemporaryBeatRemover(addedBeats).Apply(testArrangement, nullLog);

            testArrangement.Ebeats.Should().NotContain(testBeat);
        }

        [Fact]
        public void FirstNoguitarSectionRestorer_RestoresNoguitarSection()
        {
            const int firstNGSectionTime = 4500;

            new FirstNoguitarSectionRestorer(firstNGSectionTime).Apply(testArrangement, nullLog);

            testArrangement.Sections.Should().Contain(s => s.Time == firstNGSectionTime && s.Name == "noguitar");
            testArrangement.Phrases.Should().Contain(p => p.Name == "NG");
            testArrangement.PhraseIterations.Should().Contain(pi => pi.Time == firstNGSectionTime);
        }

        [Fact]
        public void ENDPhraseProcessor_MovesEndPhrase()
        {
            int endPhraseId = testArrangement.Phrases.FindIndex(p => p.Name.Equals("END", StringComparison.OrdinalIgnoreCase));
            var endPhraseIter = testArrangement.PhraseIterations.First(pi => pi.PhraseId == endPhraseId);
            int oldTime = endPhraseIter.Time + 2000;

            new ENDPhraseProcessor(oldTime).Apply(testArrangement, nullLog);

            endPhraseIter.Time.Should().Be(oldTime);
        }

        [Fact]
        public void NoguitarAnchorRestorer_RestoresAnchor()
        {
            var testAnchor = new Anchor(1, 4500);
            var ngAnchors = new List<Anchor>
            {
                testAnchor
            };

            new NoguitarAnchorRestorer(ngAnchors).Apply(testArrangement, nullLog);

            testArrangement.Levels[0].Anchors[0].Should().Be(testAnchor);
        }

        [Fact]
        public void RemoveBeatsEvent_RemovesBeats()
        {
            testArrangement.Events.Add(new Event("removebeats", 112500));

            new CustomEventPostProcessor(new List<ImproverMessage>()).Apply(testArrangement, nullLog);

            testArrangement.Events.Should().NotContain(ev => ev.Code == "removebeats");
            testArrangement.Ebeats.Should().NotContain(eb => eb.Time >= 112500);
        }

        [Fact]
        public void SlideOutEvent_CreatesHandshape()
        {
            const int chordTime = 20222;
            const int sustainTime = 3000;

            var phrase = new Phrase("test", (byte)(testArrangement.Levels.Count - 1), PhraseMask.None);
            testArrangement.Phrases.Add(phrase);

            var phraseIter = new PhraseIteration(chordTime, testArrangement.Phrases.Count - 1);
            testArrangement.PhraseIterations.Add(phraseIter);

            var template = new ChordTemplate
            {
                Fingers = new sbyte[] { 1, 3, 3, -1, -1, -1 },
                Frets = new sbyte[] { 1, 3, 4, -1, -1, -1 }
            };
            testArrangement.ChordTemplates.Add(template);
            short chordId = (short)(testArrangement.ChordTemplates.Count - 1);

            var chord = new Chord
            {
                ChordId = chordId,
                Time = chordTime,
                ChordNotes = new List<Note>
                {
                    new Note
                    {
                        String = 0,
                        Fret = 1,
                        SlideUnpitchTo = 5,
                        Sustain = sustainTime
                    },
                    new Note
                    {
                        String = 1,
                        Fret = 3,
                        SlideUnpitchTo = 7,
                        Sustain = sustainTime
                    },
                    new Note
                    {
                        String = 2,
                        Fret = 3,
                        SlideUnpitchTo = 7,
                        Sustain = sustainTime
                    }
                }
            };

            var hardestLevel = testArrangement.Levels.Last();

            var handshape = new HandShape(chordId, chordTime, chordTime + sustainTime);
            hardestLevel.Chords.Add(chord);
            hardestLevel.HandShapes.Add(handshape);
            testArrangement.Events.Add(new Event("so", chordTime));

            int handShapeCount = hardestLevel.HandShapes.Count;
            int chordTemplateCount = testArrangement.ChordTemplates.Count;

            new CustomEventPostProcessor(new List<ImproverMessage>()).Apply(testArrangement, nullLog);

            testArrangement.Events.Should().NotContain(ev => ev.Code == "so");
            hardestLevel.HandShapes.Should().HaveCount(handShapeCount + 1);
            testArrangement.ChordTemplates.Should().HaveCount(chordTemplateCount + 1);
            handshape.EndTime.Should().BeLessThan(chordTime + sustainTime);
        }

        [Fact]
        public void AnchorPlaceholderNoteRemover_WorksForNotes()
        {
            var testNote = new Note { Time = 30000, Fret = 2, String = 2, SlideTo = 5, IsLinkNext = true };
            testArrangement.Levels[0].Notes.Add(testNote);
            testArrangement.Levels[0].Notes.Add(new Note { Time = 33000, Fret = 5, String = 2 });

            new AnchorPlaceholderNoteRemover().Apply(testArrangement, nullLog);

            testArrangement.Levels[0].Notes.Should().NotContain(n => n.Time == 33f);
            testNote.IsLinkNext.Should().BeFalse();
        }

        [Fact]
        public void AnchorPlaceholderNoteRemover_WorksForChords()
        {
            var testChord = new Chord
            {
                Time = 30000,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0, SlideTo = 7, IsLinkNext = true, Sustain = 3000 },
                    new Note { Fret = 5, String = 1, SlideTo = 7, IsLinkNext = true, Sustain = 3000 },
                    new Note { Fret = 5, String = 2, Sustain = 3000 }
                },
                IsLinkNext = true
            };
            testArrangement.Levels[0].Chords.Add(testChord);
            testArrangement.Levels[0].Notes.Add(new Note
            {
                Time = 33000,
                String = 0,
                Fret = 7
            });
            testArrangement.Levels[0].Notes.Add(new Note
            {
                Time = 33000,
                String = 1,
                Fret = 7
            });

            new AnchorPlaceholderNoteRemover().Apply(testArrangement, nullLog);

            testArrangement.Levels[0].Notes.Should().NotContain(n => n.Time == 33000);
            testChord.IsLinkNext.Should().BeFalse();
            testChord.ChordNotes.Should().NotContain(n => n.IsLinkNext);
        }

        [Fact]
        public void AnchorPlaceholderNoteRemover_DoesNotRemoveWrongNotes()
        {
            var testChord = new Chord
            {
                Time = 30000,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0, IsLinkNext = true, Sustain = 3000 },
                    new Note { Fret = 5, String = 1, SlideTo = 7, IsLinkNext = true, Sustain = 3000 },
                    new Note { Fret = 5, String = 2, Sustain = 3000 }
                },
                IsLinkNext = true
            };
            testArrangement.Levels[0].Chords.Add(testChord);
            testArrangement.Levels[0].Notes.Add(new Note
            {
                Time = 34000,
                String = 0,
                Fret = 7
            });
            testArrangement.Levels[0].Notes.Add(new Note
            {
                Time = 33000,
                String = 1,
                Fret = 7
            });

            new AnchorPlaceholderNoteRemover().Apply(testArrangement, nullLog);

            testArrangement.Levels[0].Notes.Should().Contain(n => n.Time == 34000);
        }

        [Fact]
        public void HighDensityRemover_RemovesHighDensityAndChordNotes()
        {
            testArrangement.Levels[0].Chords.Add(new Chord
            {
                Time = 10000,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0 },
                    new Note { Fret = 7, String = 1 },
                    new Note { Fret = 7, String = 2 }
                }
            });
            testArrangement.Levels[0].Chords.Add(new Chord
            {
                Time = 11000,
                IsHighDensity = true,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0 },
                    new Note { Fret = 7, String = 1 },
                    new Note { Fret = 7, String = 2 }
                }
            });
            testArrangement.Levels[0].HandShapes.Add(new HandShape
            {
                StartTime = 10000,
                EndTime = 15000
            });

            new HighDensityRemover().Apply(testArrangement, nullLog);

            testArrangement.Levels[0].Chords[1].IsHighDensity.Should().BeFalse();
            testArrangement.Levels[0].Chords[1].ChordNotes.Should().BeNull();
        }

        [Fact]
        public void HighDensityRemover_DetectsFirstNonMutedChord()
        {
            testArrangement.Levels[0].Chords.Add(new Chord
            {
                Time = 10000,
                IsHighDensity = true,
                IsFretHandMute = true,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0 },
                    new Note { Fret = 7, String = 1 },
                    new Note { Fret = 7, String = 2 }
                }
            });
            testArrangement.Levels[0].Chords.Add(new Chord
            {
                Time = 11000,
                IsHighDensity = true,
                IsFretHandMute = true,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0 },
                    new Note { Fret = 7, String = 1 },
                    new Note { Fret = 7, String = 2 }
                }
            });
            testArrangement.Levels[0].Chords.Add(new Chord
            {
                Time = 11000,
                IsHighDensity = true,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0 },
                    new Note { Fret = 7, String = 1 },
                    new Note { Fret = 7, String = 2 }
                }
            });
            testArrangement.Levels[0].HandShapes.Add(new HandShape
            {
                StartTime = 10000,
                EndTime = 15000
            });

            new HighDensityRemover().Apply(testArrangement, nullLog);

            testArrangement.Levels[0].Chords[0].IsHighDensity.Should().BeFalse();
            testArrangement.Levels[0].Chords[1].IsHighDensity.Should().BeFalse();
            testArrangement.Levels[0].Chords[2].IsHighDensity.Should().BeFalse();
            testArrangement.Levels[0].Chords[0].ChordNotes.Should().BeNull();
            testArrangement.Levels[0].Chords[1].ChordNotes.Should().BeNull();
            testArrangement.Levels[0].Chords[2].ChordNotes.Should().NotBeNull();
        }

        [Fact]
        public void HighDensityRemover_SetsIgnoreForHarmonicChords()
        {
            var firstLevel = testArrangement.Levels[0];

            firstLevel.Chords.Add(new Chord
            {
                Time = 10000,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 7, String = 0, IsHarmonic = true },
                    new Note { Fret = 7, String = 1, IsHarmonic = true },
                    new Note { Fret = 7, String = 2, IsHarmonic = true }
                }
            });
            firstLevel.Chords.Add(new Chord
            {
                Time = 11000,
                IsHighDensity = true,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 7, String = 0, IsHarmonic = true },
                    new Note { Fret = 7, String = 1, IsHarmonic = true },
                    new Note { Fret = 7, String = 2, IsHarmonic = true }
                }
            });
            firstLevel.Chords.Add(new Chord
            {
                Time = 11000,
                IsHighDensity = true,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 7, String = 0, IsHarmonic = true },
                    new Note { Fret = 7, String = 1, IsHarmonic = true },
                    new Note { Fret = 7, String = 2, IsHarmonic = true }
                }
            });
            firstLevel.HandShapes.Add(new HandShape
            {
                StartTime = 10000,
                EndTime = 15000
            });

            new HighDensityRemover().Apply(testArrangement, nullLog);

            firstLevel.Chords[1].IsHighDensity.Should().BeFalse();
            firstLevel.Chords[2].IsHighDensity.Should().BeFalse();
            firstLevel.Chords[1].ChordNotes.Should().BeNull();
            firstLevel.Chords[2].ChordNotes.Should().BeNull();
            firstLevel.Chords[1].IsIgnore.Should().BeTrue();
            firstLevel.Chords[2].IsIgnore.Should().BeTrue();
        }
    }
}
