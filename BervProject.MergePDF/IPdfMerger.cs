namespace BervProject.MergePDF;

/// <summary>
/// Interface to merge with Pdf
/// </summary>
public interface IPdfMerger
{
    /// <summary>
    /// Merge files from Streams
    /// </summary>
    /// <param name="files">Streams</param>
    /// <returns>A merged files</returns>
    Stream MergeFiles(IReadOnlyCollection<Stream> files);
}