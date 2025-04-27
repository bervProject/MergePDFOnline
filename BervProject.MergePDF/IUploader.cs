namespace BervProject.MergePDF;

/// <summary>
/// Uploader
/// </summary>
public interface IUploader
{
    /// <summary>
    /// Upload handler
    /// </summary>
    /// <param name="file"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    public Task<bool> UploadAsync(Stream file, string destination);
}