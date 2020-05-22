﻿using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class Section : IXmlSerializable, IHasTimeCode
    {
        public string Name { get; set; } = string.Empty;
        public int Number { get; set; }
        public float Time { get; set; }

        public Section(string name, float startTime, int number)
        {
            Name = name;
            Number = number;
            Time = startTime;
        }

        public Section() { }

        public override string ToString()
            => $"{Time:F3}: {Name} #{Number}";

        #region IXmlSerializable Implementation

        XmlSchema? IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Name = reader.GetAttribute("name");
            Number = int.Parse(reader.GetAttribute("number"), NumberFormatInfo.InvariantInfo);
            Time = float.Parse(reader.GetAttribute("startTime"), NumberFormatInfo.InvariantInfo);

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("number", Number.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("startTime", Time.ToString("F3", NumberFormatInfo.InvariantInfo));
        }

        #endregion
    }
}