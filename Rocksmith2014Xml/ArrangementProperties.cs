using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class ArrangementProperties : IXmlSerializable
    {
        public byte Represent { get; set; }
        public byte BonusArrangement { get; set; }
        public byte StandardTuning { get; set; }
        public byte NonStandardChords { get; set; }
        public byte BarreChords { get; set; }
        public byte PowerChords { get; set; }
        public byte DropDPower { get; set; }
        public byte OpenChords { get; set; }
        public byte FingerPicking { get; set; }
        public byte PickDirection { get; set; }
        public byte DoubleStops { get; set; }
        public byte PalmMutes { get; set; }
        public byte Harmonics { get; set; }
        public byte PinchHarmonics { get; set; }
        public byte Hopo { get; set; }
        public byte Tremolo { get; set; }
        public byte Slides { get; set; }
        public byte UnpitchedSlides { get; set; }
        public byte Bends { get; set; }
        public byte Tapping { get; set; }
        public byte Vibrato { get; set; }
        public byte FretHandMutes { get; set; }
        public byte SlapPop { get; set; }
        public byte TwoFingerPicking { get; set; }
        public byte FifthsAndOctaves { get; set; }
        public byte Syncopation { get; set; }
        public byte BassPick { get; set; }
        public byte Sustain { get; set; }
        public byte PathLead { get; set; }
        public byte PathRhythm { get; set; }
        public byte PathBass { get; set; }

        #region IXmlSerializable Implementation

        XmlSchema IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Represent = byte.Parse(reader.GetAttribute("represent"), NumberFormatInfo.InvariantInfo);
            BonusArrangement = byte.Parse(reader.GetAttribute("bonusArr"), NumberFormatInfo.InvariantInfo);
            StandardTuning = byte.Parse(reader.GetAttribute("standardTuning"), NumberFormatInfo.InvariantInfo);
            NonStandardChords = byte.Parse(reader.GetAttribute("nonStandardChords"), NumberFormatInfo.InvariantInfo);
            BarreChords = byte.Parse(reader.GetAttribute("barreChords"), NumberFormatInfo.InvariantInfo);
            PowerChords = byte.Parse(reader.GetAttribute("powerChords"), NumberFormatInfo.InvariantInfo);
            DropDPower = byte.Parse(reader.GetAttribute("dropDPower"), NumberFormatInfo.InvariantInfo);
            OpenChords = byte.Parse(reader.GetAttribute("openChords"), NumberFormatInfo.InvariantInfo);
            FingerPicking = byte.Parse(reader.GetAttribute("fingerPicking"), NumberFormatInfo.InvariantInfo);
            PickDirection = byte.Parse(reader.GetAttribute("pickDirection"), NumberFormatInfo.InvariantInfo);
            DoubleStops = byte.Parse(reader.GetAttribute("doubleStops"), NumberFormatInfo.InvariantInfo);
            PalmMutes = byte.Parse(reader.GetAttribute("palmMutes"), NumberFormatInfo.InvariantInfo);
            Harmonics = byte.Parse(reader.GetAttribute("harmonics"), NumberFormatInfo.InvariantInfo);
            PinchHarmonics = byte.Parse(reader.GetAttribute("pinchHarmonics"), NumberFormatInfo.InvariantInfo);
            Hopo = byte.Parse(reader.GetAttribute("hopo"), NumberFormatInfo.InvariantInfo);
            Tremolo = byte.Parse(reader.GetAttribute("tremolo"), NumberFormatInfo.InvariantInfo);
            Slides = byte.Parse(reader.GetAttribute("slides"), NumberFormatInfo.InvariantInfo);
            UnpitchedSlides = byte.Parse(reader.GetAttribute("unpitchedSlides"), NumberFormatInfo.InvariantInfo);
            Bends = byte.Parse(reader.GetAttribute("bends"), NumberFormatInfo.InvariantInfo);
            Tapping = byte.Parse(reader.GetAttribute("tapping"), NumberFormatInfo.InvariantInfo);
            Vibrato = byte.Parse(reader.GetAttribute("vibrato"), NumberFormatInfo.InvariantInfo);
            FretHandMutes = byte.Parse(reader.GetAttribute("fretHandMutes"), NumberFormatInfo.InvariantInfo);
            SlapPop = byte.Parse(reader.GetAttribute("slapPop"), NumberFormatInfo.InvariantInfo);
            TwoFingerPicking = byte.Parse(reader.GetAttribute("twoFingerPicking"), NumberFormatInfo.InvariantInfo);
            FifthsAndOctaves = byte.Parse(reader.GetAttribute("fifthsAndOctaves"), NumberFormatInfo.InvariantInfo);
            Syncopation = byte.Parse(reader.GetAttribute("syncopation"), NumberFormatInfo.InvariantInfo);
            BassPick = byte.Parse(reader.GetAttribute("bassPick"), NumberFormatInfo.InvariantInfo);
            Sustain = byte.Parse(reader.GetAttribute("sustain"), NumberFormatInfo.InvariantInfo);
            PathLead = byte.Parse(reader.GetAttribute("pathLead"), NumberFormatInfo.InvariantInfo);
            PathRhythm = byte.Parse(reader.GetAttribute("pathRhythm"), NumberFormatInfo.InvariantInfo);
            PathBass = byte.Parse(reader.GetAttribute("pathBass"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("represent", Represent.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("bonusArr", BonusArrangement.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("standardTuning", StandardTuning.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("nonStandardChords", NonStandardChords.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("barreChords", BarreChords.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("powerChords", PowerChords.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("dropDPower", DropDPower.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("openChords", OpenChords.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("fingerPicking", FingerPicking.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("pickDirection", PickDirection.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("doubleStops", DoubleStops.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("palmMutes", PalmMutes.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("harmonics", Harmonics.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("pinchHarmonics", PinchHarmonics.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("hopo", Hopo.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("tremolo", Tremolo.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("slides", Slides.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("unpitchedSlides", UnpitchedSlides.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("bends", Bends.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("tapping", Tapping.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("vibrato", Vibrato.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("fretHandMutes", FretHandMutes.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("slapPop", SlapPop.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("twoFingerPicking", TwoFingerPicking.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("fifthsAndOctaves", FifthsAndOctaves.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("syncopation", Syncopation.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("bassPick", BassPick.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("sustain", Sustain.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("pathLead", PathLead.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("pathRhythm", PathRhythm.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("pathBass", PathBass.ToString(NumberFormatInfo.InvariantInfo));
        }

        #endregion
    }
}
