using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public struct BendValue: IXmlSerializable, IEquatable<BendValue>, IHasTimeCode
    {
        public int Time { get; private set; }
        public float Step { get; private set; }

        public BendValue(int time, float step)
        {
            Time = time;
            Step = step;
        }

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: {Step:F2}";

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
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            if(Step != 0f || !InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("step", Step.ToString("F3", NumberFormatInfo.InvariantInfo));
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
            => obj is BendValue other && Equals(other);

        public bool Equals(BendValue other)
            => Time == other.Time && Step == other.Step;

        public override int GetHashCode()
            => Utils.ShiftAndWrap(Time.GetHashCode(), 2) ^ Step.GetHashCode();

        public static bool operator ==(BendValue left, BendValue right)
            => left.Equals(right);

        public static bool operator !=(BendValue left, BendValue right)
            => !(left == right);

        #endregion
    }
}