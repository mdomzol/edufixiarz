using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EDUFIXiarz.Models;
using EDUFIXiarz.Services;
namespace EDUFIXiarz;

public partial class MainWindow : Window
{

private List<AppDefinition> GetSelectedApps()
    {
        var selectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var alias in AppSelectionAliases)
        {
            if (GetAppCheckBox(alias.Key)?.IsChecked == true)
                selectedIds.Add(alias.Value);
        }

        return ApplicationService.Applications
            .Where(app => selectedIds.Contains(app.Id))
            .ToList();
    }

private CheckBox? GetAppCheckBox(string alias) => alias switch
    {
        "Adobe" => AppsView.AdobeReaderCheck,
        "Everything" => AppsView.EverythingCheck,
        "Chrome" => AppsView.ChromeCheck,
        "Firefox" => AppsView.FirefoxCheck,
        "7zip" => AppsView.SevenZipCheck,
        "VSCode" => AppsView.VscodeCheck,
        "VLC" => AppsView.VlcCheck,
        "Notepad++" => AppsView.NotepadPlusPlusCheck,
        "LibreOffice" => AppsView.LibreOfficeCheck,
        "PuTTY" => AppsView.PuttyCheck,
        "GIMP" => AppsView.GimpCheck,
        _ => null
    };

private void PreviewAppsButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedApps();

        SetupView.AppsPreviewList.Children.Clear();

        if (selected.Count == 0)
        {
            AddEmptyAppsPreviewMessage();
        }
        else
        {
            for (var index = 0; index < selected.Count; index++)
            {
                var item = new Border
                {
                    BorderBrush = FindResource("BorderBrush") as Brush,
                    BorderThickness = new Thickness(0, index == 0 ? 0 : 1, 0, 0),
                    Padding = new Thickness(0, index == 0 ? 0 : 8, 0, 8)
                };

                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var number = new TextBlock
                {
                    Text = $"{index + 1:00}",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = FindResource("AccentBrush") as Brush,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var name = new TextBlock
                {
                    Text = selected[index].Name,
                    FontSize = 12,
                    Foreground = FindResource("TextBrush") as Brush,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(name, 1);
                row.Children.Add(number);
                row.Children.Add(name);
                item.Child = row;
                SetupView.AppsPreviewList.Children.Add(item);
            }
        }

        var isVisible = SetupView.AppsPreviewPanel.Visibility == Visibility.Visible;
        SetupView.AppsPreviewPanel.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
        SetupView.PreviewAppsButton.Content = isVisible
            ? "POKAŻ WYBRANE APLIKACJE  ›"
            : "UKRYJ WYBRANE APLIKACJE  ‹";
    }

private void SetAppSelection(params string[] aliases)
    {
        var selected = aliases.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var alias in AppSelectionAliases.Keys)
        {
            var checkBox = GetAppCheckBox(alias);
            if (checkBox is not null)
                checkBox.IsChecked = selected.Contains(alias);
        }

        SetupView.AppsCheck.IsChecked = aliases.Length > 0;
        UpdateSelectedAppsCount();
    }

private void UpdateSelectedAppsCount()
    {
    }

    private void AddEmptyAppsPreviewMessage()
    {
        SetupView.AppsPreviewList.Children.Add(new TextBlock
        {
            Text = "Brak wybranych aplikacji.",
            FontSize = 11,
            Foreground = FindResource("MutedBrush") as Brush,
            Margin = new Thickness(0, 0, 0, 2)
        });
    }

private void StandardPackageButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppSelection("Adobe", "Everything", "Chrome", "7zip", "VLC", "LibreOffice", "Notepad++");
        Log("Wybrano pakiet: Standardowe stanowisko biurowe.");
    }

private void BasicPackageButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppSelection("Chrome", "7zip", "VLC", "Notepad++");
        Log("Wybrano pakiet: Podstawowy komputer.");
    }

private void DeveloperPackageButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppSelection("Everything", "Chrome", "Firefox", "7zip", "VSCode", "PuTTY", "Notepad++");
        Log("Wybrano pakiet: Programista IT / DEV.");
    }

private void GraphicsPackageButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppSelection("Adobe", "Chrome", "7zip", "VLC", "LibreOffice", "GIMP", "Notepad++");
        Log("Wybrano pakiet: Multimedia i grafika.");
    }

private void SelectAllAppsButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppSelection(AppSelectionAliases.Keys.ToArray());
        Log("Zaznaczono wszystkie aplikacje.");
    }

private void ClearAllAppsButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppSelection();
        SetupView.AppsCheck.IsChecked = false;
        Log("Odznaczono wszystkie aplikacje.");
    }
}
