using Rocksmith2014Xml;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DDCImprover.Core
{
    public static class Extensions
    {
        public static string TimeToString(this float num)
        {
            if (XMLProcessor.Preferences.DisplayTimesInSeconds)
            {
                return num.ToString("F3", NumberFormatInfo.InvariantInfo);
            }
            else
            {
                int minutes = (int)num / 60;
                float secMs = num - (minutes * 60);
                return $"{minutes.ToString("D2", NumberFormatInfo.InvariantInfo)}:{secMs.ToString("00.000", NumberFormatInfo.InvariantInfo)}";
            }
        }

        /// <summary>
        /// Returns index of element at the time to find in a list ordered by time.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements"></param>
        /// <param name="timeToFind"></param>
        /// <returns>Index of the element, -1 if not found.</returns>
        public static int FindIndexByTime<T>(this IList<T> elements, float timeToFind)
            where T : IHasTimeCode
        {
            for(int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (Utils.TimeEqualToMilliseconds(element.Time, timeToFind))
                    return i;
                else if (element.Time > timeToFind)
                    return -1;
            }

            return -1;
        }

        public static T FindByTime<T>(this IList<T> elements, float timeToFind)
            where T : IHasTimeCode
        {
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (Utils.TimeEqualToMilliseconds(element.Time, timeToFind))
                    return element;
                else if (element.Time > timeToFind)
                    return default;
            }

            return default;
        }

        public static void InsertByTime<T>(this List<T> elements, T element)
            where T : IHasTimeCode
        {
            int insertIndex = elements.FindIndex(hs => hs.Time > element.Time);
            if (insertIndex != -1)
                elements.Insert(insertIndex, element);
            else
                elements.Add(element);
        }

        /// <summary>
        /// Skips a specified number of elements at the end of a sequence.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="enumerable"></param>
        /// <param name="skipCount">Number of elements to skip.</param>
        /// <remarks>https://blogs.msdn.microsoft.com/ericwhite/2008/11/14/the-skiplast-extension-method/</remarks>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> enumerable, int skipCount)
        {
            if (skipCount < 0)
                throw new ArgumentException("Number of elements to skip cannot be negative.");

            return SkipLastImpl(enumerable, skipCount);

            // Implementation as local function
            static IEnumerable<T> SkipLastImpl(IEnumerable<T> source, int count)
            {
                Queue<T> saveList = new Queue<T>(count + 1);
                foreach (T item in source)
                {
                    saveList.Enqueue(item);
                    if (count > 0)
                    {
                        --count;
                        continue;
                    }

                    yield return saveList.Dequeue();
                }
            }
        }

        /// <summary>
        /// Skips the last element at the end of a sequence
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="enumerable"></param>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.SkipLast(1);
        }
    }
}
