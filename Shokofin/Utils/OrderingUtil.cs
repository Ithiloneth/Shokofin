using Shokofin.API.Info;
using Shokofin.API.Models;

namespace Shokofin.Utils
{
    public class Ordering
    {
        public enum GroupFilterType {
            Default = 0,
            Movies = 1,
            Others = 2,
        }

        /// <summary>
        /// Group series or movie box-sets
        /// </summary>
        public enum GroupType
        {
            /// <summary>
            /// No grouping. All series will have their own entry.
            /// </summary>
            Default = 0,

            /// <summary>
            /// Don't group, but make series merge-friendly by using the season numbers from TvDB.
            /// </summary>
            MergeFriendly = 1,

            /// <summary>
            /// Group seris based on Shoko's default group filter.
            /// </summary>
            ShokoGroup = 2,

            /// <summary>
            /// Group movies based on Shoko's series.
            /// </summary>
            ShokoSeries = 3,
        }

        /// <summary>
        /// Season or movie ordering when grouping series/box-sets using Shoko's groups.
        /// </summary>
        public enum OrderType
        {
            /// <summary>
            /// Let Shoko decide the order.
            /// </summary>
            Default = 0,

            /// <summary>
            /// Order seasons by release date.
            /// </summary>
            ReleaseDate = 1,

            /// <summary>
            /// Order seasons based on the chronological order of relations.
            /// </summary>
            Chronological = 2,
        }

        /// <summary>
        /// Get index number for a movie in a box-set.
        /// </summary>
        /// <returns>Absoute index.</returns>
        public static int GetMovieIndexNumber(GroupInfo group, SeriesInfo series, EpisodeInfo episode)
        {
            switch (Plugin.Instance.Configuration.BoxSetGrouping) {
                default:
                case GroupType.Default:
                    return 1;
                case GroupType.ShokoSeries:
                    return episode.AniDB.EpisodeNumber;
                case GroupType.ShokoGroup: {
                    int offset = 0;
                    foreach (SeriesInfo s in group.SeriesList) {
                        var sizes = s.Shoko.Sizes.Total;
                        if (s != series) {
                            if (episode.AniDB.Type == EpisodeType.Special) {
                                var index = series.FilteredSpecialEpisodesList.FindIndex(e => string.Equals(e.Id, episode.Id));
                                if (index == -1)
                                    throw new System.IndexOutOfRangeException("Episode not in filtered list");
                                return offset - (index + 1);
                            }
                            switch (episode.AniDB.Type) {
                                case EpisodeType.Normal:
                                    // offset += 0; // it's not needed, so it's just here as a comment instead.
                                    break;
                                case EpisodeType.Parody:
                                    offset += sizes?.Episodes ?? 0;
                                    goto case EpisodeType.Normal;
                                case EpisodeType.Other:
                                    offset += sizes?.Parodies ?? 0;
                                    goto case EpisodeType.Parody;
                            }
                            return offset + episode.AniDB.EpisodeNumber;
                        }
                        else {
                            if (episode.AniDB.Type == EpisodeType.Special) {
                                offset -= series.FilteredSpecialEpisodesList.Count;
                            }
                            offset += (sizes?.Episodes ?? 0) + (sizes?.Parodies ?? 0) + (sizes?.Others ?? 0);
                        }
                    }
                    break;
                }
            }
            return 0;
        }

        /// <summary>
        /// Get index number for an episode in a series.
        /// </summary>
        /// <returns>Absolute index.</returns>
        public static int GetIndexNumber(SeriesInfo series, EpisodeInfo episode)
        {
            switch (Plugin.Instance.Configuration.SeriesGrouping)
            {
                default:
                case GroupType.Default:
                    return episode.AniDB.EpisodeNumber;
                case GroupType.MergeFriendly: {
                    var epNum = episode?.TvDB.Number ?? 0;
                    if (epNum == 0)
                        goto case GroupType.Default;
                    return epNum;
                }
                case GroupType.ShokoGroup: {
                    if (episode.AniDB.Type == EpisodeType.Special) {
                        var index = series.FilteredSpecialEpisodesList.FindIndex(e => string.Equals(e.Id, episode.Id));
                        if (index == -1)
                            throw new System.IndexOutOfRangeException("Episode not in filtered list");
                        return -(index + 1);
                    }
                    int offset = 0;
                    var sizes = series.Shoko.Sizes.Total;
                    switch (episode.AniDB.Type) {
                        case EpisodeType.Normal:
                            // offset += 0; // it's not needed, so it's just here as a comment instead.
                            break;
                        case EpisodeType.Parody:
                            offset += sizes?.Episodes ?? 0;
                            goto case EpisodeType.Normal;
                        case EpisodeType.Other:
                            offset += sizes?.Parodies ?? 0;
                            goto case EpisodeType.Parody;
                    }
                    return offset + episode.AniDB.EpisodeNumber;
                }
            }
        }

        /// <summary>
        /// Get season number for an episode in a series.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="series"></param>
        /// <param name="episode"></param>
        /// <returns></returns>
        public static int GetSeasonNumber(GroupInfo group, SeriesInfo series, EpisodeInfo episode)
        {
            switch (Plugin.Instance.Configuration.SeriesGrouping) {
                default:
                case GroupType.Default:
                    switch (episode.AniDB.Type) {
                        case EpisodeType.Normal:
                            return 1;
                        case EpisodeType.Special:
                            return 0;
                        case EpisodeType.Trailer:
                            return 99;
                        case EpisodeType.ThemeSong:
                            return 100;
                        default:
                            return 98;
                    }
                case GroupType.MergeFriendly: {
                    var seasonNumber = episode?.TvDB?.Season;
                    if (seasonNumber == null)
                        goto case GroupType.Default;
                    return seasonNumber ?? 1;
                }
                case GroupType.ShokoGroup: {
                    var id = series.Id;
                    if (series == group.DefaultSeries)
                        return 1;
                    var index = group.SeriesList.FindIndex(s => s.Id == id);
                    if (index == -1)
                        goto case GroupType.Default;
                    var value = index - group.DefaultSeriesIndex;
                    return value < 0 ? value : value + 1;
                }
            }
        }
    }
}
