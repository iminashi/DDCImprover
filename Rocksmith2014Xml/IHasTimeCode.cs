namespace Rocksmith2014Xml
{
    /// <summary>
    /// Interface for classes that contain a time code in milliseconds.
    /// </summary>
    public interface IHasTimeCode
    {
        /// <summary>
        /// Time in milliseconds.
        /// </summary>
        int Time { get; }
    }
}
