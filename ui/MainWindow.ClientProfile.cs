using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RblxExecutorUI;

public partial class MainWindow
{
    private readonly HttpClient _profileHttp = new() { Timeout = TimeSpan.FromSeconds(10) };
    private CancellationTokenSource? _profileCancellation;

    private void StopClientProfile()
    {
        _clientPoller?.Stop();
        _profileCancellation?.Cancel();
        _profileCancellation?.Dispose();
        _profileCancellation = null;
        ConnectedAccountChip.Visibility = System.Windows.Visibility.Collapsed;
        ClientAvatarImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Brand/cesi-selected.png"));
    }

    private void StartClientDataPoller(uint pid)
    {
        StopClientProfile();
        var cancellation = _profileCancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var service = new ClientProfileService(_profileHttp);
        var timer = _clientPoller = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        bool busy = false;
        long observedUser = 0, loadedAvatar = 0, resolvedPlace = 0;
        DateTime nextAvatar = DateTime.MinValue;
        string? placeName = null;
        bool IsCurrent() => !_closed && !token.IsCancellationRequested && _connectedPid == pid;
        async System.Threading.Tasks.Task RefreshAsync()
        {
            if (busy || !IsCurrent()) return;
            busy = true;
            try
            {
                var buffer = new StringBuilder(512);
                if (!RblxCore.GetClientInfo(buffer, buffer.Capacity)) return;
                var identity = ClientIdentity.Parse(buffer.ToString());
                if (identity == null || !IsCurrent()) return;
                if (observedUser != identity.UserId)
                {
                    observedUser = identity.UserId;
                    placeName = null;
                    resolvedPlace = 0;
                    loadedAvatar = 0;
                    nextAvatar = DateTime.MinValue;
                    ClientAvatarImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Brand/cesi-selected.png"));
                }
                ClientAccountName.Text = identity.Name;
                ConnectedAccountChip.Visibility = System.Windows.Visibility.Visible;
                if (resolvedPlace != identity.PlaceId) placeName = null;
                ClientPidText.Text = $"PID: {pid} | {placeName ?? identity.PlaceId.ToString()}";
                // Avatar download is independent of the optional place-name request.
                if (loadedAvatar != identity.UserId && DateTime.UtcNow >= nextAvatar)
                {
                    nextAvatar = DateTime.UtcNow.AddSeconds(5);
                    try
                    {
                        var bytes = await service.GetAvatarAsync(identity.UserId, token);
                        if (!IsCurrent()) return;
                        if (bytes is { Length: > 0 })
                        {
                            using var stream = new MemoryStream(bytes);
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = stream;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            ClientAvatarImage.Source = bitmap;
                            loadedAvatar = identity.UserId;
                        }
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
                    catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or
                        System.Text.Json.JsonException or IOException or NotSupportedException or ArgumentException)
                    {
                        nextAvatar = DateTime.UtcNow.AddSeconds(15);
                        System.Diagnostics.Debug.WriteLine($"Avatar: {ex.GetType().Name}");
                    }
                }
                if (identity.PlaceId != resolvedPlace && identity.PlaceId > 0)
                {
                    try
                    {
                        var name = await service.GetPlaceNameAsync(identity.PlaceId, token);
                        if (!IsCurrent()) return;
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            placeName = name;
                            resolvedPlace = identity.PlaceId;
                            ClientPidText.Text = $"PID: {pid} | {placeName}";
                        }
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
                    catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException)
                    { System.Diagnostics.Debug.WriteLine($"Place: {ex.GetType().Name}"); }
                }
            }
            catch (Exception ex) when (ex is System.Runtime.InteropServices.SEHException or EntryPointNotFoundException)
            {
                timer.Stop();
                ShowNotification("Error: Client profile API unavailable in native core", true);
            }
            finally { busy = false; }
        }
        timer.Tick += async (_, _) => await RefreshAsync();
        timer.Start();
        _ = RefreshAsync();
    }
}
