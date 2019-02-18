using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class Ebeat : IXmlSerializable, IEquatable<Ebeat>, IHasTimeCode
    {
        public float Time { get; set; }
        public int Measure { get; set; } = -1;

        public Ebeat(float time, int measure)
        {
            Time = time;
            Measure = measure;
        }

        public Ebeat() { }

        public override string ToString()
        {
            string result = Time.ToString("F3");

            if (Measure != -1)
                result += $": Measure: {Measure.ToString()}";

            return result;
        }

        #region IXmlSerializable Implementation

        XmlSchema IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                switch (reader.Name)
                {
                    case "time":
                        Time = float.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "measure":
                        Measure = int.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                }
            }

            reader.MoveToElement();

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Time.ToString("F3", NumberFormatInfo.InvariantInfo));

            if (Measure != -1)
                writer.WriteAttributeString("measure", Measure.ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("measure", "-1");
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
            => obj is Ebeat other && Equals(other);

        public bool Equals(Ebeat other)
        {
            if (ReferenceEquals(this, other))
                return true;

            return !(other is null) && Utils.TimeEqualToMilliseconds(Time, other.Time) && Measure == other.Measure;
        }

        public override int GetHashCode()
            => Utils.ShiftAndWrap(Time.GetHashCode(), 2) ^ Measure.GetHashCode();

        public static bool operator ==(Ebeat left, Ebeat right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(Ebeat left, Ebeat right)
            => !(left == right);

        #endregion
    }
}