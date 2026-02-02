#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Stubs;

/// <summary>
/// Null implementation of <see cref="IChannelManager"/> for InnerShelf.
/// Channel functionality is disabled in this fork.
/// </summary>
public class NullChannelManager : IChannelManager
{
    /// <inheritdoc />
    public ChannelFeatures GetChannelFeatures(Guid? id) => null;

    /// <inheritdoc />
    public ChannelFeatures[] GetAllChannelFeatures() => Array.Empty<ChannelFeatures>();

    /// <inheritdoc />
    public bool EnableMediaSourceDisplay(BaseItem item) => false;

    /// <inheritdoc />
    public bool CanDelete(BaseItem item) => false;

    /// <inheritdoc />
    public Task DeleteItem(BaseItem item) => Task.CompletedTask;

    /// <inheritdoc />
    public Channel GetChannel(string id) => null;

    /// <inheritdoc />
    public Task<QueryResult<Channel>> GetChannelsInternalAsync(ChannelQuery query)
        => Task.FromResult(new QueryResult<Channel>());

    /// <inheritdoc />
    public Task<QueryResult<BaseItemDto>> GetChannelsAsync(ChannelQuery query)
        => Task.FromResult(new QueryResult<BaseItemDto>());

    /// <inheritdoc />
    public Task<QueryResult<BaseItemDto>> GetLatestChannelItems(InternalItemsQuery query, CancellationToken cancellationToken)
        => Task.FromResult(new QueryResult<BaseItemDto>());

    /// <inheritdoc />
    public Task<QueryResult<BaseItem>> GetLatestChannelItemsInternal(InternalItemsQuery query, CancellationToken cancellationToken)
        => Task.FromResult(new QueryResult<BaseItem>());

    /// <inheritdoc />
    public Task<QueryResult<BaseItemDto>> GetChannelItems(InternalItemsQuery query, CancellationToken cancellationToken)
        => Task.FromResult(new QueryResult<BaseItemDto>());

    /// <inheritdoc />
    public Task<QueryResult<BaseItem>> GetChannelItemsInternal(InternalItemsQuery query, IProgress<double> progress, CancellationToken cancellationToken)
        => Task.FromResult(new QueryResult<BaseItem>());

    /// <inheritdoc />
    public IEnumerable<MediaSourceInfo> GetStaticMediaSources(BaseItem item, CancellationToken cancellationToken)
        => Array.Empty<MediaSourceInfo>();
}
