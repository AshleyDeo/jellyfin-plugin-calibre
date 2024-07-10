using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.Calibre.Models
{
    /// <summary>
    /// Library configuration.
    /// </summary>
    public class ILibrary
    {
        /// <summary>
        /// Gets or sets the libraries used.
        /// </summary>
        [Required]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the library location.
        /// </summary>
        [Required]
        public string Location { get; set; } = null!;

        /// <summary>
        /// Gets or sets Calibre column with genres.
        /// </summary>
        [Required]
        public string Genres { get; set; } = "default";

        /// <summary>
        /// Gets or sets Calibre column with tags.
        /// </summary>
        [Required]
        public string Tags { get; set; } = "default";

        /// <summary>
        /// Gets or sets Community Rating Calibre column.
        /// Only last value will be kept.
        /// </summary>
        [Required]
        public string CommunityRating { get; set; } = "default";

        /// <summary>
        /// Gets or sets Parental Rating Calibre column.
        /// Only last value will be kept.
        /// </summary>
        [Required]
        public string ParentalRating { get; set; } = null!;

        /// <summary>
        /// Gets or sets Custom Rating Calibre column.
        /// Only last value will be kept.
        /// </summary>
        [Required]
        public string CustomRating { get; set; } = null!;
    }
}
