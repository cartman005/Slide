using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

// Solution comes from http://social.msdn.microsoft.com/Forums/windowsapps/en-US/1f75d1ea-66dd-44ca-a06d-5070643967f2/xaml-image-from-path
namespace Kozlowski.Slideshow
{
    /// <summary> 
    /// The converter class used to display images 
    /// </summary> 
    public class FileConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (value != null)
            {
                var file = (StorageFile)value;
                var bitmapImage = new BitmapImage();
#pragma warning disable 4014
                SetSource(bitmapImage, file.Path);
#pragma warning restore 4014
                return bitmapImage;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            throw new NotImplementedException();
        }

        public async Task SetSource(BitmapImage image, string path)
        {
            StorageFile imageFile = null;
            imageFile = await StorageFile.GetFileFromPathAsync(path);
            if (imageFile != null)
            {
                /* Ensures the filestream is disposed of */
                using (IRandomAccessStream fileStream = await imageFile.OpenReadAsync())
                {
                    if (fileStream.CanRead)
                        await image.SetSourceAsync(fileStream);
                }
            }
        }
    } 
}
