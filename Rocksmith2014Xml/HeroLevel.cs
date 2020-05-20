using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public struct HeroLevel : IXmlSerializable, IEquatable<HeroLevel>
    {
        public byte Hero { get; private set; }
        public byte Difficulty { get; private set; }

        public HeroLevel(byte hero, byte difficulty)
        {
            Hero = hero;
            Difficulty = difficulty;
        }

        public override string ToString()
            => $"Hero {Hero}: Difficulty {Difficulty}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Hero = byte.Parse(reader.GetAttribute("hero"), NumberFormatInfo.InvariantInfo);
            Difficulty = byte.Parse(reader.GetAttribute("difficulty"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("hero", Hero.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("difficulty", Difficulty.ToString(NumberFormatInfo.InvariantInfo));
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
            => obj is HeroLevel other && Equals(other);

        public bool Equals(HeroLevel other)
            => Hero == other.Hero && Difficulty == other.Difficulty;

        public override int GetHashCode()
            => (Hero, Difficulty).GetHashCode();

        public static bool operator ==(HeroLevel left, HeroLevel right)
            => left.Equals(right);

        public static bool operator !=(HeroLevel left, HeroLevel right)
            => !(left == right);

        #endregion
    }
}