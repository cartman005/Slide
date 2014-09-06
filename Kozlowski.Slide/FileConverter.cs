using Kozlowski.Slide.Background;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

// This class was adapted from a post by StormOli on 02/01/2013 to an MSDN forum here:
// http://social.msdn.microsoft.com/Forums/windowsapps/en-US/1f75d1ea-66dd-44ca-a06d-5070643967f2/xaml-image-from-path
namespace Kozlowski.Slide
{
    /// <summary> 
    /// The converter class used to display images by opening the stream and resizing them.
    /// </summary> 
    public class FileConverter : IValueConverter
    {
        /// <summary>
        /// Converts an image path to a BitmapImage of the size of the app window.
        /// </summary>
        /// <param name="value">TThe file to be opened.</param>
        /// <param name="targetType">Unused parameter.</param>
        /// <param name="parameter">Unused parameter.</param>
        /// <param name="culture">Unused parameter.</param>
        /// <returns>The BitmapImage created from the file path.</returns>
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            Debug.WriteLine("Convert");
            if (value != null)
            {
                var filePath = (string)value;
                var bitmapImage = new BitmapImage();
#pragma warning disable 4014
                SetSource(bitmapImage, filePath);
#pragma warning restore 4014
                
                return bitmapImage;
            }
            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// Converts the image back to the filepath that it was created from.
        /// This method is not currently implemented.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Opens the file stream and resizes the image.
        /// </summary>
        /// <param name="image">The image that will use the filestream as its source.</param>
        /// <param name="path">The path to the image file whose filestream will be used for the image.</param>
        /// <returns>A Task representing the operation.</returns>
        public async Task SetSource(BitmapImage image, string path)
        {
            BitmapDecoder decoder;
            var bounds = Window.Current.Bounds;
            
            StorageFile imageFile = null;
            imageFile = await StorageFile.GetFileFromPathAsync(path);
            if (imageFile != null)
            {
                // Ensures the filestream is disposed of
                using (IRandomAccessStream fileStream = await imageFile.OpenReadAsync())
                {
                    if (fileStream.CanRead)
                    {
                        decoder = await BitmapDecoder.CreateAsync(fileStream);

                        // Set the DecodePixelWidth to save memory
                        // Only set one value (height or width) to preserve the proportion
                        var dimensions = TileMaker.GetDimensions((int)decoder.PixelWidth, (int)decoder.PixelHeight, (int)bounds.Width, (int)bounds.Height);
                        image.DecodePixelWidth = (int)dimensions.Width;
                        //image.DecodePixelHeight = (int)dimensions.Height;
                        await image.SetSourceAsync(fileStream);
                    }
                }
            }
        }
    } 
}
