using XmlUtils;

namespace Rocksmith2014Xml
{
    public sealed class BendValueCollection : XmlCountListEx<BendValue>
    {
        public BendValueCollection() : base("bendValue") { }
    }

    public sealed class HeroLevelCollection : XmlCountListEx<HeroLevel>
    {
        public HeroLevelCollection() : base("heroLevel") { }
    }

    public sealed class NewLinkedDiffCollection : XmlCountListEx<NewLinkedDiff>
    {
        public NewLinkedDiffCollection() : base("newLinkedDiff") { }
    }

    public sealed class ToneCollection : XmlCountListEx<Tone>
    {
        public ToneCollection() : base("tone") { }
    }

    public sealed class NoteCollection : XmlCountListEx<Note>
    {
        public NoteCollection() : base("note") { }
    }

    public sealed class ChordCollection : XmlCountListEx<Chord>
    {
        public ChordCollection() : base("chord") { }
    }

    public sealed class AnchorCollection : XmlCountListEx<Anchor>
    {
        public AnchorCollection() : base("anchor") { }
    }

    public sealed class HandShapeCollection : XmlCountListEx<HandShape>
    {
        public HandShapeCollection() : base("handShape") { }
    }

    public sealed class EventCollection : XmlCountListEx<Event>
    {
        public EventCollection() : base("event") { }
    }

    public sealed class SectionCollection : XmlCountListEx<Section>
    {
        public SectionCollection() : base("section") { }
    }

    public sealed class EbeatCollection : XmlCountListEx<Ebeat>
    {
        public EbeatCollection() : base("ebeat") { }
    }

    public sealed class ChordTemplateCollection : XmlCountListEx<ChordTemplate>
    {
        public ChordTemplateCollection() : base("chordTemplate") { }
    }

    public sealed class PhraseCollection : XmlCountListEx<Phrase>
    {
        public PhraseCollection() : base("phrase") { }
    }

    public sealed class PhraseIterationCollection : XmlCountListEx<PhraseIteration>
    {
        public PhraseIterationCollection() : base("phraseIteration") { }
    }

    public sealed class ArrangementCollection : XmlCountListEx<Arrangement>
    {
        public ArrangementCollection() : base("level") { }
    }
}
