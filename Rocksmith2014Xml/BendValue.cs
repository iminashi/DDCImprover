using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public struct BendValue: IXmlSerializable, IEquatable<BendValue>, IHasTimeCode
    {
        public float Time { get; private set; }
        public float Step { get; private set; }

        public BendValue(float time, float step)
        {
            Time = time;
            Step = step;
        }

        public override string ToString()
            => $"{Time:F3}: {Step:F2}";

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
                    case "step":
                        Step = float.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                }
            }

            reader.MoveToElement();

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Time.ToString("F3", NumberFormatInfo.InvariantInfo));
            if(Step != 0f || !RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("step", Step.ToString("F3", NumberFormatInfo.InvariantInfo));
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
            => obj is BendValue other && Equals(other);

        public bool Equals(BendValue other)
            => Utils.TimeEqualToMilliseconds(Time, other.Time) && Step == other.Step;

        public override int GetHashCode()
            => Utils.ShiftAndWrap(Time.GetHashCode(), 2) ^ Step.GetHashCode();

        public static bool operator ==(BendValue left, BendValue right)
            => left.Equals(right);

        public static bool operator !=(BendValue left, BendValue right)
            => !(left == right);

        #endregion
    }
}