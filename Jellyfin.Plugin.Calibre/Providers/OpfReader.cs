using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Calibre.Configuration;
using Jellyfin.Plugin.Calibre.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Calibre.Providers
{
    /// <summary>
    /// OPF reader.
    /// </summary>
    /// <typeparam name="TCategoryName">The type of category.</typeparam>
    public class OpfReader<TCategoryName>
    {
        private const string DcNamespace = @"http://purl.org/dc/elements/1.1/";
        private const string OpfNamespace = @"http://www.idpf.org/2007/opf";

        private readonly XmlNamespaceManager _namespaceManager;

        private readonly XmlDocument _document;

        private readonly ILogger<TCategoryName> _logger;
        private readonly ILibrary _library;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpfReader{TCategoryName}"/> class.
        /// </summary>
        /// <param name="doc">The xdocument to parse.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="library">The ILibrary Configuration.</param>
        public OpfReader(XmlDocument doc, ILogger<TCategoryName> logger, ILibrary library)
        {
            _document = doc;
            _logger = logger;
            _namespaceManager = new XmlNamespaceManager(_document.NameTable);
            _namespaceManager.AddNamespace("dc", DcNamespace);
            _namespaceManager.AddNamespace("opf", OpfNamespace);
            _library = library;
        }

        /// <summary>
        /// Checks the file path for the existence of a cover.
        /// </summary>
        /// <param name="opfRootDirectory">The root directory in which the opf metadata file is located.</param>
        /// <returns>Returns the found cover and it's type or null.</returns>
        public (string MimeType, string Path)? ReadCoverPath(string opfRootDirectory)
        {
            var coverImage = ReadEpubCoverInto(opfRootDirectory, "//opf:item[@properties='cover-image']");
            if (coverImage is not null)
            {
                return coverImage;
            }

            var coverId = ReadEpubCoverInto(opfRootDirectory, "//opf:item[@id='cover' and @media-type='image/*']");
            if (coverId is not null)
            {
                return coverId;
            }

            var coverImageId = ReadEpubCoverInto(opfRootDirectory, "//opf:item[@id='*cover-image']");
            if (coverImageId is not null)
            {
                return coverImageId;
            }

            var metaCoverImage = _document.SelectSingleNode("//opf:meta[@name='cover']", _namespaceManager);
            var content = metaCoverImage?.Attributes?["content"]?.Value;
            if (string.IsNullOrEmpty(content) || metaCoverImage is null)
            {
                return null;
            }

            var coverPath = Path.Combine("Images", content);
            var coverFileManifest = _document.SelectSingleNode($"//opf:item[@href='{coverPath}']", _namespaceManager);
            var mediaType = coverFileManifest?.Attributes?["media-type"]?.Value;
            if (coverFileManifest?.Attributes is not null
                            && !string.IsNullOrEmpty(mediaType) && IsValidImage(mediaType))
            {
                return (mediaType, Path.Combine(opfRootDirectory, coverPath));
            }

            var coverFileIdManifest = _document.SelectSingleNode($"//opf:item[@id='{content}']", _namespaceManager);
            if (coverFileIdManifest is not null)
            {
                return ReadManifestItem(coverFileIdManifest, opfRootDirectory);
            }

            return null;
        }

        /// <summary>
        /// Read opf data.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The metadata result to update.</returns>
        public MetadataResult<Book> ReadOpfData(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var book = CreateBookFromOpf();
            var bookResult = new MetadataResult<Book> { Item = book, HasMetadata = true };

            var creators = _document.SelectNodes("//dc:creator", _namespaceManager);
            var authorConfig = _library.Authors.Split(';');
            var editorsConfig = _library.Editors.Split(';');
            var illusConfig = _library.Illustrators.Split(';');
            var otherConfig = _library.OtherPeople.Split(';');
            var translatorConfig = _library.Translators.Split(';');
            if (creators != null && creators.Count > 0)
            {
                foreach (XmlElement creator in creators)
                {
                    var creatorName = creator.InnerText;
                    string? role = creator.GetAttribute("opf:role");
                    PersonKind? type = null;
                    switch (role)
                    {
                        case "arr":
                            if (authorConfig.Length > 0 && authorConfig[0] == "default")
                            {
                                type = PersonKind.Arranger;
                            }

                            break;
                        case "art":
                            if (authorConfig.Length > 0 && authorConfig[0] == "default")
                            {
                                type = PersonKind.Artist;
                            }

                            break;
                        case "aut":
                        case "aqt":
                        case "aft":
                        case "aui":
                        default:
                            if (authorConfig.Length > 0 && authorConfig[0] == "default")
                            {
                                type = PersonKind.Author;
                            }

                            break;
                        case "edt":
                            if (editorsConfig.Length > 0 && editorsConfig[0] == "default")
                            {
                            type = PersonKind.Editor;
                            }

                            break;
                        case "ill":
                            if (illusConfig.Length > 0 && illusConfig[0] == "default")
                            {
                                type = PersonKind.Illustrator;
                            }

                            break;
                        case "lyr":
                            if (authorConfig.Length > 0 && authorConfig[0] == "default")
                            {
                                type = PersonKind.Lyricist;
                            }

                            break;
                        case "mus":
                            if (authorConfig.Length > 0 && authorConfig[0] == "default")
                            {
                                type = PersonKind.AlbumArtist;
                            }

                            break;
                        case "oth":
                            if (otherConfig.Length > 0 && otherConfig[0] == "default")
                            {
                                type = PersonKind.Unknown;
                            }

                            break;
                        case "trl":
                            if (translatorConfig.Length > 0 && translatorConfig[0] == "default")
                            {
                                type = PersonKind.Translator;
                            }

                            break;
                    }

                    if (type != null)
                    {
                        var person = new PersonInfo { Name = creatorName, Type = (PersonKind)type };
                        bookResult.AddPerson(person);
                    }
                }
            }

            foreach (var column in authorConfig)
            {
                if (column == "defualt")
                {
                    continue;
                }
                else if (!string.IsNullOrEmpty(column))
                {
                    AddCustomPerson(column, bookResult, PersonKind.Author);
                }
            }

            foreach (var column in editorsConfig)
            {
                if (column == "defualt")
                {
                    continue;
                }
                else if (!string.IsNullOrEmpty(column))
                {
                    AddCustomPerson(column, bookResult, PersonKind.Editor);
                }
            }

            foreach (var column in illusConfig)
            {
                if (column == "defualt")
                {
                    continue;
                }
                else if (!string.IsNullOrEmpty(column))
                {
                    AddCustomPerson(column, bookResult, PersonKind.Illustrator);
                }
            }

            foreach (var column in otherConfig)
            {
                if (column == "defualt")
                {
                    continue;
                }
                else if (!string.IsNullOrEmpty(column))
                {
                    AddCustomPerson(column, bookResult, PersonKind.Translator);
                }
            }

            foreach (var column in translatorConfig)
            {
                if (column == "defualt")
                {
                    continue;
                }
                else if (!string.IsNullOrEmpty(column))
                {
                    AddCustomPerson(column, bookResult, PersonKind.Translator);
                }
            }

            var artistConfig = _library.Artists.Split(';');
            foreach (var column in artistConfig)
            {
                if (!string.IsNullOrEmpty(column))
                {
                    AddCustomPerson(column, bookResult, PersonKind.Artist);
                }
            }

            var coverartConfig = _library.CoverArtists.Split(';');
            foreach (var column in coverartConfig)
            {
                if (!string.IsNullOrEmpty(column))
                {
                    AddCustomPerson(column, bookResult, PersonKind.CoverArtist);
                }
            }

            var coloristConfig = _library.Colorists.Split(';');
            foreach (var column in coloristConfig)
            {
                if (!string.IsNullOrEmpty(column))
                {
                    AddCustomPerson(column, bookResult, PersonKind.Colorist);
                }
            }

            var inkerConfig = _library.Inkers.Split(';');
            foreach (var column in inkerConfig)
            {
                if (!string.IsNullOrEmpty(column))
                {
                    AddCustomPerson(column, bookResult, PersonKind.Inker);
                }
            }

            var lettererConfig = _library.Letterers.Split(';');
            foreach (var column in lettererConfig)
            {
                if (!string.IsNullOrEmpty(column))
                {
                    AddCustomPerson(column, bookResult, PersonKind.Letterer);
                }
            }

            var pencillerConfig = _library.Pencillers.Split(';');
            foreach (var column in pencillerConfig)
            {
                if (!string.IsNullOrEmpty(column))
                {
                    AddCustomPerson(column, bookResult, PersonKind.Penciller);
                }
            }

            var writerConfig = _library.Writers.Split(';');
            foreach (var column in writerConfig)
            {
                if (!string.IsNullOrEmpty(column))
                {
                    AddCustomPerson(column, bookResult, PersonKind.Letterer);
                }
            }

            ReadStringInto("//dc:language", language => bookResult.ResultLanguage = language);

            return bookResult;
        }

        private void AddCustomPerson(string colName, MetadataResult<Book> book, PersonKind type)
        {
            var names = FindUserMetadata(colName);
            if (names != null && names.Count > 0)
            {
                foreach (var name in names)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        var person = new PersonInfo { Name = name, Type = type };
                        _logger.LogDebug("Added {Name} as {Type}", name, type);
                        book.AddPerson(person);
                    }
                }
            }
        }

        private Book CreateBookFromOpf()
        {
            var book = new Book();

            book.Name = FindMainTitle();
            book.ForcedSortName = FindSortTitle();

            var seriesNameNode = _document.SelectSingleNode("//opf:meta[@name='calibre:series']", _namespaceManager);
            var seriesIndexNode = _document.SelectSingleNode("//opf:meta[@name='calibre:series_index']", _namespaceManager);
            if (!string.IsNullOrEmpty(seriesNameNode?.Attributes?["content"]?.Value))
            {
                try
                {
                    book.SeriesName = seriesNameNode.Attributes["content"]?.Value;
                }
                catch (Exception)
                {
                    _logger.LogError("Error parsing Calibre series name");
                }
            }

            ReadStringInto("//dc:description", summary => book.Overview = summary);
            ReadStringInto("//dc:publisher", publisher => book.AddStudio(publisher));
            ReadStringInto("//dc:identifier[@opf:scheme='ISBN']", isbn => book.SetProviderId("ISBN", isbn));
            ReadStringInto("//dc:identifier[@opf:scheme='AMAZON']", amazon => book.SetProviderId("Amazon", amazon));
            ReadStringInto("//dc:identifier[@opf:scheme='GOOGLE']", google => book.SetProviderId("GoogleBooks", google));
#pragma warning disable CA1307 // Specify StringComparison for clarity
            ReadStringInto("//dc:identifier[@opf:scheme='COMICVINE']", comicvine => book.SetProviderId("ComicVine", $"{book.SeriesName?.Replace(" ", "-")}/4000-{comicvine}"));
#pragma warning restore CA1307 // Specify StringComparison for clarity

            ReadStringInto("//dc:date", date =>
            {
                if (DateTime.TryParse(date, out var dateValue))
                {
                    book.PremiereDate = dateValue.Date;
                    book.ProductionYear = dateValue.Date.Year;
                }
            });

            XmlNodeList? genresNodes = FindTags();
            var genresConfig = _library.Genres.Split(';');
            foreach (var column in genresConfig)
            {
                if (column == "defualt")
                {
                    AddGenres(genresNodes, book);
                }
                else if (!string.IsNullOrEmpty(column))
                {
                    AddCustomGenres(column, book);
                }
            }

            var tagsConfig = _library.Tags.Split(';');
            foreach (var column in tagsConfig)
            {
                if (column == "defualt")
                {
                    AddTags(genresNodes, book);
                }
                else if (!string.IsNullOrEmpty(column))
                {
                    AddCustomTags(column, book);
                }
            }

            ReadInt32AttributeInto("//opf:meta[@name='calibre:series_index']", index => book.IndexNumber = index);

            var communityConfig = _library.CommunityRating.Split(';');
            foreach (var column in communityConfig)
            {
                if (column == "default")
                {
                    ReadInt32AttributeInto("//opf:meta[@name='calibre:rating']", rating => book.CommunityRating = rating);
                }
                else if (!string.IsNullOrEmpty(column))
                {
                    AddCommunityRating(column, book);
                }
            }

            var parentalConfig = _library.ParentalRating.Split(';');
            foreach (var column in parentalConfig)
            {
                if (!string.IsNullOrEmpty(column))
                {
                    AddParentalRating(column, book);
                }
            }

            var customConfig = _library.ParentalRating.Split(';');
            foreach (var column in customConfig)
            {
                if (!string.IsNullOrEmpty(column))
                {
                    AddCustomRating(column, book);
                }
            }

            return book;
        }

        /// <summary>
        /// Get Calibre Subject / Tags Column.
        /// </summary>
        private XmlNodeList? FindTags() => _document.SelectNodes("//dc:subject", _namespaceManager);

        /// <summary>
        /// Add Genres using nodes.
        /// </summary>
        private static void AddGenres(XmlNodeList? nodes, Book book)
        {
            if (nodes != null && nodes.Count > 0)
            {
                foreach (var node in nodes.Cast<XmlNode>())
                {
                    book.AddGenre(node.InnerText);
                }
            }
        }

        /// <summary>
        /// Add Tags using list.
        /// </summary>
        private static void AddTags(XmlNodeList? nodes, Book book)
        {
            if (nodes != null && nodes.Count > 0)
            {
                foreach (var node in nodes.Cast<XmlNode>())
                {
                    book.AddTag(node.InnerText);
                }
            }
        }

        /// <summary>
        /// Add Genres from Calibre custom column.
        /// </summary>
        private void AddCustomGenres(string colName, Book book)
        {
            var genres = FindUserMetadata(colName);
            if (genres != null && genres.Count > 0)
            {
                foreach (var genre in genres)
                {
                    if (!string.IsNullOrEmpty(genre))
                    {
                        book.AddGenre(genre);
                    }
                }
            }
        }

        /// <summary>
        /// Add Tags from Calibre custom column.
        /// </summary>
        private void AddCustomTags(string colName, Book book)
        {
            var tags = FindUserMetadata(colName);
            if (tags != null && tags.Count > 0)
            {
                foreach (var tag in tags)
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        book.AddTag(tag);
                    }
                }
            }
        }

        /// <summary>
        /// Add Community Rating.
        /// </summary>
        private void AddCommunityRating(string colName, Book book)
        {
            var communityRating = FindUserMetadata(colName);
            book.OfficialRating = (communityRating.Count > 0) ? communityRating[0] : "Unrated";
            _logger.LogDebug("Rating Found: {CommunityRating}", communityRating.Count);
        }

        /// <summary>
        /// Add Parental Rating.
        /// </summary>
        private void AddParentalRating(string colName, Book book)
        {
            var parentalRating = FindUserMetadata(colName);
            book.OfficialRating = (parentalRating.Count > 0) ? parentalRating[0] : "Unrated";
            _logger.LogDebug("Rating Found: {ParentalRating}", parentalRating.Count);
        }

        /// <summary>
        /// Add Custom Rating.
        /// </summary>
        private void AddCustomRating(string colName, Book book)
        {
            var customRating = FindUserMetadata(colName);
            book.OfficialRating = (customRating.Count > 0) ? customRating[0] : "Unrated";
            _logger.LogDebug("Rating Found: {CustomRating}", customRating.Count);
        }

        private string FindMainTitle()
        {
            string title = string.Empty;
            var titleTypes = _document.SelectNodes("//opf:meta[@property='title-type']", _namespaceManager);

            if (titleTypes is not null && titleTypes.Count > 0)
            {
                foreach (XmlElement titleNode in titleTypes)
                {
                    string refines = titleNode.GetAttribute("refines").TrimStart('#');
                    string titleType = titleNode.InnerText;

                    var titleElement = _document.SelectSingleNode($"//dc:title[@id='{refines}']", _namespaceManager);
                    if (titleElement is not null && string.Equals(titleType, "main", StringComparison.OrdinalIgnoreCase))
                    {
                        title = titleElement.InnerText;
                    }
                }
            }

            // fallback in case there is no main title definition
            if (string.IsNullOrEmpty(title))
            {
                ReadStringInto("//dc:title", titleStr => title = titleStr);
            }

            return title;
        }

        private string? FindSortTitle()
        {
            var titleTypes = _document.SelectNodes("//opf:meta[@property='file-as']", _namespaceManager);

            if (titleTypes is not null && titleTypes.Count > 0)
            {
                foreach (XmlElement titleNode in titleTypes)
                {
                    string refines = titleNode.GetAttribute("refines").TrimStart('#');
                    string sortTitle = titleNode.InnerText;

                    var titleElement = _document.SelectSingleNode($"//dc:title[@id='{refines}']", _namespaceManager);
                    if (titleElement is not null)
                    {
                        return sortTitle;
                    }
                }
            }

            // look for OPF 2.0 style title_sort
            var resultElement = _document.SelectSingleNode("//opf:meta[@name='calibre:title_sort']", _namespaceManager);
            var titleSort = resultElement?.Attributes?["content"]?.Value;

            return titleSort;
        }

        private List<string> FindUserMetadata(string colName)
        {
            var resultElement = _document.SelectSingleNode($"//opf:meta[@name='calibre:user_metadata:#{colName}']", _namespaceManager);
            var resultValue = resultElement?.Attributes?["content"]?.Value;
            // string resultValue = string.Empty;
            // ReadStringAttributeInto($"//opf:meta[@name='calibre:user_metadata:#{colName}']", res => resultValue = res);
            var pattern = "(?<=#value#:)[^,:|](.*?)(?=#extra#)";
#pragma warning disable CA1307 // Specify StringComparison for clarity
            resultValue = resultValue?.Replace("\"", string.Empty);
#pragma warning restore CA1307 // Specify StringComparison for clarity

            _logger.LogDebug("#{ColName} Content: {ResultValue}", colName, resultValue);

            List<string> result = new List<string>();
            Regex regexPat = new Regex(pattern);
            if (!string.IsNullOrEmpty(resultValue))
            {
                // Get matches of pattern in text
                var regexMatches = regexPat.Matches(resultValue);
                char[] removeChar = { '[', ' ', ']' };
                if (regexMatches?.Count > 0)
                {
                    var infoArr = regexMatches[0].Value.Split(',');
                    // _logger.LogError("Matches Found: {OtherRes}", regexMatches.Count);
                    for (int i = 0; i < infoArr.Length; i++)
                    {
                        infoArr[i] = infoArr[i].Substring(1);
                        if (i == 0 && infoArr[i][0] == '[')
                        {
                            infoArr[i] = infoArr[i].TrimStart(removeChar);
                        }

                        if (i == infoArr.Length - 2)
                        {
                            infoArr[i] = infoArr[i].TrimEnd(removeChar);
                        }

                        if (i == infoArr.Length - 1)
                        {
                            break;
                        }

                        result.Add(infoArr[i]);
                    }
                }
                else
                {
                    _logger.LogDebug("Regex Match is 0");
                }
            }
            else
            {
                _logger.LogDebug("Content String was empty");
            }

            _logger.LogDebug("#{ColName} List: {Result}", colName, result);

            return result;
        }

        private void ReadStringInto(string xPath, Action<string> commitResult)
        {
            var resultElement = _document.SelectSingleNode(xPath, _namespaceManager);
            if (resultElement is not null && !string.IsNullOrWhiteSpace(resultElement.InnerText))
            {
                commitResult(resultElement.InnerText);
            }
        }

        private void ReadInt32AttributeInto(string xPath, Action<int> commitResult)
        {
            var resultElement = _document.SelectSingleNode(xPath, _namespaceManager);
            var resultValue = resultElement?.Attributes?["content"]?.Value;
            if (!string.IsNullOrEmpty(resultValue))
            {
                try
                {
                    commitResult(Convert.ToInt32(Convert.ToDouble(resultValue, CultureInfo.InvariantCulture)));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error converting to int32");
                }
            }
        }

        private void ReadStringAttributeInto(string xPath, Action<string> commitResult)
        {
            var resultElement = _document.SelectSingleNode(xPath, _namespaceManager);
            var resultValue = resultElement?.Attributes?["content"]?.Value;
            if (!string.IsNullOrEmpty(resultValue))
            {
                try
                {
                    commitResult(resultValue);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error something wrong");
                }
            }
        }

        private (string MimeType, string Path)? ReadEpubCoverInto(string opfRootDirectory, string xPath)
        {
            var resultElement = _document.SelectSingleNode(xPath, _namespaceManager);
            if (resultElement is not null)
            {
                var resultValue = ReadManifestItem(resultElement, opfRootDirectory);
                return resultValue;
            }

            return null;
        }

        private static (string MimeType, string Path)? ReadManifestItem(XmlNode manifestNode, string opfRootDirectory)
        {
            var href = manifestNode.Attributes?["href"]?.Value;
            var mediaType = manifestNode.Attributes?["media-type"]?.Value;

            if (string.IsNullOrEmpty(href)
                || string.IsNullOrEmpty(mediaType)
                || !IsValidImage(mediaType))
            {
                return null;
            }

            var coverPath = Path.Combine(opfRootDirectory, href);
            return (MimeType: mediaType, Path: coverPath);
        }

        private static bool IsValidImage(string? mimeType)
        {
            return !string.IsNullOrEmpty(mimeType)
                   && !string.IsNullOrWhiteSpace(MimeTypes.ToExtension(mimeType));
        }
    }
}
