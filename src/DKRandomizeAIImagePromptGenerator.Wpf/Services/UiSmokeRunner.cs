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

            app.Settings.Current.WindowWidth = 1110;
            app.Settings.Current.WindowHeight = 710;

            window = new MainWindow();
            if (Math.Abs(window.Width - 1110) > 0.5 ||
                Math.Abs(window.Height - 710) > 0.5)
            {
                return false;
            }

            window.Width = 1280;
            window.Height = 820;
            window.WindowStyle = WindowStyle.None;
            window.ShowInTaskbar = false;
            window.Opacity = 0.01;
            window.Left = -10000;
            window.Top = -10000;
            /* window configured for hidden smoke execution */
            /*
                Width = 1280,
                Height = 820,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                Opacity = 0.01,
                Left = -10000,
                Top = -10000
            };
            */

            app.RegisterMainWindow(window);
            window.Show();
            await DrainUiAsync(window.Dispatcher);

            if (window.PageHost.Content is not MixerView mixer)
            {
                return false;
            }

            await DrainUiAsync(window.Dispatcher);
            if (mixer.ViewModel.CharacterItems.Count < 3 ||
                mixer.ViewModel.ArtistItems.Count == 0 ||
                mixer.ViewModel.AdditionalItems.Count == 0 ||
                mixer.CharacterSelectedList.Items.Count == 0 ||
                mixer.ArtistSelectedList.Items.Count == 0 ||
                mixer.AdditionalSelectedList.Items.Count == 0)
            {
                return false;
            }

            if (mixer.CharacterRandomRadio.IsChecked != true ||
                mixer.CharacterDirectControls.IsEnabled ||
                !mixer.CharacterRandomControls.IsEnabled)
            {
                return false;
            }

            mixer.CharacterRandomCountCombo.SelectedItem = 2;
            await DrainUiAsync(window.Dispatcher);
            if (mixer.ViewModel.CharacterRandomCount != 2 ||
                mixer.CharacterSelectedList.Items.Count != 2)
            {
                return false;
            }

            mixer.CharacterDirectRadio.IsChecked = true;
            await DrainUiAsync(window.Dispatcher);
            if (mixer.ViewModel.CharacterMode != PromptSelectionMode.Fixed ||
                !mixer.CharacterDirectControls.IsEnabled ||
                mixer.CharacterRandomControls.IsEnabled)
            {
                return false;
            }

            var pickerSmoke = new PromptPickerDialog(
                PromptCategory.Character,
                mixer.ViewModel.GetAvailableItems(PromptCategory.Character),
                mixer.ViewModel.GetSelectedItems(PromptCategory.Character));
            pickerSmoke.SearchBox.Text = "second";
            if (pickerSmoke.SearchResultsList.Items.Count != 1)
            {
                pickerSmoke.Close();
                return false;
            }
            pickerSmoke.Close();

            mixer.PositiveOutput.Text = "manual session text";
            window.SettingsNavButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await DrainUiAsync(window.Dispatcher);
            if (window.PageHost.Content is not SettingsView)
            {
                return false;
            }

            window.MixerNavButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await DrainUiAsync(window.Dispatcher);
            if (!ReferenceEquals(window.PageHost.Content, mixer) ||
                mixer.PositiveOutput.Text != "manual session text")
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

            var missingCharacter = mixer.ViewModel.CharacterItems
                .FirstOrDefault(item =>
                    mixer.ViewModel.SelectedCharacters.All(selected => selected.Id != item.Id));
            if (missingCharacter is null)
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

            var libraryItem = library.ViewModel.Items
                .FirstOrDefault(item => item.Id == missingCharacter.Id);
            if (libraryItem is null)
            {
                return false;
            }

            library.GalleryListBox.SelectedItem = libraryItem;
            await DrainUiAsync(window.Dispatcher);
            if (library.AddToMixerButton.Visibility != Visibility.Visible)
            {
                return false;
            }

            library.AddToMixerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await DrainUiAsync(window.Dispatcher);
            if (mixer.ViewModel.CharacterMode != PromptSelectionMode.Fixed ||
                mixer.ViewModel.SelectedCharacters.Count != 3)
            {
                return false;
            }

            library.AddToMixerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            if (mixer.ViewModel.SelectedCharacters.Count != 3)
            {
                return false;
            }

            window.HistoryNavButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await DrainUiAsync(window.Dispatcher);
            if (window.PageHost.Content is not HistoryView history ||
                history.HistoryListBox.Items.Count != 20 ||
                history.ViewModel.TotalCount != 21 ||
                history.ViewModel.TotalPages != 2 ||
                !history.NextPageButton.IsEnabled)
            {
                return false;
            }

            history.HistoryListBox.SelectedIndex = 0;
            await DrainUiAsync(window.Dispatcher);
            if (history.DetailPane.Visibility != Visibility.Visible ||
                history.PreviewItems.Count != 3)
            {
                return false;
            }

            history.NextPageButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await DrainUiAsync(window.Dispatcher);
            if (history.ViewModel.CurrentPage != 2 ||
                history.HistoryListBox.Items.Count != 1 ||
                !history.PreviousPageButton.IsEnabled ||
                history.NextPageButton.IsEnabled)
            {
                return false;
            }

            history.PreviousPageButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await DrainUiAsync(window.Dispatcher);
            if (history.ViewModel.CurrentPage != 1 ||
                history.HistoryListBox.Items.Count != 20)
            {
                return false;
            }

            window.SettingsNavButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await DrainUiAsync(window.Dispatcher);
            if (window.PageHost.Content is not SettingsView settings ||
                settings.ThemeComboBox.Items.Count != 3 ||
                settings.StorageLocationComboBox.Items.Count != 2)
            {
                return false;
            }

            window.MixerNavButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await DrainUiAsync(window.Dispatcher);
            return ReferenceEquals(window.PageHost.Content, mixer) &&
                   mixer.ViewModel.SelectedCharacters.Count == 3;
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

        var characterSecond = new PromptItem
        {
            Category = PromptCategory.Character,
            Title = "UI Smoke Character Second",
            PositivePrompt = "character second positive",
            NegativePrompt = "character second negative",
            Memo = "second character memo"
        };
        characterSecond.Tags.Add("second");

        var characterThird = new PromptItem
        {
            Category = PromptCategory.Character,
            Title = "UI Smoke Character Third",
            PositivePrompt = "character third positive",
            NegativePrompt = "character third negative",
            Memo = "third character memo"
        };
        characterThird.Tags.Add("third");

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
        await app.Prompts.CreateAsync(characterSecond);
        await app.Prompts.CreateAsync(characterThird);
        await app.Prompts.CreateAsync(artist);
        await app.Prompts.CreateAsync(additional);

        var baseTime = DateTimeOffset.UtcNow.AddHours(-1);
        for (var index = 0; index < 20; index++)
        {
            await app.History.SaveAsync(new CombinationHistory
            {
                PositiveText = $"paged positive {index}",
                NegativeText = $"paged negative {index}",
                CreatedAt = baseTime.AddMinutes(index)
            });
        }

        var history = new CombinationHistory
        {
            CharacterPromptId = character.Id,
            CharacterTitleSnapshot = character.Title,
            ArtistPromptId = artist.Id,
            ArtistTitleSnapshot = artist.Title,
            CharacterMode = PromptSelectionMode.Fixed,
            ArtistMode = PromptSelectionMode.Random,
            AdditionalMode = PromptSelectionMode.Fixed,
            ArtistRandomCount = 1,
            PositiveText = "smoke positive",
            NegativeText = "smoke negative",
            CreatedAt = DateTimeOffset.UtcNow
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
