using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ICSharpCode.AvalonEdit.Highlighting;

namespace RblxExecutorUI;

public partial class MainWindow
{
    private bool _appearanceReady;
    private string T(string key) => UiCatalog.Get(key, _preferences.Language);
    private void InitializeAppearance()
    {
        LanguageSelector.ItemsSource = UiCatalog.Languages;
        UiFontSelector.ItemsSource = UiCatalog.UiFonts;
        EditorFontSelector.ItemsSource = UiCatalog.CodeFonts;
        ThemeSelector.SelectedIndex = _preferences.Theme == "Light" ? 1 : 0;
        PresetSelector.SelectedIndex = _preferences.WindowPreset switch { "MacOS" => 1, "Compact" => 2, _ => 0 };
        LanguageSelector.SelectedValue = _preferences.Language;
        UiFontSelector.SelectedItem = _preferences.UiFont;
        EditorFontSelector.SelectedItem = _preferences.EditorFont;
        ApplyAppearance();
        _appearanceReady = true;
    }
    private void Appearance_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_appearanceReady) return;
        _preferences.Theme = ThemeSelector.SelectedIndex == 1 ? "Light" : "Dark";
        _preferences.WindowPreset = PresetSelector.SelectedIndex switch { 1 => "MacOS", 2 => "Compact", _ => "Classic" };
        _preferences.Language = LanguageSelector.SelectedValue as string ?? "pt-BR";
        _preferences.UiFont = UiFontSelector.SelectedItem as string ?? "Segoe UI";
        _preferences.EditorFont = EditorFontSelector.SelectedItem as string ?? "Consolas";
        ApplyAppearance();
        SavePreferences();
    }
    private void ApplyAppearance()
    {
        Resources.MergedDictionaries[1] = new ResourceDictionary {
            Source = new Uri($"Themes/{_preferences.Theme}.xaml", UriKind.Relative) };
        foreach (var key in UiCatalog.Strings.Keys) Resources["Text." + key] = T(key);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(_preferences.Language);
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(_preferences.Language);
        FontFamily = new FontFamily(_preferences.UiFont + ", Segoe UI");
        Editor.FontFamily = new FontFamily(_preferences.EditorFont + ", Consolas");
        bool mac = _preferences.WindowPreset == "MacOS";
        bool compact = _preferences.WindowPreset == "Compact";
        StandardWindowControls.Visibility = mac ? Visibility.Collapsed : Visibility.Visible;
        MacWindowControls.Visibility = mac ? Visibility.Visible : Visibility.Collapsed;
        HeaderBrand.HorizontalAlignment = mac ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        HeaderRow.Height = new GridLength(compact ? 36 : 48);
        SidebarColumn.Width = new GridLength(compact ? 56 : 70);
        WindowFrame.CornerRadius = new CornerRadius(mac ? 24 : compact ? 12 : 20);
        OuterFrame.CornerRadius = WindowFrame.CornerRadius;
        UpdateContentClip();
        var selectedLogo = new BitmapImage(new Uri("pack://application:,,,/Assets/Brand/cesi-selected.png"));
        SettingsBrand.Source = selectedLogo;
        HeaderLogo.Source = selectedLogo;
        // Keep syntax readable while honoring the monochrome palette.
        var foreground = (SolidColorBrush)FindResource("SilverLight");
        var secondary = (SolidColorBrush)FindResource("SilverText");
        string syntaxPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lua.xshd");
        using var syntaxStream = System.IO.File.Exists(syntaxPath)
            ? System.IO.File.OpenRead(syntaxPath)
            : typeof(MainWindow).Assembly.GetManifestResourceStream("cesi.Lua.xshd");
        if (syntaxStream != null)
        {
            var xml = System.Xml.Linq.XDocument.Load(syntaxStream);
            foreach (var node in xml.Descendants().Where(node => node.Name.LocalName == "Color"))
                node.SetAttributeValue("foreground", (string?)node.Attribute("name") == "Comment"
                    ? secondary.Color.ToString() : foreground.Color.ToString());
            using var reader = xml.CreateReader();
            Editor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(
                reader, HighlightingManager.Instance);
        }
        Editor.TextArea.TextView.Redraw();
        Editor.TextArea.SelectionBrush = (Brush)FindResource("CardHover");
        Editor.TextArea.SelectionForeground = foreground;
        Editor.TextArea.SelectionBorder = new Pen(foreground, 1);
        Editor.TextArea.Caret.CaretBrush = foreground;
        RefreshLibrarySummary();
        // Messages already displayed must not retain the previous language.
        PreferencesStatus.SetResourceReference(TextBlock.TextProperty, "Text.Saved");
        if (IsStreamMode) NotificationText.Text = T("PrivateStatus");
    }
    private void UpdateContentClip()
    {
        double radius = Math.Max(0, WindowFrame.CornerRadius.TopLeft - WindowFrame.BorderThickness.Left);
        WindowContentRoot.Clip = new RectangleGeometry(new Rect(0, 0,
            WindowContentRoot.ActualWidth, WindowContentRoot.ActualHeight), radius, radius);
    }
    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        MaxHeight = SystemParameters.WorkArea.Height;
        MaxWidth = SystemParameters.WorkArea.Width;
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
}

public partial class MainWindow
{
    private string LocalizeNotification(string message)
    {
        string key = message switch
        {
            "No script to execute" => "NoScript",
            "Executing Script..." or "Attaching to Roblox..." => "Working",
            "Executed Successfully!" => "Executed",
            "Editor Cleared" => "Cleared",
            "Script Saved Successfully" => "FileSaved",
            "File opened" => "FileOpened",
            "Code Copied!" => "Copied",
            "Script Deleted!" => "Deleted",
            "Roblox not found!" or "No Roblox to Kill" => "NotFound",
            "Roblox Terminated" => "Ended",
            "Successfully attached to game!" => "Connected",
            "Execution Exception" or "Failed to save file" or "Failed to open file" or
            "Hub script error!" or "Copy Failed!" or "Delete Failed!" or
            "Connect failed!" or "Attach Exception" => "OperationError",
            _ => ""
        };
        if (key.Length > 0) return T(key);
        if (message.StartsWith("Error: ", StringComparison.Ordinal)) return T("Error") + ": " + message[7..];
        if (message.StartsWith("Executing ", StringComparison.Ordinal)) return T("Run") + " · " + message[10..];
        if (message.StartsWith("Connecting to PID ", StringComparison.Ordinal)) return T("Working") + " · PID " + message[18..];
        return message;
    }
}
