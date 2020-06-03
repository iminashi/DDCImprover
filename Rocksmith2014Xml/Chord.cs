using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    [Flags]
    public enum ChordMask : byte
    {
        None         = 0,
        LinkNext     = 1 << 0,
        Accent       = 1 << 1,
        FretHandMute = 1 << 2,
        HighDensity  = 1 << 3,
        Ignore       = 1 << 4,
        PalmMute     = 1 << 5,
        Hopo         = 1 << 6,
        UpStrum      = 1 << 7
    }

    public class Chord : IXmlSerializable, IComparable<Chord>, IHasTimeCode
    {
        #region Quick Access Properties

        public bool IsLinkNext
        {
            get => (Mask & ChordMask.LinkNext) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.LinkNext;
                else
                    Mask &= ~ChordMask.LinkNext;
            }
        }

        public bool IsAccent
        {
            get => (Mask & ChordMask.Accent) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.Accent;
                else
                    Mask &= ~ChordMask.Accent;
            }
        }

        public bool IsFretHandMute
        {
            get => (Mask & ChordMask.FretHandMute) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.FretHandMute;
                else
                    Mask &= ~ChordMask.FretHandMute;
            }
        }

        public bool IsHighDensity
        {
            get => (Mask & ChordMask.HighDensity) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.HighDensity;
                else
                    Mask &= ~ChordMask.HighDensity;
            }
        }

        public bool IsIgnore
        {
            get => (Mask & ChordMask.Ignore) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.Ignore;
                else
                    Mask &= ~ChordMask.Ignore;
            }
        }

        public bool IsPalmMute
        {
            get => (Mask & ChordMask.PalmMute) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.PalmMute;
                else
                    Mask &= ~ChordMask.PalmMute;
            }
        }

        public bool IsHopo
        {
            get => (Mask & ChordMask.Hopo) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.Hopo;
                else
                    Mask &= ~ChordMask.Hopo;
            }
        }

        public bool IsUpStrum
        {
            get => (Mask & ChordMask.UpStrum) != 0;
            set
            {
                if (value)
                    Mask |= ChordMask.UpStrum;
                else
                    Mask &= ~ChordMask.UpStrum;
            }
        }

        #endregion

        public ChordMask Mask { get; set; }

        public int Time { get; set; }

        public int ChordId { get; set; }

        public List<Note>? ChordNotes { get; set; }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: Id: {ChordId}";

        public int CompareTo(Chord other)
            => Time.CompareTo(other.Time);

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader.HasAttributes)
            {
                for (int i = 0; i < reader.AttributeCount; i++)
                {
                    reader.MoveToAttribute(i);

                    switch (reader.Name)
                    {
                        case "time":
                            Time = Utils.TimeCodeFromFloatString(reader.Value);
                            break;
                        case "chordId":
                            ChordId = int.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                            break;

                        case "strum":
                            if(reader.Value == "up")
                                Mask |= ChordMask.UpStrum;
                            break;

                        case "linkNext":
                            Mask |= (ChordMask)Utils.ParseBinary(reader.Value);
                            break;
                        case "accent":
                            Mask |= (ChordMask)(Utils.ParseBinary(reader.Value) << 1);
                            break;
                        case "fretHandMute":
                            Mask |= (ChordMask)(Utils.ParseBinary(reader.Value) << 2);
                            break;
                        case "highDensity":
                            Mask |= (ChordMask)(Utils.ParseBinary(reader.Value) << 3);
                            break;
                        case "ignore":
                            Mask |= (ChordMask)(Utils.ParseBinary(reader.Value) << 4);
                            break;
                        case "palmMute":
                            Mask |= (ChordMask)(Utils.ParseBinary(reader.Value) << 5);
                            break;
                        case "hopo":
                            Mask |= (ChordMask)(Utils.ParseBinary(reader.Value) << 6);
                            break;
                    }
                }

                reader.MoveToElement();
            }

            if (!reader.IsEmptyElement && reader.ReadToDescendant("chordNote"))
            {
                ChordNotes = new List<Note>();

                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    Note cn = new Note();
                    ((IXmlSerializable)cn).ReadXml(reader);
                    ChordNotes.Add(cn);
                }

                reader.ReadEndElement();
            }
            else
            {
                reader.ReadStartElement();
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("chordId", ChordId.ToString(NumberFormatInfo.InvariantInfo));

            if (IsLinkNext)
                writer.WriteAttributeString("linkNext", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("linkNext", "0");

            if (IsAccent)
                writer.WriteAttributeString("accent", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("accent", "0");

            if (IsFretHandMute)
                writer.WriteAttributeString("fretHandMute", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("fretHandMute", "0");

            if (IsHighDensity)
                writer.WriteAttributeString("highDensity", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("highDensity", "0");

            if (IsIgnore)
                writer.WriteAttributeString("ignore", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("ignore", "0");

            if (IsPalmMute)
                writer.WriteAttributeString("palmMute", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("palmMute", "0");

            if (IsHopo)
                writer.WriteAttributeString("hopo", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("hopo", "0");

            if (IsUpStrum)
                writer.WriteAttributeString("strum", "up");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("strum", "down");

            if (ChordNotes?.Count > 0)
            {
                foreach (var chordNote in ChordNotes)
                {
                    writer.WriteStartElement("chordNote");
                    ((IXmlSerializable)chordNote).WriteXml(writer);
                    writer.WriteEndElement();
                }
            }
        }

        #endregion
    }
}