namespace BervProject.MergePDF;

public interface IUploader
{
    public Task<(bool, string)> UploadAsync(Stream file, string destination);
}