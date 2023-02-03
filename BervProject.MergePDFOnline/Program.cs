using BervProject.MergePDF;
using BervProject.MergePDF.GDrive;
using BervProject.MergePDFOnline.Models;
using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<DriveService>(provider =>
{
    var auth = provider.GetRequiredService<IGoogleAuthProvider>();
    var credTask = auth.GetCredentialAsync();
    credTask.Wait();
    var cred = credTask.Result;
    var driveService = new DriveService(new BaseClientService.Initializer
    {
        HttpClientInitializer = cred
    });
    return driveService;
});
builder.Services.AddScoped<IPdfMerger, PdfMerger>();
builder.Services.AddScoped<IDownloader, Downloader>();
builder.Services.Configure<GoogleDriveSettings>(builder.Configuration.GetSection("GoogleDrive"));

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

app.MapGet("/drive", [GoogleScopedAuthorize(DriveService.ScopeConstants.Drive)]
        async ([FromServices] IPdfMerger merger, [FromServices] IDownloader downloader,
            [FromServices] IOptions<GoogleDriveSettings> options) =>
        {
            var path = options.Value.Query;
            var result = await downloader.DownloadAsync(path);
            var mergeResult = merger.MergeFiles(result);
            return Results.File(mergeResult, "application/pdf");
        })
    .WithName("GetDrive")
    .WithOpenApi();

app.Run();