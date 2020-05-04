using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class HandShape : IXmlSerializable, IHasTimeCode//, IEquatable<HandShape>
    {
        public int ChordId { get; set; }
        public float EndTime { get; set; }
        public float StartTime { get; set; }

        public float Time => StartTime;

        public HandShape(int chordId, float startTime, float endTime)
        {
            ChordId = chordId;
            EndTime = endTime;
            StartTime = startTime;
        }

        public HandShape() { }

        public override string ToString()
            => $"{StartTime:F3} - {EndTime:F3}: Chord ID {ChordId}";

        #region IXmlSerializable Implementation

        XmlSchema IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            ChordId = int.Parse(reader.GetAttribute("chordId"), NumberFormatInfo.InvariantInfo);
            StartTime = float.Parse(reader.GetAttribute("startTime"), NumberFormatInfo.InvariantInfo);
            EndTime = float.Parse(reader.GetAttribute("endTime"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("chordId", ChordId.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("startTime", StartTime.ToString("F3", NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("endTime", EndTime.ToString("F3", NumberFormatInfo.InvariantInfo));
        }

        #endregion

        /*#region Equality

        public override bool Equals(object obj)
            => obj is HandShape other && Equals(other);

        public bool Equals(HandShape other)
        {
            return ChordId == other.ChordId
                && Utils.TimeEqualToMilliseconds(StartTime, other.StartTime)
                && Utils.TimeEqualToMilliseconds(EndTime, other.EndTime);
        }

        public override int GetHashCode()
            => (ChordId, EndTime, StartTime).GetHashCode();

        public static bool operator ==(HandShape left, HandShape right)
            => left.Equals(right);

        public static bool operator !=(HandShape left, HandShape right)
            => !(left == right);

        #endregion*/
    }
}