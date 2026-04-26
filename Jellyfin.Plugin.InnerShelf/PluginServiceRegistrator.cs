using System.Net;
using Jellyfin.Plugin.InnerShelf.Sources;
using Jellyfin.Plugin.InnerShelf.Sources.BuiltIn;
using Jellyfin.Plugin.InnerShelf.Sources.MetaTube;
using Jellyfin.Plugin.InnerShelf.Subtitles;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.InnerShelf;

/// <summary>
/// Registers plugin services in the DI container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <summary>
    /// Name of the default HttpClient used by built-in scrapers.
    /// </summary>
    public const string HttpClientName = "InnerShelf";

    /// <summary>
    /// Name of the HttpClient used for MetaTube backend requests.
    /// </summary>
    public const string MetaTubeHttpClientName = "InnerShelf.MetaTube";

    /// <summary>
    /// Name of the HttpClient used for subtitle-forge requests.
    /// </summary>
    public const string SubtitleForgeHttpClientName = "InnerShelf.SubtitleForge";

    // 30s ceiling for any single request to a configured upstream. The .NET
    // default of 100s was long enough that a single wedged proxy or stalled
    // remote could block library scans for minutes per item. 30s is generous
    // (typical scrape/MetaTube/forge call is 1-3s) but tight enough to fail
    // fast on real network trouble. Health.PingAsync uses its own 5s linked
    // CTS, which fires first and is unaffected by this ceiling.
    private static readonly TimeSpan HttpRequestTimeout = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient(HttpClientName, c => c.Timeout = HttpRequestTimeout)
            .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);

        serviceCollection.AddHttpClient(MetaTubeHttpClientName, c => c.Timeout = HttpRequestTimeout)
            .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);

        serviceCollection.AddHttpClient(SubtitleForgeHttpClientName, c => c.Timeout = HttpRequestTimeout)
            .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);

        serviceCollection.AddSingleton<Sources.MovieMetadataCache>();
        serviceCollection.AddSingleton<MetadataSourceManager>();
        serviceCollection.AddSingleton<IMetadataSource, JavBusSource>();
        serviceCollection.AddSingleton<IMetadataSource, MetaTubeSource>();

        serviceCollection.AddSingleton<SubtitleForgeClient>();
        serviceCollection.AddSingleton<IScheduledTask, BackfillSubtitlesTask>();
    }

    /// <summary>
    /// Creates the primary HttpMessageHandler used by all InnerShelf HTTP clients.
    /// Configures the proxy from the plugin configuration if set. Supports HTTP(S)
    /// and SOCKS4/SOCKS4a/SOCKS5 schemes (SocketsHttpHandler native support).
    /// </summary>
    private static SocketsHttpHandler CreatePrimaryHandler()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };

        var proxyUrl = Plugin.Instance?.Configuration.HttpProxy;
        if (!string.IsNullOrWhiteSpace(proxyUrl))
        {
            try
            {
                handler.Proxy = new WebProxy(proxyUrl);
                handler.UseProxy = true;
            }
            catch (UriFormatException)
            {
                // Invalid proxy URL — fall back to direct connection rather than crash.
            }
        }

        return handler;
    }
}
