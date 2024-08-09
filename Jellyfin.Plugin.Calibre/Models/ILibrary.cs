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
        /// Gets or sets Calibre column with authors.
        /// </summary>
        [Required]
        public string Artists { get; set; } = "default";

        /// <summary>
        /// Gets or sets Calibre column with authors.
        /// </summary>
        [Required]
        public string Authors { get; set; } = "default";

        /// <summary>
        /// Gets or sets Calibre column with authors.
        /// </summary>
        [Required]
        public string Colorists { get; set; } = "default";

        /// <summary>
        /// Gets or sets Calibre column with authors.
        /// </summary>
        [Required]
        public string CoverArtists { get; set; } = "default";

        /// <summary>
        /// Gets or sets Calibre column with editors.
        /// </summary>
        [Required]
        public string Editors { get; set; } = "default";

        /// <summary>
        /// Gets or sets Calibre column with illustrators.
        /// </summary>
        [Required]
        public string Illustrators { get; set; } = "default";

        /// <summary>
        /// Gets or sets Calibre column with inkers.
        /// </summary>
        [Required]
        public string Inkers { get; set; } = "default";

        /// <summary>
        /// Gets or sets Calibre column with translators.
        /// </summary>
        [Required]
        public string Letterers { get; set; } = "default";

        /// <summary>
        /// Gets or sets Calibre column with translators.
        /// </summary>
        [Required]
        public string Pencillers { get; set; } = "default";

        /// <summary>
        /// Gets or sets Calibre column with translators.
        /// </summary>
        [Required]
        public string Writers { get; set; } = "default";

        /// <summary>
        /// Gets or sets Calibre column with translators.
        /// </summary>
        [Required]
        public string Translators { get; set; } = "default";

        /// <summary>
        /// Gets or sets Calibre column with unknowns.
        /// </summary>
        [Required]
        public string OtherPeople { get; set; } = "default";

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
