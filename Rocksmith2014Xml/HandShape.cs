using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class HandShape : IXmlSerializable, IHasTimeCode
    {
        public int ChordId { get; set; }
        public uint EndTime { get; set; }
        public uint StartTime { get; set; }

        public uint Time => StartTime;

        public HandShape(int chordId, uint startTime, uint endTime)
        {
            ChordId = chordId;
            EndTime = endTime;
            StartTime = startTime;
        }

        public HandShape() { }

        public override string ToString()
            => $"{Utils.TimeCodeToString(StartTime)} - {Utils.TimeCodeToString(EndTime)}: Chord ID {ChordId}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            ChordId = int.Parse(reader.GetAttribute("chordId"), NumberFormatInfo.InvariantInfo);
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