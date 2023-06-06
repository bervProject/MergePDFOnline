namespace BervProject.MergePDF;

/// <summary>
/// Interface for Merger
/// </summary>
public interface IMerger
{
    /// <summary>
    /// Merge end-to-end
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="destinationPath"></param>
    /// <returns>Success/Failed</returns>
    Task<(bool result, string resultPath)> Merge(string sourcePath, string destinationPath);
}