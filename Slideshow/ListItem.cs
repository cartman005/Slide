using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Kozlowski.Slideshow
{
    public class ListItem : INotifyPropertyChanged
    {
        private StorageFile file;

        public event PropertyChangedEventHandler PropertyChanged;

        public StorageFile File {
            get
            {
                return file;
            }
            set
            {
                if (value != null)
                {
                    file = value;
                    NotifyPropertyChanged("File");
                }
            }
        }

        public string Path { get { return File.Path; } }

        public string Name { get { return File.DisplayName; } }

        public override string ToString()
        {
            return Name;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }
    }
}
