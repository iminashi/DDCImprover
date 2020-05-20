using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class Anchor : IXmlSerializable, IHasTimeCode, IEquatable<Anchor>
    {
        public byte Fret { get; set; }
        public float Time { get; set; }
        public byte Width { get; set; }

        public Anchor() { }

        public Anchor(byte fret, float time, byte width = 4)
        {
            Fret = fret;
            Time = time;
            Width = width;
        }

        public override string ToString()
            => $"{Time:F3}: Fret {Fret}, Width: {Width:F3}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Time = float.Parse(reader.GetAttribute("time"), NumberFormatInfo.InvariantInfo);
            Fret = byte.Parse(reader.GetAttribute("fret"), NumberFormatInfo.InvariantInfo);
            Width = (byte)float.Parse(reader.GetAttribute("width"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Time.ToString("F3", NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("fret", Fret.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("width", Width.ToString("F3", NumberFormatInfo.InvariantInfo));
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
            => obj is Anchor other && Equals(other);

        public bool Equals(Anchor other)
        {
            if (other is null)
                return false;

            return Fret == other.Fret
                && Width == other.Width
                && Utils.TimeEqualToMilliseconds(Time, other.Time);
        }

        public override int GetHashCode()
            => (Time, Fret, Width).GetHashCode();

        public static bool operator ==(Anchor left, Anchor right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(Anchor left, Anchor right)
            => !(left == right);

        #endregion
    }
}