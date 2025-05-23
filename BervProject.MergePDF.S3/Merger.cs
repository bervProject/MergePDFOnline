﻿using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using BervProject.MergePDF.S3.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BervProject.MergePDF.S3;

public class Merger : IMerger
{
    private readonly IDownloader _downloader;
    private readonly IPdfMerger _pdfMerger;
    private readonly IUploader _uploader;

    public Merger(IPdfMerger pdfMerger, IDownloader downloader, IUploader uploader)
    {
        _pdfMerger = pdfMerger;
        _downloader = downloader;
        _uploader = uploader;
    }

    
    /// <inheritdoc />
    public async Task<bool> Merge(string sourcePath, string destinationPath, string contentType)
    {
        var downloadResult = await _downloader.DownloadFromFolderAsync(sourcePath);
        var mergedStream = _pdfMerger.MergeFiles(downloadResult);
        return await _uploader.UploadAsync(mergedStream, destinationPath, contentType);
    }
}