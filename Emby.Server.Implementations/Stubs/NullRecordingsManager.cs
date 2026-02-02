using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Stubs;

/// <summary>
/// Null implementation of <see cref="IRecordingsManager"/> for InnerShelf.
/// Recording functionality is disabled in this fork.
/// </summary>
public class NullRecordingsManager : IRecordingsManager
{
    /// <inheritdoc />
    public string? GetActiveRecordingPath(string id) => null;

    /// <inheritdoc />
    public ActiveRecordingInfo? GetActiveRecordingInfo(string path) => null;

    /// <inheritdoc />
    public IEnumerable<VirtualFolderInfo> GetRecordingFolders() => Array.Empty<VirtualFolderInfo>();

    /// <inheritdoc />
    public Task CreateRecordingFolders() => Task.CompletedTask;

    /// <inheritdoc />
    public void CancelRecording(string timerId, TimerInfo? timer)
    {
    }

    /// <inheritdoc />
    public Task RecordStream(ActiveRecordingInfo recordingInfo, BaseItem channel, DateTime recordingEndDate)
        => Task.CompletedTask;
}
