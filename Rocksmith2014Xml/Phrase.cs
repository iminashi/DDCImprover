using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    [Flags]
    public enum PhraseMask : byte
    {
        None = 0,
        Disparity = 1 << 0,
        Ignore = 1 << 1,
        Solo = 1 << 2
    }

    public sealed class Phrase : IXmlSerializable
    {
        #region Quick Access Properties

        public bool IsDisparity => (Mask & PhraseMask.Disparity) != 0;
        public bool IsIgnore => (Mask & PhraseMask.Ignore) != 0;
        public bool IsSolo => (Mask & PhraseMask.Solo) != 0;

        #endregion

        public PhraseMask Mask { get; set; }
        public byte MaxDifficulty { get; set; }
        public string Name { get; set; }

        public Phrase(string name, byte maxDifficulty, PhraseMask mask)
        {
            Name = name;
            MaxDifficulty = maxDifficulty;
            Mask = mask;
        }

        public Phrase() { }

        public override string ToString()
        {
            return $"{Name}, Max Diff: {MaxDifficulty.ToString()}, Mask: {Mask.ToString()}";
        }

        #region IXmlSerializable Serializable

        XmlSchema IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            if (reader.HasAttributes)
            {
                for (int i = 0; i < reader.AttributeCount; i++)
                {
                    reader.MoveToAttribute(i);

                    switch (reader.Name)
                    {
                        case "disparity":
                            Mask |= (PhraseMask)Utils.ParseBinary(reader.Value);
                            break;
                        case "ignore":
                            Mask |= (PhraseMask)(Utils.ParseBinary(reader.Value) << 1);
                            break;
                        case "solo":
                            Mask |= (PhraseMask)(Utils.ParseBinary(reader.Value) << 2);
                            break;
                        case "maxDifficulty":
                            MaxDifficulty = byte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                            break;
                        case "name":
                            Name = reader.Value;
                            break;
                    }
                }

                reader.MoveToElement();
            }

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("maxDifficulty", MaxDifficulty.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("name", Name);

            if (IsDisparity)
                writer.WriteAttributeString("disparity", "1");
            else if(!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("disparity", "0");

            if (IsIgnore)
                writer.WriteAttributeString("ignore", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("ignore", "0");

            if (IsSolo)
                writer.WriteAttributeString("solo", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("solo", "0");
        }

        #endregion

        /*#region Equality

        public override bool Equals(object obj)
            => obj is Phrase other && Equals(other);

        public bool Equals(Phrase other)
        {
            return Mask == other.Mask
                && MaxDifficulty == other.MaxDifficulty
                && Name == other.Name;
        }

        public override int GetHashCode()
            => (Mask, MaxDifficulty, Name).GetHashCode();

        public static bool operator ==(Phrase left, Phrase right)
            => left.Equals(right);

        public static bool operator !=(Phrase left, Phrase right)
            => !(left == right);

        #endregion*/
    }
}