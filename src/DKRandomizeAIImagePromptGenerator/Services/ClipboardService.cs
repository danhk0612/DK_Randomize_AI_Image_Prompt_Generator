using Windows.ApplicationModel.DataTransfer;

namespace DKRandomizeAIImagePromptGenerator.Services;

public sealed class ClipboardService
{
    public void CopyText(string text)
    {
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
        Clipboard.Flush();
    }
}
