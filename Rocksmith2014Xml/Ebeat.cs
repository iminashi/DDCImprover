using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class Ebeat : IXmlSerializable, IEquatable<Ebeat>, IHasTimeCode
    {
        public int Time { get; set; }
        public int Measure { get; set; } = -1;

        public Ebeat(int time, int measure)
        {
            Time = time;
            Measure = measure;
        }

        public Ebeat() { }

        public override string ToString()
        {
            string result = Utils.TimeCodeToString(Time);

            if (Measure != -1)
                result += $": Measure: {Measure}";

            return result;
        }

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                switch (reader.Name)
                {
                    case "time":
                        Time = Utils.TimeCodeFromFloatString(reader.Value);
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
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));

            if (Measure != -1)
                writer.WriteAttributeString("measure", Measure.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
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

            return !(other is null) && Time == other.Time && Measure == other.Measure;
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