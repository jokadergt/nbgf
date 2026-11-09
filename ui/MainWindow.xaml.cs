using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Runtime.InteropServices;

namespace RblxExecutorUI
{
    public class ScriptItem
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
    }

    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        private DispatcherTimer _notificationTimer;
        public ObservableCollection<ScriptItem> ScriptsList { get; set; } = new ObservableCollection<ScriptItem>();

        private bool _isExecuting = false;
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;

            _notificationTimer = new DispatcherTimer();
            _notificationTimer.Interval = TimeSpan.FromSeconds(3);
            _notificationTimer.Tick += (s, e) => CloseNotification();

            ScriptHubList.ItemsSource = ScriptsList;
            CollectionViewSource.GetDefaultView(ScriptsList).Filter = item =>
                item is ScriptItem script && script.Name.Contains(ScriptSearch.Text, StringComparison.OrdinalIgnoreCase);
            LoadScriptsFolder();
            PreviewKeyDown += Workspace_KeyDown;
            
            try 
            {
                string xshdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lua.xshd");
                if (File.Exists(xshdPath))
                {
                    using (XmlTextReader reader = new XmlTextReader(xshdPath))
                    {
                        Editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            } 
            catch { }
            InitializePreferences();
        }

        private void LoadScriptsFolder()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                ScriptsList.Clear();
                var files = Directory.GetFiles(path, "*.*")
                                     .Where(s => new[] { ".lua", ".luau", ".txt" }.Contains(Path.GetExtension(s), StringComparer.OrdinalIgnoreCase))
                                     .OrderBy(s => Path.GetFileName(s), StringComparer.OrdinalIgnoreCase);

                foreach (var file in files)
                {
                    ScriptsList.Add(new ScriptItem 
                    { 
                        Name = Path.GetFileName(file), 
                        FullPath = file 
                    });
                }
                RefreshLibrarySummary();
            }
            catch (Exception ex)
            {
                App.LogException(ex, "LoadScriptsFolder");
            }
        }

        private void ScriptSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ScriptHubList?.ItemsSource == null) return;
            RefreshLibrarySummary();
        }

        private void RefreshLibrarySummary()
        {
            var view = CollectionViewSource.GetDefaultView(ScriptsList);
            view.Refresh();
            int count = view.Cast<ScriptItem>().Count();
            LibrarySummary.Text = count == 0 ? T("Empty") : string.Format(T("Count"), count);
        }

        private void Workspace_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;
            if (e.Key == Key.S) { Save_Click(sender, e); e.Handled = true; }
            if (e.Key == Key.O) { Open_Click(sender, e); e.Handled = true; }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyAppearance();
                Editor.Text = "-- cesi\n-- Version: 1.0.0\n\nlocal player = game.Players.LocalPlayer\nprint(\"Hello, \" .. player.Name)";
                
                InitializeCore();
            }
            catch (Exception ex)
            {
                InjectionStatus.SetResourceReference(TextBlock.TextProperty, "Text.Error");
                InjectionStatus.SetResourceReference(TextBlock.ForegroundProperty, "SilverLight");
                App.LogException(ex, "MainWindow_Loaded");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitializeCore()
        {
            bool init = RblxCore.Initialize();
            _coreReady = init;
            if (init)
            {
                InjectionStatus.SetResourceReference(TextBlock.TextProperty, "Text.Disconnected");
                InjectionStatus.SetResourceReference(TextBlock.ForegroundProperty, "SilverLight"); // SilverText
            }
            else
            {
                InjectionStatus.SetResourceReference(TextBlock.TextProperty, "Text.Error");
                InjectionStatus.SetResourceReference(TextBlock.ForegroundProperty, "SilverLight");
            }
        }

        // Clip the actual content: Border.CornerRadius alone does not clip children.
        private void WindowContent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not FrameworkElement content || WindowFrame == null) return;
            UpdateContentClip();
        }

        // Window controls
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (e.OriginalSource is System.Windows.DependencyObject source)
            {
                for (var current = source; current != null; current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current))
                    if (current is System.Windows.Controls.Primitives.ButtonBase) return;
            }
            if (e.ClickCount == 2) { Maximize_Click(sender, e); return; }
            DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // Tabs
        private void Tab_Editor(object sender, RoutedEventArgs e) => SwitchTab(ViewEditor);
        private void Tab_ScriptHub(object sender, RoutedEventArgs e) => SwitchTab(ViewScriptHub);
        private void Tab_Clients(object sender, RoutedEventArgs e) => SwitchTab(ViewClients);
        private void Tab_Settings(object sender, RoutedEventArgs e) => SwitchTab(ViewSettings);

        private void SwitchTab(Grid target)
        {
            if (ViewEditor == null) return; // Prevent init crash
            ViewEditor.Visibility = Visibility.Collapsed;
            ViewScriptHub.Visibility = Visibility.Collapsed;
            ViewClients.Visibility = Visibility.Collapsed;
            ViewSettings.Visibility = Visibility.Collapsed;

            target.Visibility = Visibility.Visible;
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            target.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        // Notification System
        private void ShowNotification(string message, bool isError = false)
        {
            if (_closed) return;
            NotificationText.Text = IsStreamMode ? T("PrivateStatus") : LocalizeNotification(message);
            
            if (isError)
            {
                NotificationBox.SetResourceReference(Border.BorderBrushProperty, "SilverLight");
                NotificationIcon.Data = Geometry.Parse("M 2,14 L 14,14 L 8,2 Z M 8,11 L 8,12 M 8,6 L 8,9"); // Alert triangle
                NotificationIcon.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, "SilverLight");
            }
            else
            {
                NotificationBox.SetResourceReference(Border.BorderBrushProperty, "SilverLight"); // AccentGreen
                NotificationIcon.Data = Geometry.Parse("M 8,2 A 6,6 0 1 1 7.9,2.1 Z M 6,8 L 8,10 L 12,5"); // Check circle
                NotificationIcon.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, "SilverLight");
            }

            NotificationBox.Visibility = Visibility.Visible;
            
            // Slide in animation
            TranslateTransform transform = new TranslateTransform(50, 0);
            NotificationBox.RenderTransform = transform;
            
            DoubleAnimation slideIn = new DoubleAnimation(50, 0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new QuarticEase() { EasingMode = EasingMode.EaseOut }
            };
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));

            transform.BeginAnimation(TranslateTransform.XProperty, slideIn);
            NotificationBox.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            _notificationTimer.Stop();
            _notificationTimer.Start();
        }

        private void CloseNotification()
        {
            _notificationTimer.Stop();
            
            TranslateTransform transform = new TranslateTransform(0, 0);
            NotificationBox.RenderTransform = transform;
            
            DoubleAnimation slideOut = new DoubleAnimation(0, 20, TimeSpan.FromMilliseconds(200));
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            
            fadeOut.Completed += (s, e) => NotificationBox.Visibility = Visibility.Collapsed;

            transform.BeginAnimation(TranslateTransform.XProperty, slideOut);
            NotificationBox.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            if (_isExecuting || _isAttaching) return;
            if (_connectedPid == 0) { ShowNotification(T("Disconnected"), true); return; }
            if (!ExecutionAvailable) { ShowNotification(T("ExecutionPending"), true); return; }
            _isExecuting = true;
            try
            {
                string script = Editor.Text;
                if (string.IsNullOrWhiteSpace(script))
                {
                    ShowNotification("No script to execute", true);
                    _isExecuting = false;
                    return;
                }

                Console.WriteLine($"[C# UI] Executing EDITOR script ({script.Length} chars)...");
                ShowNotification("Executing Script...", false);

                int result = RblxCore.ExecuteScript(script);
                e.Handled = true;

                if (result == 0)
                {
                    ShowNotification("Executed Successfully!", false);
                }
                else
                {
                    string error = RblxCore.GetLastError();
                    ShowNotification($"Error: {error}", true);
                }
            }
            catch (Exception ex)
            {
                ShowNotification("Execution Exception", true);
                App.LogException(ex, "Execute_Click");
            }
            finally
            {
                _isExecuting = false;
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Editor.Text = "";
            ShowNotification("Editor Cleared", false);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
                if (!Directory.Exists(scriptPath)) Directory.CreateDirectory(scriptPath);

                var dialog = new Microsoft.Win32.SaveFileDialog()
                {
                    InitialDirectory = scriptPath,
                    Filter = "Lua Scripts (*.lua;*.luau;*.txt)|*.lua;*.luau;*.txt|All Files (*.*)|*.*",
                    DefaultExt = "lua"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, Editor.Text);
                    ShowNotification("Script Saved Successfully", false);
                    LoadScriptsFolder(); // Refresh the hub
                }
            }
            catch (Exception)
            {
                ShowNotification("Failed to save file", true);
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
                if (!Directory.Exists(scriptPath)) Directory.CreateDirectory(scriptPath);
                
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    InitialDirectory = scriptPath,
                    Filter = "Lua Scripts (*.lua;*.luau;*.txt)|*.lua;*.luau;*.txt|All Files (*.*)|*.*"
                };
                if (dialog.ShowDialog() == true)
                {
                    string content = File.ReadAllText(dialog.FileName);
                    Editor.Text = content;
                    Title = IsStreamMode ? "cesi" : $"cesi — {Path.GetFileName(dialog.FileName)}";
                    ShowNotification("File opened", false);
                }
            }
            catch (Exception)
            {
                ShowNotification("Failed to open file", true);
            }
        }

        private void ExecuteScriptHub_Click(object sender, RoutedEventArgs e)
        {
            if (_isExecuting || _isAttaching) return;
            if (_connectedPid == 0) { ShowNotification(T("Disconnected"), true); return; }
            if (!ExecutionAvailable) { ShowNotification(T("ExecutionPending"), true); return; }
            _isExecuting = true;
            try 
            {
                var btn = sender as Button;
                if (btn != null && btn.Tag != null)
                {
                    string path = btn.Tag.ToString() ?? "";
                    if (File.Exists(path))
                    {
                        string content = File.ReadAllText(path);
                        Console.WriteLine($"[C# UI] Executing HUB script: {Path.GetFileName(path)} ({content.Length} chars)...");
                        ShowNotification($"Executing {Path.GetFileName(path)}...", false);
                        int result = RblxCore.ExecuteScript(content);
                        ShowNotification(result == 0 ? "Executed Successfully!" : $"Error: {RblxCore.GetLastError()}", result != 0);
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowNotification("Hub script error!", true);
                App.LogException(ex, "ExecuteScriptHub_Click");
            }
            finally
            {
                _isExecuting = false;
            }
        }

        private void CopyScriptHub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                if (btn != null && btn.Tag != null)
                {
                    string path = btn.Tag.ToString() ?? "";
                    if (File.Exists(path))
                    {
                        Clipboard.SetText(File.ReadAllText(path));
                        ShowNotification("Code Copied!", false);
                    }
                }
            }
            catch (Exception)
            {
                ShowNotification("Copy Failed!", true);
            }
        }

        private void DeleteScriptHub_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                if (btn != null && btn.Tag != null)
                {
                    string path = btn.Tag.ToString() ?? "";
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        ShowNotification("Script Deleted!", false);
                        LoadScriptsFolder();
                    }
                }
            }
            catch (Exception)
            {
                ShowNotification("Delete Failed!", true);
            }
        }

        private void KillRoblox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var procs = System.Diagnostics.Process.GetProcessesByName("RobloxPlayerBeta");
                foreach (var p in procs) { p.Kill(); }
                ShowNotification("Roblox Terminated", false);
            }
            catch { ShowNotification("No Roblox to Kill", true); }
        }

        private void StartProcessMonitor(uint pid)
        {
            _processMonitor?.Stop();
            var timer = _processMonitor = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, ev) => 
            {
                try {
                    using var proc = System.Diagnostics.Process.GetProcessById((int)pid);
                    if (proc.HasExited) throw new Exception();
                } catch {
                    timer.Stop();
                    _connectedPid = 0;
                    ExecutionAvailable = false;
                    StopClientProfile();
                    // Reset UI
                    InjectionStatus.SetResourceReference(TextBlock.TextProperty, "Text.Disconnected");
                    InjectionStatus.SetResourceReference(TextBlock.ForegroundProperty, "SilverLight");
                    ClientPidText.SetResourceReference(TextBlock.TextProperty, "Text.Disconnected");
                    ClientStatusBadge.SetResourceReference(TextBlock.TextProperty, "Text.Ready");
                    ClientStatusBadge.SetResourceReference(TextBlock.ForegroundProperty, "SilverLight");
                    ClientBadgeBg.SetResourceReference(Border.BackgroundProperty, "CardHover");
                    ClientAccountName.SetResourceReference(TextBlock.TextProperty, "Text.Disconnected");
                    // Profile placeholder remains visible until a valid account photo arrives.
                    RblxCore.Disconnect();
                }
            };
            timer.Start();
        }

        private async void Attach_Click(object sender, RoutedEventArgs e) => await AttachClientAsync();

        private async System.Threading.Tasks.Task AttachClientAsync(uint detectedPid = 0)
        {
            if (_closed || !_coreReady || _isAttaching) return;
            _isAttaching = true;
            try
            {
                ShowNotification("Attaching to Roblox...", false);
                await System.Threading.Tasks.Task.Run(() => 
                {
                    uint pid = detectedPid != 0 ? detectedPid : RblxCore.FindRobloxProcess();
                    
                    if (_closed || pid == _connectedPid) return;
                    if (pid == 0)
                    {
                        Dispatcher.Invoke(() => ShowNotification("Roblox not found!", true));
                        return;
                    }

                    string actualVersion;
                    string executablePath;
                    using (var process = Process.GetProcessById((int)pid))
                    {
                        executablePath = process.MainModule?.FileName ?? "";
                        actualVersion = Path.GetDirectoryName(executablePath) is string directory
                            ? Path.GetFileName(directory) : "unknown";
                    }
                    string coreVersion;
                    try { coreVersion = RblxCore.GetOffsetVersion(); }
                    catch (EntryPointNotFoundException)
                    {
                        Dispatcher.Invoke(() => ShowNotification("Error: Syntax.dll legacy: missing build/version metadata. Rebuild the native core; client: " + actualVersion, true));
                        return;
                    }
                    if (!string.Equals(coreVersion, actualVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        Dispatcher.Invoke(() => ShowNotification($"Error: Offset version mismatch. Client: {actualVersion}; core: {coreVersion}", true));
                        return;
                    }
                    string expectedHash = RblxCore.GetExpectedExeSha256();
                    using (var executable = File.OpenRead(executablePath))
                    {
                        string actualHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(executable));
                        if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
                        {
                            Dispatcher.Invoke(() => ShowNotification("Error: Roblox executable SHA-256 does not match this core profile", true));
                            return;
                        }
                    }
                    Dispatcher.Invoke(() => ShowNotification($"Connecting to PID {pid}...", false));
                    bool connected = RblxCore.Connect(pid);
                    bool executionReady = connected && RblxCore.IsExecutionReady();
                    if (_closed) { if (connected) RblxCore.Disconnect(); return; }
                    
                    Dispatcher.Invoke(() => 
                    {
                        if (_closed) return;
                        if (connected)
                        {
                            _connectedPid = pid;
                            ExecutionAvailable = executionReady;
                            ShowNotification(T(executionReady ? "Connected" : "StructuralConnected"), false);
                            
                            InjectionStatus.SetResourceReference(TextBlock.TextProperty, executionReady ? "Text.Connected" : "Text.StructuralConnected");
                            InjectionStatus.SetResourceReference(TextBlock.ForegroundProperty, "SilverLight");
                            
                            // Update client page
                            ClientPidText.Text = $"PID: {pid}";
                            ClientStatusBadge.SetResourceReference(TextBlock.TextProperty, executionReady ? "Text.Connected" : "Text.StructuralConnected");
                            ClientStatusBadge.SetResourceReference(TextBlock.ForegroundProperty, "SilverLight");
                            ClientBadgeBg.SetResourceReference(Border.BackgroundProperty, "CardHover"); // 10% opacity
                            
                            // Start native polling and monitors
                            StartClientDataPoller(pid);
                            StartProcessMonitor(pid);
                        }
                        else
                        {
                            ShowNotification($"Error: {RblxCore.GetLastError()}", true);
                            InjectionStatus.SetResourceReference(TextBlock.TextProperty, "Text.Error");
                            InjectionStatus.SetResourceReference(TextBlock.ForegroundProperty, "SilverLight");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                ShowNotification("Attach Exception", true);
                if (!_closed) App.LogException(ex, "Attach_Click");
            }
            finally { _isAttaching = false; }
        }

        private void ToggleConsole_Checked(object sender, RoutedEventArgs e)
        {
            if (IsStreamMode) return;
            var handle = GetConsoleWindow();
            if (handle == IntPtr.Zero)
            {
                AllocConsole();
                RblxCore.RedirConsole(); // Sync C++ stdout to new console
                Console.WriteLine("[C# UI] Console allocated on user request.");
                handle = GetConsoleWindow();
            }
            
            if (handle != IntPtr.Zero) ShowWindow(handle, SW_SHOW);
        }

        private void ToggleConsole_Unchecked(object sender, RoutedEventArgs e)
        {
            var handle = GetConsoleWindow();
            if (handle != IntPtr.Zero) ShowWindow(handle, SW_HIDE);
        }
    }
}