using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using BervProject.MergePDF.S3.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BervProject.MergePDF.S3;

public class Merger : IMerger
{
    private readonly ILogger<Merger> _logger;
    private readonly IDownloader _downloader;
    private readonly IAmazonS3 _amazonS3Service;
    private readonly S3Settings _s3Settings;
    private readonly IPdfMerger _pdfMerger;
    
    public Merger(IAmazonS3 amazonS3Service, IPdfMerger pdfMerger, IDownloader downloader, ILogger<Merger> logger, IOptions<S3Settings> s3Settings)
    {
        _amazonS3Service = amazonS3Service;
        _pdfMerger = pdfMerger;
        _downloader = downloader;
        _logger = logger;
        _s3Settings = s3Settings.Value;
    }
    public async Task<bool> Merge(string sourcePath, string destinationPath)
    {
        var downloadResult = await _downloader.DownloadAsync(sourcePath);
        var mergedStream = _pdfMerger.MergeFiles(downloadResult);
        var putRequest = new PutObjectRequest
        {
            BucketName = _s3Settings.BucketName,
            Key = destinationPath,
            InputStream = mergedStream
        };
        var uploadResponse = await _amazonS3Service.PutObjectAsync(putRequest);
        var httpStatusCode = uploadResponse.HttpStatusCode;
        _logger.LogInformation("Result: {HttpStatusCode}", httpStatusCode);
        return uploadResponse is { HttpStatusCode: HttpStatusCode.OK };
    }
}