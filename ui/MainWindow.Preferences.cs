using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace RblxExecutorUI;

public partial class MainWindow
{
    public static readonly DependencyProperty ExecutionAvailableProperty = DependencyProperty.Register(
        nameof(ExecutionAvailable), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
    public bool ExecutionAvailable { get => (bool)GetValue(ExecutionAvailableProperty); private set => SetValue(ExecutionAvailableProperty, value); }

    public static readonly DependencyProperty IsStreamModeProperty = DependencyProperty.Register(
        nameof(IsStreamMode), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
    public bool IsStreamMode
    {
        get => (bool)GetValue(IsStreamModeProperty);
        private set => SetValue(IsStreamModeProperty, value);
    }

    private readonly UserPreferences _preferences = UserPreferences.Load();
    private readonly DispatcherTimer _autoInjectTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private DispatcherTimer? _processMonitor;
    private DispatcherTimer? _clientPoller;
    private bool _preferencesReady, _coreReady, _isAttaching, _closed;
    private uint _connectedPid;
    private readonly AutoInjectSchedule _autoSchedule = new();

    private void InitializePreferences()
    {
        AutoInjectToggle.IsChecked = _preferences.AutoInject;
        StreamModeToggle.IsChecked = _preferences.StreamMode;
        InitializeAppearance();
        ApplyStreamMode();
        _preferencesReady = true;
        _autoInjectTimer.Tick += AutoInject_Tick;
        _autoInjectTimer.Start();
        Closed += (_, _) =>
        {
            _closed = true;
            _autoInjectTimer.Stop();
            _processMonitor?.Stop();
            StopClientProfile();
            _profileHttp.Dispose();
            _notificationTimer.Stop();
            if (_coreReady && !_isAttaching) RblxCore.Disconnect();
        };
    }

    private void Preferences_Changed(object sender, RoutedEventArgs e)
    {
        if (!_preferencesReady) return;
        _preferences.AutoInject = AutoInjectToggle.IsChecked == true;
        _preferences.StreamMode = StreamModeToggle.IsChecked == true;
        _autoSchedule.Reset();
        ApplyStreamMode();
        SavePreferences();
    }

    private void SavePreferences()
    {
        try
        {
            _preferences.Save();
            PreferencesStatus.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, "Text.Saved");
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
        {
            PreferencesStatus.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, "Text.SaveError");
        }
    }

    private void ApplyStreamMode()
    {
        IsStreamMode = StreamModeToggle.IsChecked == true;
        Title = "cesi";
        foreach (var element in new System.Windows.UIElement[] {
            ClientAccountName, ClientPidText, ClientAvatarImage, ConnectedAccountChip, Editor, ScriptHubList, ScriptSearch })
        {
            element.Effect = IsStreamMode ? new System.Windows.Media.Effects.BlurEffect { Radius = 18 } : null;
        }
        StreamModeBadge.Visibility = IsStreamMode ? Visibility.Visible : Visibility.Collapsed;
        // Clear old toast content and hide the external debug console before the next frame.
        NotificationText.Text = IsStreamMode ? T("PrivateStatus") : "cesi";
        DebugConsoleToggle.IsEnabled = !IsStreamMode;
        if (IsStreamMode)
        {
            DebugConsoleToggle.IsChecked = false;
            var handle = GetConsoleWindow();
            if (handle != IntPtr.Zero) ShowWindow(handle, SW_HIDE);
        }
    }

    private async void AutoInject_Tick(object? sender, EventArgs e)
    {
        if (_closed || !_coreReady || _isAttaching || !_preferences.AutoInject) return;
        // Managed discovery avoids overlapping native calls while a connection is pending.
        uint pid = 0;
        var processes = Process.GetProcessesByName("RobloxPlayerBeta");
        try
        {
            foreach (var process in processes)
            {
                try { if (!process.HasExited) { pid = (uint)process.Id; break; } }
                catch (InvalidOperationException) { }
            }
        }
        finally { foreach (var process in processes) process.Dispose(); }
        if (!_autoSchedule.TrySchedule(pid, _connectedPid, DateTime.UtcNow)) return;
        await AttachClientAsync(pid);
    }
}
