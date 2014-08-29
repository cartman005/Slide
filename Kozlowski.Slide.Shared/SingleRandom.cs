using System;

namespace Kozlowski.Slide.Shared
{
    public class SingleRandom : Random
    {
        private static SingleRandom instance;

        public static SingleRandom Instance
        {
            get
            {
                if (instance == null)
                    instance = new SingleRandom();

                return instance;
            }
        }

        private SingleRandom() { }
    }
}
