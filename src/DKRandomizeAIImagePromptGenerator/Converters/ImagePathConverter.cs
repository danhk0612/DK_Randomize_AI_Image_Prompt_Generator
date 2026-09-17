using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DKRandomizeAIImagePromptGenerator.Converters;

public sealed class ImagePathConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string relativePath || string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var app = (App)Application.Current;
        var fullPath = app.Images.ResolvePath(relativePath);
        return new BitmapImage(new Uri(fullPath));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
