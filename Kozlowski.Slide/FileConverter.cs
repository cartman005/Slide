using Kozlowski.Slide.Background;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

// Solution comes from http://social.msdn.microsoft.com/Forums/windowsapps/en-US/1f75d1ea-66dd-44ca-a06d-5070643967f2/xaml-image-from-path
namespace Kozlowski.Slide
{
    /// <summary> 
    /// The converter class used to display images 
    /// </summary> 
    public class FileConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            Debug.WriteLine("Convert");
            if (value != null)
            {
                var filePath = (string)value;
                var bitmapImage = new BitmapImage();
                //Debug.WriteLine(file.DisplayName);
#pragma warning disable 4014
                SetSource(bitmapImage, filePath);
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
            BitmapDecoder decoder;
            Rect bounds = Window.Current.Bounds;
            Debug.WriteLine("Size " + bounds.Width + " x " + bounds.Height);
            
            StorageFile imageFile = null;
            imageFile = await StorageFile.GetFileFromPathAsync(path);
            if (imageFile != null)
            {
                /* Ensures the filestream is disposed of */
                using (IRandomAccessStream fileStream = await imageFile.OpenReadAsync())
                {
                    if (fileStream.CanRead)
                    {
                        decoder = await BitmapDecoder.CreateAsync(fileStream);

                        Size dimensions = TileMaker.GetDimensions((int)decoder.PixelWidth, (int)decoder.PixelHeight, (int)bounds.Width, (int)bounds.Height);
                        Debug.WriteLine("Set image to " + dimensions.Width + " x " + dimensions.Height);
                        image.DecodePixelWidth = (int)dimensions.Width;
                        image.DecodePixelHeight = (int)dimensions.Height;
                        await image.SetSourceAsync(fileStream);
                        Debug.WriteLine("Done");
                    }
                }
            }
        }
    } 
}
