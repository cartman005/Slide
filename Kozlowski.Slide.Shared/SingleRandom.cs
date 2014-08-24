using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
