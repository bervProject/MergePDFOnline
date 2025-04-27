using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using BervProject.MergePDF.S3.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BervProject.MergePDF.S3;

/// <inheritdoc />
public class Downloader : IDownloader
{
    private readonly ILogger<Downloader> _logger;
    private readonly IAmazonS3 _s3Service;
    private readonly S3Settings _s3Settings;

    public Downloader(IOptions<S3Settings> s3Settings, IAmazonS3 s3Service, ILogger<Downloader> logger)
    {
        _s3Settings = s3Settings.Value;
        _s3Service = s3Service;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Stream>> DownloadFromFolderAsync(string folderPath)
    {
        var resultData = new List<Stream>();
        var listObjectRequest = new ListObjectsV2Request
        {
            BucketName = _s3Settings.BucketName,
            Prefix = folderPath
        };
        bool getAll;

        do
        {
            var response = await _s3Service.ListObjectsV2Async(listObjectRequest);
            var objectsCount = response.S3Objects.Count;
            _logger.LogInformation("Get objects: {ObjectsCount}", objectsCount);
            foreach (var s3Object in response.S3Objects)
            {
                try
                {
                    if (s3Object.Size <= 0)
                    {
                        _logger.LogInformation("S3 Object {Key} has size below 0", s3Object.Key);
                        continue;
                    }
                    var req = new GetObjectRequest
                    {
                        BucketName = s3Object.BucketName,
                        Key = s3Object.Key
                    };
                    var objectResponse = await _s3Service.GetObjectAsync(req);
                    var copyMemory = new MemoryStream();
                    await using (var streamResponse = objectResponse.ResponseStream)
                    {
                        await streamResponse.CopyToAsync(copyMemory);
                    }
                    copyMemory.Position = 0;
                    resultData.Add(copyMemory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error when getting object");
                }
            }

            listObjectRequest.ContinuationToken = response.NextContinuationToken;
            getAll = string.IsNullOrEmpty(response.NextContinuationToken);
        } while (!getAll);

        return resultData;
    }

    /// <inheritdoc />   
    public async Task<MemoryStream> DownloadFileAsync(string filePath)
    {
        var response = await _s3Service.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _s3Settings.BucketName,
            Key = filePath
        });
        _logger.LogInformation("Response {HttpStatusCode}", response.HttpStatusCode);
        var memoryStream = new MemoryStream();
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            return memoryStream;
        }

        await using (var responseStream = response.ResponseStream)
        {
            await responseStream.CopyToAsync(memoryStream);
        }
        memoryStream.Position = 0;
        return memoryStream;
    }
}