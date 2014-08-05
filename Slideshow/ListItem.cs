using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Kozlowski.Slideshow
{
    public class ListItem
    {
        public StorageFile File { get; set; }

        public string Path { get { return File.Path; } }

        public string Name { get { return File.DisplayName; } }

        public override string ToString()
        {
            return Name;
        }
    }
}
