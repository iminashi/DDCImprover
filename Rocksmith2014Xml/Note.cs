﻿using System;
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
        PickDirection = 1 << 10
        //Tap = 1 << 11
        // TODO: Add righthand, pluck and slap?
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
            Pluck = other.Pluck;
            RightHand = other.RightHand;
            Slap = other.Slap;
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

        /*public bool IsTap
        {
            get => (Mask & NoteMask.Tap) != 0;
            set
            {
                if (value)
                    Mask |= NoteMask.Tap;
                else
                    Mask &= ~NoteMask.Tap;
            }
        }*/

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

        public bool IsVibrato => Vibrato != 0;
        public bool IsBend => Bend != 0;
        public bool IsSlide => SlideTo != -1;
        public bool IsUnpitchedSlide => SlideUnpitchTo != -1;
        public bool IsTap => Tap != 0;

        #endregion

        #region Properties

        private float _time;
        private float _sustain;

        public float Time
        {
            get => _time;
            set
            {
                if (value < 0.0f)
                    throw new InvalidOperationException("Time cannot be less than zero");

                _time = value;
            }
        }

        public float Sustain
        {
            get => _sustain;
            set
            {
                if (value < 0.0f)
                    throw new InvalidOperationException("Sustain cannot be less than zero");

                _sustain = value;
            }
        }

        public float Bend { get; set; }
        public sbyte Fret { get; set; }

        public bool Hopo
            => (Mask & NoteMask.HammerOn) != 0 || (Mask & NoteMask.PullOff) != 0;

        public sbyte LeftHand { get; set; } = -1;
        public sbyte Pluck { get; set; } = -1;
        public sbyte Slap { get; set; } = -1;
        public sbyte SlideTo { get; set; } = -1;
        public sbyte SlideUnpitchTo { get; set; } = -1;
        public sbyte RightHand { get; set; } = -1;
        public sbyte String { get; set; }

        public byte Tap { get; set; }
        public byte Vibrato { get; set; }

        public NoteMask Mask { get; set; }

        public BendValueCollection BendValues { get; set; }

        #endregion

        public override string ToString()
        {
            return $"{Time.ToString("F3", NumberFormatInfo.InvariantInfo)}: Fret: {Fret}, String: {String}";
        }

        public int CompareTo(Note other)
        {
            return Time.CompareTo(other.Time);
        }

        #region IXmlSerializable Implementation

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            for (int i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                switch (reader.Name)
                {
                    case "time":
                        Time = float.Parse(reader.Value, CultureInfo.InvariantCulture);
                        break;
                    case "sustain":
                        Sustain = float.Parse(reader.Value, CultureInfo.InvariantCulture);
                        break;
                    case "bend":
                        Bend = float.Parse(reader.Value, CultureInfo.InvariantCulture);
                        break;
                    case "fret":
                        Fret = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "leftHand":
                        LeftHand = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "pluck":
                        Pluck = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
                        break;
                    case "slap":
                        Slap = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
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
                    case "rightHand":
                        RightHand = sbyte.Parse(reader.Value, NumberFormatInfo.InvariantInfo);
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
                    //case "tap":
                    //    Mask |= (NoteMask)(Utils.ParseBinary(reader.Value) << 11);
                    //    break;
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
            writer.WriteAttributeString("time", Time.ToString("F3", NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("string", String.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("fret", Fret.ToString(NumberFormatInfo.InvariantInfo));

            if (Sustain > 0.0f)
                writer.WriteAttributeString("sustain", Sustain.ToString("F3", NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("sustain", "0.000");

            if (IsLinkNext)
                writer.WriteAttributeString("linkNext", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("linkNext", "0");

            if (IsAccent)
                writer.WriteAttributeString("accent", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("accent", "0");

            if (IsBend)
                writer.WriteAttributeString("bend", Bend.ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("bend", "0");

            if (IsHammerOn)
                writer.WriteAttributeString("hammerOn", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("hammerOn", "0");

            if (IsHarmonic)
                writer.WriteAttributeString("harmonic", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("harmonic", "0");

            if (Hopo)
                writer.WriteAttributeString("hopo", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("hopo", "0");

            if (IsIgnore)
                writer.WriteAttributeString("ignore", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("ignore", "0");

            if (LeftHand != -1)
                writer.WriteAttributeString("leftHand", LeftHand.ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("leftHand", "-1");

            if (IsMute)
                writer.WriteAttributeString("mute", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("mute", "0");

            if (IsPalmMute)
                writer.WriteAttributeString("palmMute", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("palmMute", "0");

            if (Pluck != -1)
                writer.WriteAttributeString("pluck", Pluck.ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("pluck", "-1");

            if (IsPullOff)
                writer.WriteAttributeString("pullOff", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("pullOff", "0");

            if (Slap != -1)
                writer.WriteAttributeString("slap", Slap.ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("slap", "-1");

            if (SlideTo != -1)
                writer.WriteAttributeString("slideTo", SlideTo.ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("slideTo", "-1");

            if (IsTremolo)
                writer.WriteAttributeString("tremolo", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("tremolo", "0");

            if (IsHarmonicPinch)
                writer.WriteAttributeString("harmonicPinch", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("harmonicPinch", "0");

            if ((Mask & NoteMask.PickDirection) != 0)
                writer.WriteAttributeString("pickDirection", "1");
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("pickDirection", "0");

            if (RightHand != -1)
                writer.WriteAttributeString("rightHand", RightHand.ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("rightHand", "-1");

            if (SlideUnpitchTo != -1)
                writer.WriteAttributeString("slideUnpitchTo", SlideUnpitchTo.ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("slideUnpitchTo", "-1");

            if (IsTap)
                writer.WriteAttributeString("tap", Tap.ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("tap", "0");

            if (IsVibrato)
                writer.WriteAttributeString("vibrato", Vibrato.ToString(NumberFormatInfo.InvariantInfo));
            else if (!RS2014Song.UseAbridgedXml)
                writer.WriteAttributeString("vibrato", "0");

            if (BendValues?.Count > 0)
            {
                writer.WriteStartElement("bendValues");
                ((IXmlSerializable)BendValues).WriteXml(writer);
                writer.WriteEndElement(); // </bendValues>
            }
        }

        XmlSchema IXmlSerializable.GetSchema() => null;

        #endregion
    }
}
