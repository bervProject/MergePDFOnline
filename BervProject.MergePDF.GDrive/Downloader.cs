using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.Logging;

namespace BervProject.MergePDF.GDrive;

/// <inheritdoc />
public class Downloader : IDownloader
{
    private readonly DriveService _driveService;
    private readonly ILogger<Downloader> _logger;

    public Downloader(DriveService driveService, ILogger<Downloader> logger)
    {
        _driveService = driveService;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Stream>> DownloadAsync(string folderPath)
    {
        var result = new List<Stream>();
        var fileListRequest = _driveService.Files.List();
        fileListRequest.Q = folderPath;
        fileListRequest.Fields = "files(id,name,trashed)";
        fileListRequest.OrderBy = "name";
        var response = await fileListRequest.ExecuteAsync();

        foreach (var file in response.Files)
        {
            if (file.Trashed == true)
            {
                var fileId = file.Id;
                var fileName = file.Name;
                _logger.LogInformation("File {FileId}:{FileName} is ignored because already deleted", fileId, fileName);
                continue;
            }

            var downloadFile = DownloadFile(_driveService, file.Id);
            result.Add(downloadFile);
        }

        return result;
    }

    private MemoryStream DownloadFile(DriveService driveService, string fileId)
    {
        var stream = new MemoryStream();
        var getFile = driveService.Files.Get(fileId);

        getFile.MediaDownloader.ProgressChanged +=
            progress =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                    {
                        Console.WriteLine(progress.BytesDownloaded);
                        break;
                    }
                    case DownloadStatus.Completed:
                    {
                        Console.WriteLine("Download complete.");
                        break;
                    }
                    case DownloadStatus.Failed:
                    {
                        Console.WriteLine("Download failed.");
                        break;
                    }
                }
            };

        getFile.Download(stream);
        stream.Position = 0;
        return stream;
    }
}