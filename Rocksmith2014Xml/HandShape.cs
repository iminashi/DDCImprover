using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class HandShape : IXmlSerializable, IHasTimeCode
    {
        public short ChordId { get; set; }
        public int EndTime { get; set; }
        public int StartTime { get; set; }

        public int Time => StartTime;

        public HandShape() { }

        public HandShape(short chordId, int startTime, int endTime)
        {
            ChordId = chordId;
            StartTime = startTime;
            EndTime = endTime;
        }

        public HandShape(HandShape other)
        {
            ChordId = other.ChordId;
            StartTime = other.StartTime;
            EndTime = other.EndTime;
        }

        public override string ToString()
            => $"{Utils.TimeCodeToString(StartTime)} - {Utils.TimeCodeToString(EndTime)}: Chord ID {ChordId}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            ChordId = short.Parse(reader.GetAttribute("chordId"), NumberFormatInfo.InvariantInfo);
            StartTime = Utils.TimeCodeFromFloatString(reader.GetAttribute("startTime"));
            EndTime = Utils.TimeCodeFromFloatString(reader.GetAttribute("endTime"));

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("chordId", ChordId.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("startTime", Utils.TimeCodeToString(StartTime));
            writer.WriteAttributeString("endTime", Utils.TimeCodeToString(EndTime));
        }

        #endregion
    }
}