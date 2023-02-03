// See https://aka.ms/new-console-template for more information

using Amazon.S3;
using BervProject.MergePDF;
using BervProject.MergePDF.S3;
using BervProject.MergePDF.S3.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json");

var config = configuration.Build();

var serviceCollection = new ServiceCollection();

serviceCollection.AddLogging();
serviceCollection.Configure<S3Settings>(config.GetSection("S3"));
serviceCollection.AddAWSService<IAmazonS3>();
serviceCollection.AddScoped<IDownloader, Downloader>();
serviceCollection.AddScoped<IPdfMerger, PdfMerger>();
serviceCollection.AddScoped<IMerger, Merger>();

var serviceProvider = serviceCollection.BuildServiceProvider();

var merger = serviceProvider.GetRequiredService<IMerger>();
var generatedTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
var destinationPath = $"merged/certificates-{generatedTimestamp}.pdf";
var result = await merger.Merge("certificates", destinationPath);
Console.WriteLine(result);