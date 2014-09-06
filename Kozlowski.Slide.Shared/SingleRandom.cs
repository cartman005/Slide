using System;

namespace Kozlowski.Slide.Shared
{
    /// <summary>
    /// A singleton Random class.
    /// </summary>
    public class SingleRandom : Random
    {
        /// <summary>
        /// The instance of the Random class, initialized lazily so that it is only created if/when needed.
        /// </summary>
        private static readonly Lazy<SingleRandom> instance = new Lazy<SingleRandom>(() => new SingleRandom());

        /// <summary>
        /// Get the app's instance of the Random class.
        /// </summary>
        public static SingleRandom Instance { get { return instance.Value; } }

        /// <summary>
        /// Unused class constructor which is private so that the class can only be instantiated by the Instance method.
        /// </summary>
        private SingleRandom() { }
    }
}
