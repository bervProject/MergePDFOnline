namespace BervProject.MergePDF;

/// <summary>
/// Downloader
/// </summary>
public interface IDownloader
{
    /// <summary>
    /// Download from folder handler
    /// </summary>
    /// <param name="folderPath">Source path</param>
    /// <returns>Enumerable of streams</returns>
    Task<IReadOnlyCollection<Stream>> DownloadFromFolderAsync(string folderPath);
    
    /// <summary>
    /// Download File handler
    /// </summary>
    /// <param name="filePath">Source path</param>
    /// <returns>Stream file</returns>
    Task<MemoryStream> DownloadFileAsync(string filePath);
}