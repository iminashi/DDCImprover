using Rocksmith2014Xml;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace DDCImprover.Core
{
    public static class Extensions
    {
        /// <summary>
        /// Converts a time in seconds into a string according to the user preferences.
        /// </summary>
        /// <param name="time">The time to convert.</param>
        /// <returns>The time as a string in seconds and milliseconds, or minutes, seconds and milliseconds.</returns>
        public static string TimeToString(this float time)
        {
            if (XMLProcessor.Preferences.DisplayTimesInSeconds)
            {
                return time.ToString("F3", NumberFormatInfo.InvariantInfo);
            }
            else
            {
                int minutes = (int)time / 60;
                float secMs = time - (minutes * 60);
                return $"{minutes.ToString("D2", NumberFormatInfo.InvariantInfo)}:{secMs.ToString("00.000", NumberFormatInfo.InvariantInfo)}";
            }
        }

        /// <summary>
        /// Returns the index of the element at the time to find in a list ordered by time.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="elements"></param>
        /// <param name="timeToFind">The time for the element to find.</param>
        /// <returns>Index of the element, -1 if not found.</returns>
        public static int FindIndexByTime<T>(this IList<T> elements, float timeToFind)
            where T : IHasTimeCode
        {
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (Utils.TimeEqualToMilliseconds(element.Time, timeToFind))
                    return i;
                else if (element.Time > timeToFind)
                    return -1;
            }

            return -1;
        }

        /// <summary>
        /// Finds the first element that has the given time from a list ordered by time.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="elements"></param>
        /// <param name="timeToFind">The time for the element to find.</param>
        /// <returns>The found element or null if not found.</returns>
        public static T? FindByTime<T>(this IList<T> elements, float timeToFind)
            where T : class, IHasTimeCode
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

        /// <summary>
        /// Inserts an element into a list ordered by time.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="elements"></param>
        /// <param name="element">The element to insert.</param>
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
        /// <typeparam name="T">The type of the elements.</typeparam>
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
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="enumerable"></param>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> enumerable)
            => enumerable.SkipLast(1);

        /// <summary>
        /// Starts a string as a file name for a shell-executed process.
        /// </summary>
        /// <param name="fileName">The file name for the process.</param>
        public static void StartAsProcess(this string fileName)
            => Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
    }
}
