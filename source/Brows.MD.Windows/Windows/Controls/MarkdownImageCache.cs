using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Brows.Windows.Controls;

internal sealed class MarkdownImageCache {
    private const int MaximumRemoteImageBytes = 16 * 1024 * 1024;

    private static readonly HttpClient Client = new(new HttpClientHandler { UseCookies = false }) {
        MaxResponseContentBufferSize = MaximumRemoteImageBytes
    };

    private static readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        ResourceLock = new();

    /*
     * A blocked UNC read must leave capacity for this preview's local files.
     */
    private readonly SemaphoreSlim FileSlots = new(4);
    private readonly SemaphoreSlim RemoteSlots = new(4);
    private readonly Dictionary<Uri, Entry> Entries = [];

    private static BitmapImage Decode(Stream stream) {
        var
        bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static Stream OpenResource(Uri uri) {
        /*
         * A WPF host can create controls on multiple dispatchers without an Application.
         */
        lock (ResourceLock) {
            return Application.GetResourceStream(uri)?.Stream;
        }
    }

    private static async Task<LoadedImage> ReadAsync(Uri uri, LoadedImage previous, CancellationToken token) {
        if (uri.IsFile) {
            var info = new FileInfo(uri.LocalPath);
            var length = info.Length;
            var modified = info.LastWriteTimeUtc;
            if (previous is not null && previous.Length == length && previous.Modified == modified) {
                return previous;
            }
            using var stream = info.OpenRead();
            return new LoadedImage(Decode(stream), length, modified);
        }
        if (uri.Scheme == "pack") {
            var dispatcher = Application.Current?.Dispatcher;
            using var resource = dispatcher is null ? OpenResource(uri) :
                await dispatcher.InvokeAsync(() => OpenResource(uri), DispatcherPriority.Background, token)
                    .Task.ConfigureAwait(false);
            return resource is null ? null : new LoadedImage(Decode(resource));
        }
        using var
        response = await Client.GetAsync(uri, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
#if NETFRAMEWORK
        using var content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
        using var content = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
#endif
        return new LoadedImage(Decode(content));
    }

    private async Task RefreshAsync(Uri uri, Entry entry, CancellationTokenSource cancellation) {
        var slots = uri.IsFile && !uri.IsUnc ? FileSlots : RemoteSlots;
        var token = cancellation.Token;
        try {
            await slots.WaitAsync(token);
            try {
                entry.Image = await Task.Run(() => ReadAsync(uri, entry.Image, token), token);
            }
            finally {
                slots.Release();
            }
        }
        catch (Exception ex)
        when (ex is IOException or NotSupportedException or FormatException or UnauthorizedAccessException or
                    WebException or ArgumentException or COMException or SecurityException or HttpRequestException or
                    OperationCanceledException or InvalidOperationException) {
            entry.Image = null;
        }
        finally {
            entry.Load = null;
            cancellation.Dispose();
        }
        var notify = entry.Notify;
        entry.Notify = null;
        notify?.Invoke(entry.Image?.Bitmap);
    }

    internal BitmapImage Get(Uri uri) {
        return Entries.TryGetValue(uri, out var entry) ? entry.Image?.Bitmap : null;
    }

    internal void Update(Dictionary<Uri, Action<BitmapImage>> requests) {
        foreach (var uri in Entries.Keys.Where(uri => requests?.ContainsKey(uri) != true).ToArray()) {
            var entry = Entries[uri];
            Entries.Remove(uri);
            entry.Notify = null;
            entry.Load?.Cancel();
        }
        if (requests is null) {
            return;
        }
        foreach (var request in requests) {
            if (!Entries.TryGetValue(request.Key, out var entry)) {
                Entries.Add(request.Key, entry = new Entry());
            }
            request.Value(entry.Image?.Bitmap);
            if (entry.Image is not null && !request.Key.IsFile) {
                continue;
            }
            entry.Notify = request.Value;
            if (entry.Load is null) {
                entry.Load = new CancellationTokenSource();
                _ = RefreshAsync(request.Key, entry, entry.Load);
            }
        }
    }

    private sealed class Entry {
        internal LoadedImage Image { get; set; }
        internal CancellationTokenSource Load { get; set; }
        internal Action<BitmapImage> Notify { get; set; }
    }

    private sealed class LoadedImage {
        internal BitmapImage Bitmap { get; }
        internal long Length { get; }
        internal DateTime Modified { get; }

        internal LoadedImage(BitmapImage bitmap, long length = 0, DateTime modified = default) {
            Bitmap = bitmap;
            Length = length;
            Modified = modified;
        }
    }
}
