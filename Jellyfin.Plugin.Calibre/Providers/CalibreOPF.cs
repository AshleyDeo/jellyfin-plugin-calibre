using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Plugin.Calibre.Configuration;
using Jellyfin.Plugin.Calibre.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Calibre.Providers
{
    /// <summary>
    /// Calibre OPF book provider.
    /// </summary>
    public class CalibreOpf : ILocalMetadataProvider<Book>, IHasItemChangeMonitor
    {
        private const string OpfFile = "metadata.opf";

        private readonly IFileSystem _fileSystem;

        private readonly ILogger<CalibreOpf> _logger;
        private readonly PluginConfiguration _configuration;
        private ILibrary _library;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalibreOpf"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{BookProviderFromOpf}"/> interface.</param>
        public CalibreOpf(IFileSystem fileSystem, ILogger<CalibreOpf> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _configuration = Plugin.Instance!.Configuration;
            _library = new ILibrary();
        }

        /// <inheritdoc />
        public string Name => "Calibre";

        /// <inheritdoc />
        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            var file = GetXmlFile(item.Path);

            return file.Exists && _fileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
        }

        /// <inheritdoc />
        public Task<MetadataResult<Book>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var path = GetXmlFile(info.Path).FullName;

            try
            {
                var result = ReadOpfData(path, cancellationToken);

                if (result is null)
                {
                    return Task.FromResult(new MetadataResult<Book> { HasMetadata = false });
                }
                else
                {
                    return Task.FromResult(result);
                }
            }
            catch (FileNotFoundException)
            {
                return Task.FromResult(new MetadataResult<Book> { HasMetadata = false });
            }
        }

        private FileSystemMetadata GetXmlFile(string path)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            var directoryInfo = fileInfo.IsDirectory ? fileInfo : _fileSystem.GetDirectoryInfo(Path.GetDirectoryName(path)!);

            var directoryPath = directoryInfo.FullName;

            var specificFile = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(path) + ".opf");

            var file = _fileSystem.GetFileInfo(specificFile);

            if (file.Exists)
            {
                return file;
            }

            _logger.LogDebug("Metadata File found");
            file = _fileSystem.GetFileInfo(Path.Combine(directoryPath, OpfFile));

            var libPaths = directoryPath.Split('\\');
            string libPath = string.Empty;
            for (int i = 0; i < libPaths.Length - 2; i++)
            {
                libPath += libPaths[i];
                if (i < libPaths.Length - 3)
                {
                    libPath += '\\';
                }
            }

            GetLibrary(libPath);
            _logger.LogDebug("Library Found: {Name} - {Genre}", _library.Name, _library.Genres);

            return file;
        }

        private void GetLibrary(string path)
        {
            var libs = _configuration.LibConfigs;
            if (libs?.Length > 0)
            {
                foreach (var config in libs)
                {
                    if (config?.Location == path)
                    {
                        // _logger.LogInformation("Library Found: {Lib}", config.Name);
                        _library = config;
                        break;
                    }
                }
            }
        }

        private MetadataResult<Book> ReadOpfData(string metaFile, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var doc = new XmlDocument();
            doc.Load(metaFile);

            var utilities = new OpfReader<CalibreOpf>(doc, _logger, _library);
            return utilities.ReadOpfData(cancellationToken);
        }
    }
}
