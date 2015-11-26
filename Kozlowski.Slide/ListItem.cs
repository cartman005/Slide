using System.ComponentModel;

namespace Kozlowski.Slide
{
    /// <summary>
    /// This class is used as an item to bind to the FlipView. Each instance represents an image to be displayed during the slideshow.
    /// </summary>
    public class ListItem : INotifyPropertyChanged
    {
        private string _filePath;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the path to the image file.
        /// </summary>
        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                if (value != null)
                {
                    _filePath = value;
                    NotifyPropertyChanged("FilePath");
                }
            }
        }

        /// <summary>
        /// Gets or sets the display name of the image.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Returns the display name of the image.
        /// </summary>
        /// <returns>The display name of the image.</returns>
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
