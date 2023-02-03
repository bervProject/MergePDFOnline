using iText.Kernel.Pdf;
using Microsoft.Extensions.Logging;

namespace BervProject.MergePDF;

/// <inheritdoc />
public class PdfMerger : IPdfMerger
{
    private readonly ILogger<PdfMerger> _logger;

    public PdfMerger(ILogger<PdfMerger> logger)
    {
        _logger = logger;
    }
    
    /// <inheritdoc />
    public Stream MergeFiles(IReadOnlyCollection<Stream> files)
    {
        var outputFile = new MemoryStream();
        var pdfDocument = new PdfDocument(new PdfWriter(outputFile));
        foreach (var file in files)
        {
            try
            {
                var copiedDocument = new PdfDocument(new PdfReader(file));
                copiedDocument.CopyPagesTo(1, copiedDocument.GetNumberOfPages(), pdfDocument);
                copiedDocument.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when copy files to merge");            
            }
        }

        var pages = pdfDocument.GetNumberOfPages();
        _logger.LogInformation("The document pages: {Pages}", pages);
        pdfDocument.Close();
        var returnMemory = new MemoryStream(outputFile.ToArray());
        return returnMemory;
    }
}