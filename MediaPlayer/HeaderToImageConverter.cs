using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MediaPlayer
{
    #region HeaderToImageConverter

    [ValueConversion(typeof(string), typeof(bool))]
    public class HeaderToImageConverter : IValueConverter
    {
        public static HeaderToImageConverter Instance = new HeaderToImageConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if ((value as string).Contains(@"\"))
                {
                    Uri uri = new Uri("pack://application:,,,/Image/diskdrive.png");
                    BitmapImage source = new BitmapImage(uri);
                    return source;
                }
                else if ((value as string).Contains(".mp3") || (value as string).Contains(".wav") ||
                        (value as string).Contains(".wmv") || (value as string).Contains(".avi") ||
                        (value as string).Contains(".mp4") || (value as string).Contains(".jpg") ||
                        (value as string).Contains(".jpeg") || (value as string).Contains(".png") ||
                        (value as string).Contains(".bmp"))
                {
                    Uri uri = new Uri("pack://application:,,,/Image/file.png");
                    BitmapImage source = new BitmapImage(uri);
                    return source;
                }
                else
                {
                    Uri uri = new Uri("pack://application:,,,/Image/folder.png");
                    BitmapImage source = new BitmapImage(uri);
                    return source;
                }
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert back");
        }
    }

    #endregion // DoubleToIntegerConverter


}