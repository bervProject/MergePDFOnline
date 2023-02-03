namespace BervProject.MergePDF;

public interface IUploader
{
    public Task<bool> UploadAsync(Stream file, string destination);
}