using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Converters;

public sealed class ImagePathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string relativePath || string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        try
        {
            var app = (App)System.Windows.Application.Current;
            var fullPath = app.Images.ResolvePath(relativePath);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
