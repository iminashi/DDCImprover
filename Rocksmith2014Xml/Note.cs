using System;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Rocksmith2014Xml
{
    [Flags]
    public enum NoteMask : ushort
    {
        None = 0,
        LinkNext = 1 << 0,
        Accent = 1 << 1,
        HammerOn = 1 << 2,
        Harmonic = 1 << 3,
        Ignore = 1 << 4,
        Mute = 1 << 5,
        PalmMute = 1 << 6,
        PullOff = 1 << 7,
        Tremolo = 1 << 8,
        HarmonicPinch = 1 << 9,
        PickDirection = 1 << 10,
        Slap = 1 << 11,
        Pluck = 1 << 12,
        RightHand = 1 << 13
    }

    public class Note : IXmlSerializable, IComparable<Note>, IHasTimeCode
    {
        public Note(Note other)
        {
            Mask = other.Mask;
            Time = other.Time;
            String = other.String;
            Fret = other.Fret;
            Sustain = other.Sustain;

            Bend = other.Bend;
            LeftHand = other.LeftHand;
            SlideTo = other.SlideTo;
            SlideUnpitchTo = other.SlideUnpitchTo;
            Tap = other.Tap;
            Vibrato = other.Vibrato;

            if (other.BendValues != null)
            {
                BendValues = new BendValueCollection();
                BendValues.AddRange(other.BendValues);
            }
        }

        public Note() { }

        #region Quick Access Properties

        public bool IsLinkNext
        {
            get => (Mask & NoteMask.LinkNext) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.LinkNext;
                else
                    Mask &= ~NoteMask.LinkNext;
            }
        }

        public bool IsAccent
        {
            get => (Mask & NoteMask.Accent) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Accent;
                else
                    Mask &= ~NoteMask.Accent;
            }
        }

        public bool IsHammerOn
        {
            get => (Mask & NoteMask.HammerOn) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.HammerOn;
                else
                    Mask &= ~NoteMask.HammerOn;
            }
        }

        public bool IsHarmonic
        {
            get => (Mask & NoteMask.Harmonic) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Harmonic;
                else
                    Mask &= ~NoteMask.Harmonic;
            }
        }

        public bool IsHarmonicPinch
        {
            get => (Mask & NoteMask.HarmonicPinch) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.HarmonicPinch;
                else
                    Mask &= ~NoteMask.HarmonicPinch;
            }
        }

        public bool IsIgnore
        {
            get => (Mask & NoteMask.Ignore) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Ignore;
                else
                    Mask &= ~NoteMask.Ignore;
            }
        }

        public bool IsMute
        {
            get => (Mask & NoteMask.Mute) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Mute;
                else
                    Mask &= ~NoteMask.Mute;
            }
        }

        public bool IsPalmMute
        {
            get => (Mask & NoteMask.PalmMute) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.PalmMute;
                else
                    Mask &= ~NoteMask.PalmMute;
            }
        }

        public bool IsPullOff
        {
            get => (Mask & NoteMask.PullOff) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.PullOff;
                else
                    Mask &= ~NoteMask.PullOff;
            }
        }

        public bool IsTremolo
        {
            get => (Mask & NoteMask.Tremolo) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Tremolo;
                else
                    Mask &= ~NoteMask.Tremolo;
            }
        }

        public bool IsSlap
        {
            get => (Mask & NoteMask.Slap) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Slap;
                else
                    Mask &= ~NoteMask.Slap;
            }
        }

        public bool IsPluck
        {
            get => (Mask & NoteMask.Pluck) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Pluck;
                else
                    Mask &= ~NoteMask.Pluck;
            }
        }

        public bool IsRightHand
        {
            get => (Mask & NoteMask.RightHand) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.RightHand;
                else
                    Mask &= ~NoteMask.RightHand;
            }
        }

        public bool IsVibrato => Vibrato != 0;
        public bool IsBend => Bend != 0;
        public bool IsSlide => SlideTo != -1;
        public bool IsUnpitchedSlide => SlideUnpitchTo != -1;
        public bool IsTap => Tap != 0;

        #endregion

        #region Properties

        public int Time { get; set; }

        public int Sustain { get; set; }

        public float Bend { get; set; }
        public sbyte Fret { get; set; }

        public bool Hopo
            => (Mask & NoteMask.HammerOn) != 0 || (Mask & NoteMask.PullOff) != 0;

        public sbyte LeftHand { get; set; } = -1;
        public sbyte SlideTo { get; set; } = -1;
        public sbyte SlideUnpitchTo { get; set; } = -1;
        public sbyte String { get; set; }

        public byte Tap { get; set; }
        public byte Vibrato { get; set; }

        public NoteMask Mask { get; set; }

        public BendValueCollection? BendValues { get; set; }

        #endregion

        public override string ToString()
            => $"{Utils.TimeCodeToString(Time)}: Fret: {Fret}, String: {String}";

        public int CompareTo(Note other)
            => Time.CompareTo(other.Time);

        #region IXmlSerializable Implementation

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
                    case "sustain":
                        Sustain = Utils.TimeCodeFromFloatString(reader.Value);
                        break;
                    case "bend":
                        Bend = float.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "fret":
                        Fret = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "leftHand":
                        LeftHand = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "slideTo":
                        SlideTo = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "slideUnpitchTo":
                        SlideUnpitchTo = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "string":
                        String = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "vibrato":
                        Vibrato = byte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "tap":
                        Tap = byte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;

                    case "linkNext":
                        Mask |= (NoteMask)Utils.ParseBinary(reader.Value);
                        break;
                    case "accent":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 1);
                        break;
                    case "hammerOn":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 2);
                        break;
                    case "harmonic":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 3);
                        break;
                    case "ignore":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 4);
                        break;
                    case "mute":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 5);
                        break;
                    case "palmMute":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 6);
                        break;
                    case "pullOff":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 7);
                        break;
                    case "tremolo":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 8);
                        break;
                    case "harmonicPinch":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 9);
                        break;
                    case "pickDirection":
                        Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 10);
                        break;
                    case "slap":
                        if(sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo) != -1)
                            Mask |= NoteMask.Slap;
                        break;
                    case "pluck":
                        if(sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo) != -1)
                            Mask |= NoteMask.Pluck;
                        break;
                    case "rightHand":
                        if (sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo) != -1)
                            Mask |= NoteMask.RightHand;
                        break;
                }
            }

            reader.MoveToElement();

            if (!reader.IsEmptyElement && reader.ReadToDescendant("bendValues"))
            {
                BendValues = new BendValueCollection();

                ((IXmlSerializable)BendValues).ReadXml(reader);

                reader.ReadEndElement(); // </note>
            }
            else
            {
                reader.ReadStartElement();
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Utils.TimeCodeToString(Time));
            writer.WriteAttributeString("string", String.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("fret", Fret.ToString(NumberFormatInfo.InvariantInfo));

            if (Sustain > 0.0f)
                writer.WriteAttributeString("sustain", Utils.TimeCodeToString(Sustain));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("sustain", "0.000");

            if (IsLinkNext)
                writer.WriteAttributeString("linkNext", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("linkNext", "0");

            if (IsAccent)
                writer.WriteAttributeString("accent", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("accent", "0");

            if (IsBend)
                writer.WriteAttributeString("bend", Bend.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("bend", "0");

            if (IsHammerOn)
                writer.WriteAttributeString("hammerOn", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("hammerOn", "0");

            if (IsHarmonic)
                writer.WriteAttributeString("harmonic", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("harmonic", "0");

            if (Hopo)
                writer.WriteAttributeString("hopo", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("hopo", "0");

            if (IsIgnore)
                writer.WriteAttributeString("ignore", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("ignore", "0");

            if (LeftHand != -1)
                writer.WriteAttributeString("leftHand", LeftHand.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("leftHand", "-1");

            if (IsMute)
                writer.WriteAttributeString("mute", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("mute", "0");

            if (IsPalmMute)
                writer.WriteAttributeString("palmMute", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("palmMute", "0");

            if (IsPluck)
                writer.WriteAttributeString("pluck", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("pluck", "-1");

            if (IsPullOff)
                writer.WriteAttributeString("pullOff", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("pullOff", "0");

            if (IsSlap)
                writer.WriteAttributeString("slap", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("slap", "-1");

            if (IsSlide)
                writer.WriteAttributeString("slideTo", SlideTo.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("slideTo", "-1");

            if (IsTremolo)
                writer.WriteAttributeString("tremolo", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("tremolo", "0");

            if (IsHarmonicPinch)
                writer.WriteAttributeString("harmonicPinch", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("harmonicPinch", "0");

            if ((Mask & NoteMask.PickDirection) != 0)
                writer.WriteAttributeString("pickDirection", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("pickDirection", "0");

            if (IsRightHand)
                writer.WriteAttributeString("rightHand", "1");
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("rightHand", "-1");

            if (IsUnpitchedSlide)
                writer.WriteAttributeString("slideUnpitchTo", SlideUnpitchTo.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("slideUnpitchTo", "-1");

            if (IsTap)
                writer.WriteAttributeString("tap", Tap.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("tap", "0");

            if (IsVibrato)
                writer.WriteAttributeString("vibrato", Vibrato.ToString(NumberFormatInfo.InvariantInfo));
            else if (!InstrumentalArrangement.UseAbridgedXml)
                writer.WriteAttributeString("vibrato", "0");

            if (BendValues?.Count > 0)
            {
                writer.WriteStartElement("bendValues");
                ((IXmlSerializable)BendValues).WriteXml(writer);
                writer.WriteEndElement(); // </bendValues>
            }
        }

        XmlSchema? IXmlSerializable.GetSchema() => null;

        #endregion
    }
}
