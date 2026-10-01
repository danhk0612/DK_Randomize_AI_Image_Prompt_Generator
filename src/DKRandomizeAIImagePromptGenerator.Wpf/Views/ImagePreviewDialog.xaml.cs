using System.Windows;
using System.Windows.Media.Imaging;
using DKRandomizeAIImagePromptGenerator.Models;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Views;

public partial class ImagePreviewDialog : Window
{
    public ImagePreviewDialog(PromptItem item)
    {
        InitializeComponent();

        TitleText.Text = item.Title;
        Title = $"{item.Title} - 이미지 크게 보기";

        if (string.IsNullOrWhiteSpace(item.ImagePath))
        {
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var app = (App)Application.Current;
            var fullPath = app.Images.ResolvePath(item.ImagePath);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            PreviewImage.Source = bitmap;
        }
        catch
        {
            EmptyText.Visibility = Visibility.Visible;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) =>
        Close();
}