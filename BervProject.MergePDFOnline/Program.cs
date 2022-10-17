using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
        .AddAuthentication(o =>
        {
            // This forces challenge results to be handled by Google OpenID Handler, so there's no
            // need to add an AccountController that emits challenges for Login.
            o.DefaultChallengeScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
            // This forces forbid results to be handled by Google OpenID Handler, which checks if
            // extra scopes are required and does automatic incremental auth.
            o.DefaultForbidScheme = GoogleOpenIdConnectDefaults.AuthenticationScheme;
            // Default scheme that will handle everything else.
            // Once a user is authenticated, the OAuth2 token info is stored in cookies.
            o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddGoogleOpenIdConnect(options =>
        {
            options.ClientId = builder.Configuration["Google:ClientId"];
            options.ClientSecret = builder.Configuration["Google:ClientSecret"];
        });

builder.Services.AddAuthorization();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

var parentId = "0ByphvyJcG2Haa3B0WVFYRWZsaFk";

app.MapGet("/drive", [GoogleScopedAuthorize(DriveService.ScopeConstants.Drive)] async ([FromServices] IGoogleAuthProvider auth) =>
{
    var cred = await auth.GetCredentialAsync();

    var driveService = new DriveService(new BaseClientService.Initializer
    {
        HttpClientInitializer = cred
    });
    var fileListRequest = driveService.Files.List();
    fileListRequest.Q = $"mimeType='application/pdf' and '{parentId}' in parents and name contains '[Cert]'";
    fileListRequest.Fields = "files(id,name,trashed)";
    fileListRequest.OrderBy = "name";
    var response = await fileListRequest.ExecuteAsync();
    if (response == null)
    {
        return null;
    }
    var outputFile = new MemoryStream();
    var pdfDocument = new PdfDocument(new PdfWriter(outputFile));
    foreach (var file in response.Files)
    {
        if (file.Trashed == true)
        {
            Console.WriteLine($"File {file.Id}:{file.Name} is ignored because already deleted!");
            continue;
        }
        try
        {
            Console.WriteLine(file.Id);
            var downloaded = DownloadFile(driveService, file.Id);
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
    return Results.File(outputFile.ToArray(), "application/pdf");
})
.WithName("GetDrive")
.WithOpenApi();

app.Run();

static MemoryStream DownloadFile(DriveService driveService, string fileId)
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

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
