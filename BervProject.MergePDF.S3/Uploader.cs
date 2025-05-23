﻿using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using BervProject.MergePDF.S3.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BervProject.MergePDF.S3;

public class Uploader : IUploader
{
    private readonly S3Settings _s3Settings;
    private readonly IAmazonS3 _amazonS3Service;
    private readonly ILogger<Uploader> _logger;
    public Uploader(IAmazonS3 amazonS3, IOptions<S3Settings> options, ILogger<Uploader> logger)
    {
        _amazonS3Service = amazonS3;
        _s3Settings = options.Value;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<bool> UploadAsync(Stream file, string destinationPath, string contentType)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = _s3Settings.BucketName,
            Key = destinationPath,
            InputStream = file,
            ContentType = contentType
        };
        var uploadResponse = await _amazonS3Service.PutObjectAsync(putRequest);
        var httpStatusCode = uploadResponse.HttpStatusCode;
        _logger.LogInformation("Result: {HttpStatusCode}", httpStatusCode);
        var success = uploadResponse is { HttpStatusCode: HttpStatusCode.OK };
        return success;
    }
}