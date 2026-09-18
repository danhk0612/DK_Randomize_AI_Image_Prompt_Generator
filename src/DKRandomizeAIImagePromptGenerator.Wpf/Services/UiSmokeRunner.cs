using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Wpf.Views;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Services;

public static class UiSmokeRunner
{
    public static async Task<bool> RunAsync(App app)
    {
        var root = Path.Combine(Path.GetTempPath(), $"dk-prompt-wpf-ui-smoke-{Guid.NewGuid():N}");
        MainWindow? window = null;

        try
        {
            var paths = AppDataPaths.Create(root);
            await app.InitializeServicesAsync(paths);
            await SeedAsync(app);

            window = new MainWindow
            {
                Width = 1280,
                Height = 820,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                Opacity = 0.01,
                Left = -10000,
                Top = -10000
            };

            app.MainWindow = window;
            window.Show();
            await DrainUiAsync(window.Dispatcher);

            if (window.PageHost.Content is not MixerView mixer)
            {
                return false;
            }

            await DrainUiAsync(window.Dispatcher);
            if (mixer.CharacterPromptCombo.Items.Count == 0 ||
                mixer.ArtistPromptCombo.Items.Count == 0 ||
                mixer.AdditionalPromptCombo.Items.Count == 0)
            {
                return false;
            }

            if (!VerifyNavigationAccessibility(window))
            {
                return false;
            }

            if (!VerifyKeyboardTraversal(window))
            {
                return false;
            }

            window.Width = 850;
            await DrainUiAsync(window.Dispatcher);
            if (window.NavigationColumn.Width.Value != 64 ||
                window.BrandPanel.Visibility != Visibility.Collapsed)
            {
                return false;
            }

            window.Width = 1280;
            await DrainUiAsync(window.Dispatcher);
            if (window.NavigationColumn.Width.Value != 220 ||
                window.BrandPanel.Visibility != Visibility.Visible)
            {
                return false;
            }

            window.LibraryNavButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await DrainUiAsync(window.Dispatcher);
            if (window.PageHost.Content is not PromptLibraryView library ||
                library.GalleryListBox.Items.Count == 0)
            {
                return false;
            }

            window.HistoryNavButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await DrainUiAsync(window.Dispatcher);
            if (window.PageHost.Content is not HistoryView history ||
                history.HistoryListBox.Items.Count == 0)
            {
                return false;
            }

            window.SettingsNavButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await DrainUiAsync(window.Dispatcher);
            if (window.PageHost.Content is not SettingsView settings ||
                settings.ThemeComboBox.Items.Count != 3)
            {
                return false;
            }

            window.MixerNavButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await DrainUiAsync(window.Dispatcher);
            return window.PageHost.Content is MixerView;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (window is not null)
            {
                window.Hide();
                window.Content = null;
            }

            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
            catch
            {
                // The smoke result should reflect UI behavior, not best-effort temp cleanup.
            }
        }
    }

    private static async Task SeedAsync(App app)
    {
        var character = new PromptItem
        {
            Category = PromptCategory.Character,
            Title = "UI Smoke Character",
            PositivePrompt = "character positive",
            NegativePrompt = "character negative",
            Memo = "character memo"
        };
        character.Tags.Add("smoke");

        var artist = new PromptItem
        {
            Category = PromptCategory.Artist,
            Title = "UI Smoke Artist",
            PositivePrompt = "artist positive",
            NegativePrompt = "artist negative",
            Memo = "artist memo"
        };
        artist.Tags.Add("style");

        var additional = new PromptItem
        {
            Category = PromptCategory.Additional,
            Title = "UI Smoke Additional",
            PositivePrompt = "additional positive",
            NegativePrompt = "additional negative",
            Memo = "additional memo"
        };
        additional.Tags.Add("extra");

        await app.Prompts.CreateAsync(character);
        await app.Prompts.CreateAsync(artist);
        await app.Prompts.CreateAsync(additional);

        var history = new CombinationHistory
        {
            CharacterPromptId = character.Id,
            CharacterTitleSnapshot = character.Title,
            ArtistPromptId = artist.Id,
            ArtistTitleSnapshot = artist.Title,
            PositiveText = "smoke positive",
            NegativeText = "smoke negative"
        };
        history.AdditionalItems.Add(new CombinationHistoryAdditional(additional.Id, additional.Title));
        await app.History.SaveAsync(history);
    }

    private static bool VerifyNavigationAccessibility(MainWindow window)
    {
        var buttons = new[]
        {
            window.MixerNavButton,
            window.LibraryNavButton,
            window.HistoryNavButton,
            window.SettingsNavButton
        };

        return buttons.All(button =>
            button.Focusable &&
            !string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)) &&
            button.ToolTip is not null);
    }

    private static bool VerifyKeyboardTraversal(MainWindow window)
    {
        window.Activate();
        if (!window.MixerNavButton.Focus())
        {
            return false;
        }

        var before = Keyboard.FocusedElement;
        var moved = window.MixerNavButton.MoveFocus(
            new TraversalRequest(FocusNavigationDirection.Next));
        var after = Keyboard.FocusedElement;

        return moved && before is not null && after is not null && !ReferenceEquals(before, after);
    }

    private static async Task DrainUiAsync(Dispatcher dispatcher)
    {
        await dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Task.Delay(40);
        await dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
    }
}
