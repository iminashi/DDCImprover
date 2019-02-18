using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    public sealed class ChordTemplate : IXmlSerializable
    {
        public string ChordName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        public sbyte[] Fingers =
        {
            -1,
            -1,
            -1,
            -1,
            -1,
            -1
        };

        public sbyte[] Frets =
        {
            -1,
            -1,
            -1,
            -1,
            -1,
            -1
        };

        public override string ToString()
        {
            string result = string.Empty;

            for (int i = 0; i < 6; i++)
            {
                if(Frets[i] != -1)
                {
                    result += $"Fret{i}={Frets[i]} ";
                }

                if(Fingers[i] != -1)
                {
                    result += $"Finger{i}={Fingers[i]} ";
                }
            }

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
                    case "chordName":
                        ChordName = reader.Value;
                        break;
                    case "displayName":
                        DisplayName = reader.Value;
                        break;
                    case "finger0":
                        Fingers[0] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "finger1":
                        Fingers[1] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "finger2":
                        Fingers[2] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "finger3":
                        Fingers[3] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "finger4":
                        Fingers[4] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "finger5":
                        Fingers[5] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;

                    case "fret0":
                        Frets[0] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "fret1":
                        Frets[1] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "fret2":
                        Frets[2] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "fret3":
                        Frets[3] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "fret4":
                        Frets[4] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "fret5":
                        Frets[5] = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                }
            }

            reader.MoveToElement();

            reader.ReadStartElement();
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("chordName", ChordName);
            writer.WriteAttributeString("displayName", DisplayName);

            if (Fingers[0] != -1)
                writer.WriteAttributeString("finger0", Fingers[0].ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("finger0", "-1");

            if (Fingers[1] != -1)
                writer.WriteAttributeString("finger1", Fingers[1].ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("finger1", "-1");

            if (Fingers[2] != -1)
                writer.WriteAttributeString("finger2", Fingers[2].ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("finger2", "-1");

            if (Fingers[3] != -1)
                writer.WriteAttributeString("finger3", Fingers[3].ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("finger3", "-1");

            if (Fingers[4] != -1)
                writer.WriteAttributeString("finger4", Fingers[4].ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("finger4", "-1");

            if (Fingers[5] != -1)
                writer.WriteAttributeString("finger5", Fingers[5].ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("finger5", "-1");

            if (Frets[0] != -1)
                writer.WriteAttributeString("fret0", Frets[0].ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("fret0", "-1");

            if (Frets[1] != -1)
                writer.WriteAttributeString("fret1", Frets[1].ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("fret1", "-1");

            if (Frets[2] != -1)
                writer.WriteAttributeString("fret2", Frets[2].ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("fret2", "-1");

            if (Frets[3] != -1)
                writer.WriteAttributeString("fret3", Frets[3].ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("fret3", "-1");

            if (Frets[4] != -1)
                writer.WriteAttributeString("fret4", Frets[4].ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("fret4", "-1");

            if (Frets[5] != -1)
                writer.WriteAttributeString("fret5", Frets[5].ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("fret5", "-1");
        }

        #endregion
    }
}