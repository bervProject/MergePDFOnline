using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using iText.Kernel.Pdf;
using static Google.Apis.Requests.BatchRequest;

namespace BervProject.MergePDFOnline.Utils;

public static class GoogleDriveUtils
{
    public static MemoryStream DownloadFile(DriveService driveService, string fileId)
    {
        var stream = new MemoryStream();
        var getFile = driveService.Files.Get(fileId);

        getFile.MediaDownloader.ProgressChanged +=
                    progress =>
                    {
                        switch (progress.Status)
                        {
                            case DownloadStatus.Downloading:
                                {
                                    Console.WriteLine(progress.BytesDownloaded);
                                    break;
                                }
                            case DownloadStatus.Completed:
                                {
                                    Console.WriteLine("Download complete.");
                                    break;
                                }
                            case DownloadStatus.Failed:
                                {
                                    Console.WriteLine("Download failed.");
                                    break;
                                }
                        }
                    };

        getFile.Download(stream);
        stream.Position = 0;
        return stream;
    }

    public static byte[] GetAllFiles(DriveService driveService, IList<Google.Apis.Drive.v3.Data.File> files)
    {
        var outputFile = new MemoryStream();
        var pdfDocument = new PdfDocument(new PdfWriter(outputFile));
        foreach (var file in files)
        {
            if (file.Trashed == true)
            {
                Console.WriteLine($"File {file.Id}:{file.Name} is ignored because already deleted!");
                continue;
            }
            try
            {
                Console.WriteLine(file.Id);
                var downloaded = GoogleDriveUtils.DownloadFile(driveService, file.Id);
                var copiedDocument = new PdfDocument(new PdfReader(downloaded));
                copiedDocument.CopyPagesTo(1, copiedDocument.GetNumberOfPages(), pdfDocument);
                copiedDocument.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{file.Name}: {ex.Message}");
            }
        }
        Console.WriteLine(pdfDocument.GetNumberOfPages());
        pdfDocument.Close();
        return outputFile.ToArray();
    }
}

