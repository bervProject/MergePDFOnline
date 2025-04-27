using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BervProject.MergePDF.Lambda
{
    /// <summary>
    /// A collection of sample Lambda functions that provide a REST api for doing simple math calculations. 
    /// </summary>
    public class Functions
    {
        private readonly IMerger _merger;
        private readonly IDownloader _downloader;
        private readonly IAmazonSimpleEmailServiceV2 _amazonSimpleEmailService;
        private readonly HttpClient _httpClient;
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Functions(IMerger merger, IHttpClientFactory httpClientFactory, IDownloader downloader)
        {
            _merger = merger;
            _downloader = downloader;
            var region = RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("SES_REGION") ?? "ap-southeast-1");
            _amazonSimpleEmailService = new AmazonSimpleEmailServiceV2Client(region);
            _httpClient = httpClientFactory.CreateClient("AppConfig");
        }

        /// <summary>
        /// Root route that provides information about the other requests that can be made.
        /// </summary>
        /// <returns></returns>
        [LambdaFunction]
        public async Task Default(ILambdaContext context)
        {
            var useEnhancedEmail = await IsEnhancedEmail(context);
            var success = false;
            var message = useEnhancedEmail ? """<strong>Failed</strong> to merge.<br>""" : "Failed to merge. ";
            var destinationPath = string.Empty;
            try
            {
                var generatedTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                destinationPath = $"merged/certificates-{generatedTimestamp}.pdf";
                success = await _merger.Merge("certificates", destinationPath);
                context.Logger.LogInformation($"Result: {success}");
                if (success)
                {
                    message = useEnhancedEmail ? $"""Success merge the result to: <b>{destinationPath}</b>. You can check the attached document.""" : $"Success merge the result to: {destinationPath}. You can check the attached document.";
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError(ex.Message);
                destinationPath = string.Empty;
                message += ex.Message;
            }
            finally
            {
                await SendEnhancedEmail(success, message, destinationPath);
            }
        }

        private async Task<bool> IsEnhancedEmail(ILambdaContext context)
        {
            try
            {
                var result =
                    await _httpClient.GetAsync("applications/pdf_merger/environments/dev/configurations/feature_a");
                var content = await result.Content.ReadAsStringAsync();
                var statusCode = result.StatusCode;
                context.Logger.LogInformation($"StatusCode: {statusCode}. Result: {content}");
                var jsonContent = JsonSerializer.Deserialize<JsonObject>(content);
                if (jsonContent != null && jsonContent.ContainsKey("use_enhanced_message"))
                {
                    var useEnhancedMessage = jsonContent["use_enhanced_message"];
                    if (useEnhancedMessage?["enabled"] != null)
                    {
                        return bool.Parse(useEnhancedMessage["enabled"]?.ToString() ?? string.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError(ex.Message);
                return false;
            }

            return false;
        }
        
        private async Task SendEnhancedEmail(bool status, string bodyMessage, string attachmentPath = "")
        {
            var emailRequest = new SendEmailRequest
            {
                Destination = new Destination
                {
                    ToAddresses = [Environment.GetEnvironmentVariable("TO_EMAIL") ?? ""]
                },
                FromEmailAddress = Environment.GetEnvironmentVariable("FROM_EMAIL"),
                Content = new EmailContent
                {
                    Simple = new Message
                    {
                        Subject = new Content
                        {
                            Data = $"PDF Merge Result - {(status ? "Success" : "Failed")}"
                        },
                        Body = new Body
                        {
                            Html = new Content
                            {
                                Data = bodyMessage
                            }
                        }
                    }
                }
            };
            
            using var memoryStream = new MemoryStream();

            if (!string.IsNullOrEmpty(attachmentPath))
            {
                var stream = await _downloader.DownloadFileAsync(attachmentPath);
                await stream.CopyToAsync(memoryStream);
                emailRequest.Content.Simple.Attachments = new List<Attachment>
                {
                    new()
                    {
                        RawContent = memoryStream,
                        FileName = "merged.pdf"
                    }
                };
            }
            await _amazonSimpleEmailService.SendEmailAsync(emailRequest);
        }

    }
}