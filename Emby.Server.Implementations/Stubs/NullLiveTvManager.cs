#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Stubs;

/// <summary>
/// Null implementation of <see cref="ILiveTvManager"/> for InnerShelf.
/// LiveTV functionality is disabled in this fork.
/// </summary>
public class NullLiveTvManager : ILiveTvManager
{
    /// <inheritdoc />
    public event EventHandler<GenericEventArgs<TimerEventInfo>> SeriesTimerCancelled;

    /// <inheritdoc />
    public event EventHandler<GenericEventArgs<TimerEventInfo>> TimerCancelled;

    /// <inheritdoc />
    public event EventHandler<GenericEventArgs<TimerEventInfo>> TimerCreated;

    /// <inheritdoc />
    public event EventHandler<GenericEventArgs<TimerEventInfo>> SeriesTimerCreated;

    /// <inheritdoc />
    public IReadOnlyList<ILiveTvService> Services => Array.Empty<ILiveTvService>();

    /// <inheritdoc />
    public Task<SeriesTimerInfoDto> GetNewTimerDefaults(CancellationToken cancellationToken)
        => Task.FromResult(new SeriesTimerInfoDto());

    /// <inheritdoc />
    public Task<SeriesTimerInfoDto> GetNewTimerDefaults(string programId, CancellationToken cancellationToken)
        => Task.FromResult(new SeriesTimerInfoDto());

    /// <inheritdoc />
    public Task CancelTimer(string id) => Task.CompletedTask;

    /// <inheritdoc />
    public Task CancelSeriesTimer(string id) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<TimerInfoDto> GetTimer(string id, CancellationToken cancellationToken)
        => Task.FromResult<TimerInfoDto>(null);

    /// <inheritdoc />
    public Task<SeriesTimerInfoDto> GetSeriesTimer(string id, CancellationToken cancellationToken)
        => Task.FromResult<SeriesTimerInfoDto>(null);

    /// <inheritdoc />
    public Task<QueryResult<BaseItemDto>> GetRecordingsAsync(RecordingQuery query, DtoOptions options)
        => Task.FromResult(new QueryResult<BaseItemDto>());

    /// <inheritdoc />
    public Task<QueryResult<TimerInfoDto>> GetTimers(TimerQuery query, CancellationToken cancellationToken)
        => Task.FromResult(new QueryResult<TimerInfoDto>());

    /// <inheritdoc />
    public Task<QueryResult<SeriesTimerInfoDto>> GetSeriesTimers(SeriesTimerQuery query, CancellationToken cancellationToken)
        => Task.FromResult(new QueryResult<SeriesTimerInfoDto>());

    /// <inheritdoc />
    public Task<BaseItemDto> GetProgram(string id, CancellationToken cancellationToken, User user = null)
        => Task.FromResult<BaseItemDto>(null);

    /// <inheritdoc />
    public Task<QueryResult<BaseItemDto>> GetPrograms(InternalItemsQuery query, DtoOptions options, CancellationToken cancellationToken)
        => Task.FromResult(new QueryResult<BaseItemDto>());

    /// <inheritdoc />
    public Task UpdateTimer(TimerInfoDto timer, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task UpdateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task CreateTimer(TimerInfoDto timer, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task CreateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<QueryResult<BaseItemDto>> GetRecommendedProgramsAsync(InternalItemsQuery query, DtoOptions options, CancellationToken cancellationToken)
        => Task.FromResult(new QueryResult<BaseItemDto>());

    /// <inheritdoc />
    public QueryResult<BaseItem> GetRecommendedProgramsInternal(InternalItemsQuery query, DtoOptions options, CancellationToken cancellationToken)
        => new QueryResult<BaseItem>();

    /// <inheritdoc />
    public LiveTvInfo GetLiveTvInfo(CancellationToken cancellationToken)
        => new LiveTvInfo { Services = Array.Empty<LiveTvServiceInfo>(), IsEnabled = false };

    /// <inheritdoc />
    public Task ResetTuner(string id, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Folder GetInternalLiveTvFolder(CancellationToken cancellationToken) => null;

    /// <inheritdoc />
    public IEnumerable<User> GetEnabledUsers() => Array.Empty<User>();

    /// <inheritdoc />
    public QueryResult<BaseItem> GetInternalChannels(LiveTvChannelQuery query, DtoOptions dtoOptions, CancellationToken cancellationToken)
        => new QueryResult<BaseItem>();

    /// <inheritdoc />
    public Task AddInfoToProgramDto(IReadOnlyCollection<(BaseItem Item, BaseItemDto ItemDto)> programs, IReadOnlyList<ItemFields> fields, User user = null)
        => Task.CompletedTask;

    /// <inheritdoc />
    public void AddChannelInfo(IReadOnlyCollection<(BaseItemDto ItemDto, LiveTvChannel Channel)> items, DtoOptions options, User user)
    {
    }

    /// <inheritdoc />
    public void AddInfoToRecordingDto(BaseItem item, BaseItemDto dto, ActiveRecordingInfo activeRecordingInfo, User user = null)
    {
    }

    /// <inheritdoc />
    public Task<BaseItem[]> GetRecordingFoldersAsync(User user)
        => Task.FromResult(Array.Empty<BaseItem>());
}
