namespace BervProject.MergePDF;

public interface IUploader
{
    public Task UploadAsync(Stream file);
}