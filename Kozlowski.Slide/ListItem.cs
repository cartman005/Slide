using System.ComponentModel;

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
                    NotifyPropertyChanged("FilePath");
                }
            }
        }
        
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
