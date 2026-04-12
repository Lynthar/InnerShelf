using Jellyfin.Plugin.InnerShelf.Sources;
using Jellyfin.Plugin.InnerShelf.Sources.BuiltIn;
using Jellyfin.Plugin.InnerShelf.Sources.MetaTube;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.InnerShelf;

/// <summary>
/// Registers plugin services in the DI container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<MetadataSourceManager>();
        serviceCollection.AddSingleton<IMetadataSource, JavBusSource>();
        serviceCollection.AddSingleton<IMetadataSource, MetaTubeSource>();
    }
}
