using BervProject.MergePDFOnline.Utils;
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

app.MapGet("/drive", [GoogleScopedAuthorize(DriveService.ScopeConstants.Drive)] async ([FromServices] IGoogleAuthProvider auth) =>
{
    var cred = await auth.GetCredentialAsync();

    var driveService = new DriveService(new BaseClientService.Initializer
    {
        HttpClientInitializer = cred
    });
    var fileListRequest = driveService.Files.List();
    fileListRequest.Q = builder.Configuration["GoogleDrive:Query"];
    fileListRequest.Fields = "files(id,name,trashed)";
    fileListRequest.OrderBy = "name";
    var response = await fileListRequest.ExecuteAsync();
    if (response == null)
    {
        return null;
    }
    var output = GoogleDriveUtils.GetAllFiles(driveService, response.Files);
    return Results.File(output, "application/pdf");
})
.WithName("GetDrive")
.WithOpenApi();

app.Run();
