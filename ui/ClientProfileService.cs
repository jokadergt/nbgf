using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RblxExecutorUI;

public sealed record ClientIdentity(string Name, long UserId, string JobId, long PlaceId)
{
    public static ClientIdentity? Parse(string value)
    {
        var parts = value.Split('|');
        if (parts.Length != 4 || string.IsNullOrWhiteSpace(parts[0]) ||
            parts[0].Equals("Unknown", StringComparison.OrdinalIgnoreCase) ||
            !long.TryParse(parts[1], out var userId) || userId <= 0 ||
            !long.TryParse(parts[3], out var placeId) || placeId < 0) return null;
        return new(parts[0], userId, parts[2], placeId);
    }
}

public sealed class ClientProfileService(HttpClient client)
{
    public async Task<byte[]?> GetAvatarAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId <= 0) return null;
        using var response = await client.GetAsync(
            $"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={userId}&size=150x150&format=Png&isCircular=false", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!json.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) return null;
        foreach (var entry in data.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object ||
                !entry.TryGetProperty("targetId", out var target) || target.ValueKind != JsonValueKind.Number || !target.TryGetInt64(out var id) || id != userId ||
                !entry.TryGetProperty("state", out var state) || state.ValueKind != JsonValueKind.String || state.GetString() != "Completed" ||
                !entry.TryGetProperty("imageUrl", out var url) || url.ValueKind != JsonValueKind.String ||
                !Uri.TryCreate(url.GetString(), UriKind.Absolute, out var imageUri) ||
                imageUri.Scheme != Uri.UriSchemeHttps ||
                !(imageUri.Host.Equals("rbxcdn.com", StringComparison.OrdinalIgnoreCase) ||
                  imageUri.Host.EndsWith(".rbxcdn.com", StringComparison.OrdinalIgnoreCase))) continue;
            return await client.GetByteArrayAsync(imageUri, cancellationToken);
        }
        // Pending/Blocked/failed responses are retried by the session poller.
        return null;
    }

    public async Task<string?> GetPlaceNameAsync(long placeId, CancellationToken cancellationToken)
    {
        if (placeId <= 0) return null;
        using var response = await client.GetAsync($"https://economy.roblox.com/v2/assets/{placeId}/details", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return json.RootElement.TryGetProperty("Name", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() : null;
    }
}
