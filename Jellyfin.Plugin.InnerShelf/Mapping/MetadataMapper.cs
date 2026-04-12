using Jellyfin.Data.Enums;
using Jellyfin.Plugin.InnerShelf.Sources.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.InnerShelf.Mapping;

/// <summary>
/// Maps internal metadata models to Jellyfin's MetadataResult types.
/// </summary>
public static class MetadataMapper
{
    /// <summary>
    /// The provider ID key used in Jellyfin's ProviderIds dictionary.
    /// </summary>
    public const string ProviderKey = "InnerShelf";

    /// <summary>
    /// Maps a <see cref="MovieMetadata"/> to a Jellyfin <see cref="MetadataResult{Movie}"/>.
    /// </summary>
    public static MetadataResult<Movie> ToMovieResult(MovieMetadata source)
    {
        var config = Plugin.Instance?.Configuration;
        var titleTemplate = config?.TitleTemplate ?? "{code} {title}";

        var displayTitle = titleTemplate
            .Replace("{code}", source.Code, StringComparison.OrdinalIgnoreCase)
            .Replace("{title}", source.Title ?? source.OriginalTitle ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        var movie = new Movie
        {
            Name = displayTitle,
            OriginalTitle = source.OriginalTitle,
            Overview = source.Overview,
            OfficialRating = "XXX",
            PremiereDate = source.ReleaseDate,
            ProductionYear = source.ReleaseDate?.Year,
            Genres = source.Genres.ToArray(),
            Studios = source.Studio is not null ? [source.Studio] : [],
            ProviderIds = new Dictionary<string, string>
            {
                [ProviderKey] = source.Code
            }
        };

        if (source.RuntimeMinutes.HasValue)
        {
            movie.RunTimeTicks = TimeSpan.FromMinutes(source.RuntimeMinutes.Value).Ticks;
        }

        // Map tags (Label, Series)
        var tags = new List<string>();
        if (!string.IsNullOrEmpty(source.Label))
        {
            tags.Add($"Label: {source.Label}");
        }

        if (!string.IsNullOrEmpty(source.Series))
        {
            tags.Add($"Series: {source.Series}");
        }

        movie.Tags = tags.ToArray();

        // Map people
        var people = new List<PersonInfo>();

        foreach (var actor in source.Actors)
        {
            people.Add(new PersonInfo
            {
                Name = actor.Name,
                ImageUrl = actor.ImageUrl,
                Type = PersonKind.Actor
            });
        }

        if (!string.IsNullOrEmpty(source.Director))
        {
            people.Add(new PersonInfo
            {
                Name = source.Director,
                Type = PersonKind.Director
            });
        }

        var result = new MetadataResult<Movie>
        {
            HasMetadata = true,
            Item = movie,
            People = people,
            Provider = ProviderKey
        };

        // Map images
        if (!string.IsNullOrEmpty(source.CoverUrl))
        {
            result.RemoteImages.Add((source.CoverUrl, ImageType.Primary));
        }

        if (!string.IsNullOrEmpty(source.BackdropUrl))
        {
            result.RemoteImages.Add((source.BackdropUrl, ImageType.Backdrop));
        }

        return result;
    }

    /// <summary>
    /// Maps a <see cref="MovieMetadata"/> to Jellyfin <see cref="RemoteSearchResult"/>.
    /// </summary>
    public static RemoteSearchResult ToSearchResult(SourceSearchResult source)
    {
        var result = new RemoteSearchResult
        {
            Name = source.Title ?? source.Code,
            SearchProviderName = ProviderKey,
            ImageUrl = source.ThumbnailUrl,
            PremiereDate = source.ReleaseDate,
            ProductionYear = source.ReleaseDate?.Year,
        };

        result.SetProviderId(ProviderKey, source.Code);

        return result;
    }

    /// <summary>
    /// Maps an <see cref="ActorMetadata"/> to a Jellyfin <see cref="MetadataResult{Person}"/>.
    /// </summary>
    public static MetadataResult<Person> ToPersonResult(ActorMetadata source)
    {
        var person = new Person
        {
            Name = source.Name,
            PremiereDate = source.BirthDate,
        };

        var result = new MetadataResult<Person>
        {
            HasMetadata = true,
            Item = person,
            Provider = ProviderKey
        };

        if (!string.IsNullOrEmpty(source.ImageUrl))
        {
            result.RemoteImages.Add((source.ImageUrl, ImageType.Primary));
        }

        return result;
    }
}
