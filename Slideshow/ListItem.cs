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
        public BitmapImage Image { get; set; }

        public StorageFile File { get; set; }

        public string GetName()
        {
            return File.DisplayName;
        }

        public override string ToString()
        {
            return GetName();
        }
    }
}
