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
        private readonly RS2014Song testSong;
        private readonly Action<string> nullLog = _ => { };

        public PostProcessorBlocksTests(ConfigurationFixture fixture)
        {
            XMLProcessor.Preferences = fixture.Configuration;

            testSong = RS2014Song.Load(@".\TestFiles\postTest_RS2.xml");
        }

        [Fact]
        public void ExtraneousBeatsRemoverTest()
        {
            float audioEnd = testSong.SongLength;
            testSong.Ebeats.Should().Contain(b => b.Time > audioEnd);

            new ExtraneousBeatsRemover().Apply(testSong, nullLog);

            testSong.Ebeats.Should().NotContain(b => b.Time > audioEnd);
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
            testSong.ChordTemplates.Add(testChordTemp1);
            testSong.ChordTemplates.Add(testChordTemp2);

            new ChordNameProcessor(new List<ImproverMessage>()).Apply(testSong, nullLog);

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
            testSong.ChordTemplates.Add(testChordTemp);

            new ChordNameProcessor(new List<ImproverMessage>()).Apply(testSong, nullLog);

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
            testSong.ChordTemplates.Add(testChordTemp1);
            testSong.ChordTemplates.Add(testChordTemp2);

            new ChordNameProcessor(new List<ImproverMessage>()).Apply(testSong, nullLog);

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
            testSong.ChordTemplates.Add(testChordTemp);

            new ChordNameProcessor(new List<ImproverMessage>()).Apply(testSong, nullLog);

            testChordTemp.ChordName.Should().Be("");
            testChordTemp.DisplayName.Should().Be("");
            testChordTemp.Frets[0].Should().Be((sbyte)number);
        }

        [Fact]
        public void OneLevelPhraseFixerTest()
        {
            testSong.PhraseIterations.Last().Time = testSong.SongLength;
            testSong.PhraseIterations.Last().PhraseId++;

            var testPhrase = new Phrase("test", 0, PhraseMask.None);
            testSong.Phrases.Insert(testSong.Phrases.Count - 1, testPhrase);
            testSong.PhraseIterations.Insert(testSong.PhraseIterations.Count - 1,
                new PhraseIteration
                {
                    PhraseId = testSong.Phrases.Count - 2,
                    Time = 10f
                });
            testSong.Levels[0].Notes.Add(
                new Note
                {
                    Sustain = 3f,
                    Fret = 3,
                    Time = 10f
                });

            new OneLevelPhraseFixer().Apply(testSong, nullLog);

            testPhrase.MaxDifficulty.Should().Be(1);
        }

        [Fact]
        public void TimeSignatureEventRemoverTest()
        {
            testSong.Events.Should().Contain(e => e.Code.StartsWith("TS"));

            new TimeSignatureEventRemover().Apply(testSong, nullLog);

            testSong.Events.Should().NotContain(e => e.Code.StartsWith("TS"));
        }

        [Fact]
        public void TemporaryBeatRemoverTest()
        {
            var testBeat = new Ebeat(222.222f, 1);
            var addedBeats = new List<Ebeat> { testBeat };
            testSong.Ebeats.Add(testBeat);

            new TemporaryBeatRemover(addedBeats).Apply(testSong, nullLog);

            testSong.Ebeats.Should().NotContain(testBeat);
        }

        [Fact]
        public void FirstNoguitarSectionRestorerTest()
        {
            const float firstNGSectionTime = 4.5f;

            new FirstNoguitarSectionRestorer(firstNGSectionTime).Apply(testSong, nullLog);

            testSong.Sections.Should().Contain(s => s.Time == firstNGSectionTime && s.Name == "noguitar");
            testSong.Phrases.Should().Contain(p => p.Name == "NG");
            testSong.PhraseIterations.Should().Contain(pi => pi.Time == firstNGSectionTime);
        }

        [Fact]
        public void ENDPhraseProcessorTest()
        {
            int endPhraseId = testSong.Phrases.FindIndex(p => p.Name.Equals("END", StringComparison.OrdinalIgnoreCase));
            var endPhraseIter = testSong.PhraseIterations.First(pi => pi.PhraseId == endPhraseId);
            float oldTime = endPhraseIter.Time + 2f;

            new ENDPhraseProcessor(oldTime).Apply(testSong, nullLog);

            endPhraseIter.Time.Should().Be(oldTime);
        }

        [Fact]
        public void NoguitarAnchorRestorerTest()
        {
            var testAnchor = new Anchor(1, 4.5f);
            var ngAnchors = new List<Anchor>
            {
                testAnchor
            };

            new NoguitarAnchorRestorer(ngAnchors).Apply(testSong, nullLog);

            testSong.Levels[0].Anchors[0].Should().Be(testAnchor);
        }

        [Fact]
        public void CustomEvent_RemoveBeats_Test()
        {
            testSong.Events.Add(new Event("removebeats", 112.5f));

            new CustomEventPostProcessor(new List<ImproverMessage>()).Apply(testSong, nullLog);

            testSong.Events.Should().NotContain(ev => ev.Code == "removebeats");
            testSong.Ebeats.Should().NotContain(eb => eb.Time >= 112.5f);
        }

        [Fact]
        public void CustomEvent_SlideOut_Test()
        {
            const float chordTime = 20.222f;
            const float sustainTime = 3f;

            var phrase = new Phrase("test", (byte)(testSong.Levels.Count - 1), PhraseMask.None);
            testSong.Phrases.Add(phrase);

            var phraseIter = new PhraseIteration
            {
                Time = chordTime,
                PhraseId = testSong.Phrases.Count - 1
            };
            testSong.PhraseIterations.Add(phraseIter);

            var template = new ChordTemplate
            {
                Fingers = new sbyte[] { 1, 3, 3, -1, -1, -1 },
                Frets = new sbyte[] { 1, 3, 4, -1, -1, -1 }
            };
            testSong.ChordTemplates.Add(template);
            int chordId = testSong.ChordTemplates.Count - 1;

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

            var hardestLevel = testSong.Levels.Last();

            var handshape = new HandShape(chordId, chordTime, chordTime + sustainTime);
            hardestLevel.Chords.Add(chord);
            hardestLevel.HandShapes.Add(handshape);
            testSong.Events.Add(new Event("so", chordTime));

            int handShapeCount = hardestLevel.HandShapes.Count;
            int chordTemplateCount = testSong.ChordTemplates.Count;

            new CustomEventPostProcessor(new List<ImproverMessage>()).Apply(testSong, nullLog);

            testSong.Events.Should().NotContain(ev => ev.Code == "so");
            hardestLevel.HandShapes.Should().HaveCount(handShapeCount + 1);
            testSong.ChordTemplates.Should().HaveCount(chordTemplateCount + 1);
            handshape.EndTime.Should().BeLessThan(chordTime + sustainTime);
        }

        [Fact]
        public void AnchorPlaceholderNoteRemover_WorksForNotes()
        {
            var testNote = new Note { Time = 30f, Fret = 2, String = 2, SlideTo = 5, IsLinkNext = true };
            testSong.Levels[0].Notes.Add(testNote);
            testSong.Levels[0].Notes.Add(new Note { Time = 33f, Fret = 5, String = 2 });

            new AnchorPlaceholderNoteRemover().Apply(testSong, nullLog);

            testSong.Levels[0].Notes.Should().NotContain(n => n.Time == 33f);
            testNote.IsLinkNext.Should().BeFalse();
        }

        [Fact]
        public void AnchorPlaceholderNoteRemover_WorksForChords()
        {
            var testChord = new Chord
            {
                Time = 30f,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0, SlideTo = 7, IsLinkNext = true, Sustain = 3f },
                    new Note { Fret = 5, String = 1, SlideTo = 7, IsLinkNext = true, Sustain = 3f },
                    new Note { Fret = 5, String = 2, Sustain = 3f }
                },
                IsLinkNext = true
            };
            testSong.Levels[0].Chords.Add(testChord);
            testSong.Levels[0].Notes.Add(new Note
            {
                Time = 33f,
                String = 0,
                Fret = 7
            });
            testSong.Levels[0].Notes.Add(new Note
            {
                Time = 33f,
                String = 1,
                Fret = 7
            });

            new AnchorPlaceholderNoteRemover().Apply(testSong, nullLog);

            testSong.Levels[0].Notes.Should().NotContain(n => n.Time == 33f);
            testChord.IsLinkNext.Should().BeFalse();
            testChord.ChordNotes.Should().NotContain(n => n.IsLinkNext);
        }

        [Fact]
        public void AnchorPlaceholderNoteRemover_DoesNotRemoveWrongNotes()
        {
            var testChord = new Chord
            {
                Time = 30f,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0, IsLinkNext = true, Sustain = 3f },
                    new Note { Fret = 5, String = 1, SlideTo = 7, IsLinkNext = true, Sustain = 3f },
                    new Note { Fret = 5, String = 2, Sustain = 3f }
                },
                IsLinkNext = true
            };
            testSong.Levels[0].Chords.Add(testChord);
            testSong.Levels[0].Notes.Add(new Note
            {
                Time = 34f,
                String = 0,
                Fret = 7
            });
            testSong.Levels[0].Notes.Add(new Note
            {
                Time = 33f,
                String = 1,
                Fret = 7
            });

            new AnchorPlaceholderNoteRemover().Apply(testSong, nullLog);

            testSong.Levels[0].Notes.Should().Contain(n => n.Time == 34f);
        }

        [Fact]
        public void HighDensityRemover_RemovesHighDensityAndChordNotes()
        {
            testSong.Levels[0].Chords.Add(new Chord
            {
                Time = 10f,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0 },
                    new Note { Fret = 7, String = 1 },
                    new Note { Fret = 7, String = 2 }
                }
            });
            testSong.Levels[0].Chords.Add(new Chord
            {
                Time = 11f,
                IsHighDensity = true,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0 },
                    new Note { Fret = 7, String = 1 },
                    new Note { Fret = 7, String = 2 }
                }
            });
            testSong.Levels[0].HandShapes.Add(new HandShape
            {
                StartTime = 10f,
                EndTime = 15f
            });

            new HighDensityRemover().Apply(testSong, nullLog);

            testSong.Levels[0].Chords[1].IsHighDensity.Should().BeFalse();
            testSong.Levels[0].Chords[1].ChordNotes.Should().BeNull();
        }

        [Fact]
        public void HighDensityRemover_DetectsFirstNonMutedChord()
        {
            testSong.Levels[0].Chords.Add(new Chord
            {
                Time = 10f,
                IsHighDensity = true,
                IsFretHandMute = true,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0 },
                    new Note { Fret = 7, String = 1 },
                    new Note { Fret = 7, String = 2 }
                }
            });
            testSong.Levels[0].Chords.Add(new Chord
            {
                Time = 11f,
                IsHighDensity = true,
                IsFretHandMute = true,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0 },
                    new Note { Fret = 7, String = 1 },
                    new Note { Fret = 7, String = 2 }
                }
            });
            testSong.Levels[0].Chords.Add(new Chord
            {
                Time = 11f,
                IsHighDensity = true,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 5, String = 0 },
                    new Note { Fret = 7, String = 1 },
                    new Note { Fret = 7, String = 2 }
                }
            });
            testSong.Levels[0].HandShapes.Add(new HandShape
            {
                StartTime = 10f,
                EndTime = 15f
            });

            new HighDensityRemover().Apply(testSong, nullLog);

            testSong.Levels[0].Chords[0].IsHighDensity.Should().BeFalse();
            testSong.Levels[0].Chords[1].IsHighDensity.Should().BeFalse();
            testSong.Levels[0].Chords[2].IsHighDensity.Should().BeFalse();
            testSong.Levels[0].Chords[0].ChordNotes.Should().BeNull();
            testSong.Levels[0].Chords[1].ChordNotes.Should().BeNull();
            testSong.Levels[0].Chords[2].ChordNotes.Should().NotBeNull();
        }

        [Fact]
        public void HighDensityRemover_SetsIgnoreForHarmonicChords()
        {
            var firstLevel = testSong.Levels[0];

            firstLevel.Chords.Add(new Chord
            {
                Time = 10f,
                ChordNotes = new List<Note>
                {
                    new Note { Fret = 7, String = 0, IsHarmonic = true },
                    new Note { Fret = 7, String = 1, IsHarmonic = true },
                    new Note { Fret = 7, String = 2, IsHarmonic = true }
                }
            });
            firstLevel.Chords.Add(new Chord
            {
                Time = 11f,
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
                Time = 11f,
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
                StartTime = 10f,
                EndTime = 15f
            });

            new HighDensityRemover().Apply(testSong, nullLog);

            firstLevel.Chords[1].IsHighDensity.Should().BeFalse();
            firstLevel.Chords[2].IsHighDensity.Should().BeFalse();
            firstLevel.Chords[1].ChordNotes.Should().BeNull();
            firstLevel.Chords[2].ChordNotes.Should().BeNull();
            firstLevel.Chords[1].IsIgnore.Should().BeTrue();
            firstLevel.Chords[2].IsIgnore.Should().BeTrue();
        }
    }
}
