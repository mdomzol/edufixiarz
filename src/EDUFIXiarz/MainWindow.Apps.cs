using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EDUFIXiarz.Models;
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
        "Adobe" => AdobeReaderCheck,
        "Everything" => EverythingCheck,
        "Chrome" => ChromeCheck,
        "Firefox" => FirefoxCheck,
        "7zip" => SevenZipCheck,
        "VSCode" => VscodeCheck,
        "VLC" => VlcCheck,
        "Notepad++" => NotepadPlusPlusCheck,
        "LibreOffice" => LibreOfficeCheck,
        "PuTTY" => PuttyCheck,
        "GIMP" => GimpCheck,
        _ => null
    };

private void PreviewAppsButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedApps();

        SelectedAppsCountText.Text = selected.Count.ToString();
        AppsPreviewList.Children.Clear();

        if (selected.Count == 0)
        {
            AppsPreviewSummaryText.Text = "Brak aplikacji wskazanych do instalacji.";
        }
        else
        {
            var summary = selected.Count == 1 ? "aplikacja wskazana" : "aplikacje wskazane";
            AppsPreviewSummaryText.Text = $"{selected.Count} {summary} do instalacji.";

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
                AppsPreviewList.Children.Add(item);
            }
        }

        var isVisible = AppsPreviewPanel.Visibility == Visibility.Visible;
        AppsPreviewPanel.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
        PreviewAppsButton.Content = isVisible
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

        AppsCheck.IsChecked = aliases.Length > 0;
        UpdateSelectedAppsCount();
    }

private void UpdateSelectedAppsCount()
    {
        SelectedAppsCountText.Text = GetSelectedApps().Count.ToString();
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
        AppsCheck.IsChecked = false;
        Log("Odznaczono wszystkie aplikacje.");
    }

private void ResetSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        HostnameCheck.IsChecked = false;
        _joinDomainRequested = false;
        DomainCredentialsExpander.IsExpanded = false;
        DomainCredentialsExpander.Visibility = Visibility.Collapsed;
        DomainCheck.Content = "DOŁĄCZ DO DOMENY AD";
        DomainCheck.Background = FindResource("InputBrush") as Brush;
        DomainCheck.BorderBrush = FindResource("BorderBrush") as Brush;
        DomainPasswordBox.Clear();
        BloatwareCheck.IsChecked = false;
        OfficeCheck.IsChecked = false;
        ClearAllAppsButton_Click(sender, e);
        Log("Wybór zadań i aplikacji został wyzerowany.");
    }

    private static string Escape(string value) => value.Replace("'", "''");
}

}
