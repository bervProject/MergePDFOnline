using Amazon.S3;
using Amazon.SimpleEmail;
using BervProject.MergePDF.S3;
using BervProject.MergePDF.S3.Models;

namespace BervProject.MergePDF.Lambda
{
    [Amazon.Lambda.Annotations.LambdaStartup]
    public class Startup
    {
        /// <summary>
        /// Services for Lambda functions can be registered in the services dependency injection container in this method. 
        ///
        /// The services can be injected into the Lambda function through the containing type's constructor or as a
        /// parameter in the Lambda function using the FromService attribute. Services injected for the constructor have
        /// the lifetime of the Lambda compute container. Services injected as parameters are created within the scope
        /// of the function invocation.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            var builder = new ConfigurationBuilder()
                                        .AddJsonFile("appsettings.json", true)
                                        .AddEnvironmentVariables();

            //// Add AWS Systems Manager as a potential provider for the configuration. This is 
            //// available with the Amazon.Extensions.Configuration.SystemsManager NuGet package.
            // builder.AddSystemsManager("/mergepdf/settings");

            var configuration = builder.Build();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(logger =>
            {
                logger.AddLambdaLogger();
                logger.SetMinimumLevel(LogLevel.Debug);
            });
            services.Configure<S3Settings>(configuration.GetSection("S3"));
            services.AddAWSService<IAmazonS3>();
            services.AddAWSService<IAmazonSimpleEmailService>();
            services.AddScoped<IDownloader, Downloader>();
            services.AddScoped<IPdfMerger, PdfMerger>();
            services.AddScoped<IUploader, Uploader>();
            services.AddScoped<IMerger, Merger>();
            services.AddHttpClient("AppConfig", client =>
            {
                client.BaseAddress = new Uri("http://localhost:2772");
            });
        }
    }
}