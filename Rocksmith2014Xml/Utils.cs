using System;
using System.Diagnostics;

namespace Rocksmith2014Xml
{
    public static class Utils
    {
        internal static byte ParseBinary(string text)
        {
            Debug.Assert(text.Length == 1 && (text[0] == '0' || text[0] == '1'));

            char c = text[0];
            unchecked
            {
                c -= '0';
            }

            if (c > 1)
                return 1; //throw new InvalidCastException($"Expected either 1 or 0, instead got: {text}");
            else
                return (byte)c;
        }

        private const float Epsilon = 0.0009f;

        public static bool TimeEqualToMilliseconds(float a, float b)
        {
            if(a == b)
            {
                return true;
            }
            else
            {
                return Math.Abs(a - b) < Epsilon;
            }
        }

        internal static int ShiftAndWrap(int value, int positions)
        {
            positions &= 0x1F;

            // Save the existing bit pattern, but interpret it as an unsigned integer.
            uint number = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);

            // Preserve the bits to be discarded.
            uint wrapped = number >> (32 - positions);

            // Shift and wrap the discarded bits.
            return BitConverter.ToInt32(BitConverter.GetBytes((number << positions) | wrapped), 0);
        }
    }
}
