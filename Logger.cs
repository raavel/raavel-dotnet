using System.Diagnostics;

namespace Raavel
{
    /// <summary>
    /// Default logging to use in this Raavel library
    /// </summary>
    internal static class Logger
    {
        /// <summary>
        /// Record an informational message
        /// </summary>
        /// <param name="message">The info message</param>
        public static void Info(string message)
        {
            Debug.WriteLine("[INFO] " + message, "Raavel");
        }

        /// <summary>
        /// Record a warning message
        /// </summary>
        /// <param name="message">The warning message</param>
        public static void Warning(string message)
        {
            Debug.WriteLine("[WARN] " + message, "Raavel");
        }

        /// <summary>
        /// Record an error message
        /// </summary>
        /// <param name="message">The error message</param>
        public static void Error(string message)
        {
            Debug.WriteLine("[ERROR] " + message, "Raavel");
        }
    }
}
