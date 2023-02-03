using System.Collections.Concurrent;

namespace BervProject.MergePDF;

/// <summary>
/// Downloader
/// </summary>
public interface IDownloader
{
    /// <summary>
    /// Downloader handle
    /// </summary>
    /// <param name="folderPath">Source path</param>
    /// <returns>Enumerable of streams</returns>
    Task<IReadOnlyCollection<Stream>> DownloadAsync(string folderPath);
}