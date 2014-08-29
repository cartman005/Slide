using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Kozlowski.Slide
{
    public class ListItem : INotifyPropertyChanged
    {
        private string filePath;

        public event PropertyChangedEventHandler PropertyChanged;

        public string FilePath
        {
            get
            {
                return filePath;
            }
            set
            {
                if (value != null)
                {
                    filePath = value;
                    NotifyPropertyChanged("File");
                }
            }
        }

        public string Path { get { return filePath; } }

        public string Name { get; set; }

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
