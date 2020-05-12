using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    [XmlRoot("linkedDiff", Namespace = "")]
    public struct LinkedDiff : IXmlSerializable, IEquatable<LinkedDiff>
    {
        public int ChildId { get; private set; }
        public int ParentId { get; private set; }

        #region IXmlSerializable Implementation

        XmlSchema IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            ChildId = int.Parse(reader.GetAttribute("childId"), NumberFormatInfo.InvariantInfo);
            ParentId = int.Parse(reader.GetAttribute("parentId"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("childId", ChildId.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("parentId", ParentId.ToString(NumberFormatInfo.InvariantInfo));
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
            => obj is LinkedDiff other && Equals(other);

        public bool Equals(LinkedDiff other)
            => ChildId == other.ChildId && ParentId == other.ParentId;

        public override int GetHashCode()
            => (ChildId, ParentId).GetHashCode();

        public static bool operator ==(LinkedDiff left, LinkedDiff right)
            => left.Equals(right);

        public static bool operator !=(LinkedDiff left, LinkedDiff right)
            => !(left == right);

        #endregion
    }
}