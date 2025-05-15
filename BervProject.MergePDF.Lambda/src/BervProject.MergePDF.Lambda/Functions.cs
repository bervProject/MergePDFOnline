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
                success = await _merger.Merge("certificates", destinationPath, "application/pdf");
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
                await SendEnhancedEmail(context, success, message, destinationPath);
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
        
        private async Task SendEnhancedEmail(ILambdaContext context, bool status, string bodyMessage, string attachmentPath = "")
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
            try
            {
                if (!string.IsNullOrEmpty(attachmentPath))
                {
                    // Download the file from S3
                    var stream = await _downloader.DownloadFileAsync(attachmentPath);
                    
                    // Check if the stream has content
                    if (stream != null && stream.Length > 0)
                    {
                        context.Logger.LogInformation($"Attachment: {attachmentPath}. File size: {stream.Length} bytes.");
                        
                        // Check if file size is within SES limits (40MB)
                        if (stream.Length > 40 * 1024 * 1024)
                        {
                            context.Logger.LogWarning($"Attachment size ({stream.Length} bytes) exceeds SES limit of 40MB. Skipping attachment.");
                            // Add a message to the email body about the large attachment
                            emailRequest.Content.Simple.Body.Html.Data += "<br><br>The merged PDF was too large to attach to this email.";
                        }
                        else
                        {
                            // Create a copy of the stream to ensure it's properly positioned and won't be disposed
                            byte[] fileBytes = stream.ToArray();
                            var attachmentStream = new MemoryStream(fileBytes);
                            
                            emailRequest.Content.Simple.Attachments =
                            [
                                new()
                                {
                                    RawContent = attachmentStream,
                                    FileName = Path.GetFileName(attachmentPath),
                                    ContentType = "application/pdf"
                                }
                            ];
                        }
                    }
                    else
                    {
                        context.Logger.LogError($"Failed to download attachment or attachment is empty: {attachmentPath}");
                        emailRequest.Content.Simple.Body.Html.Data += "<br><br>The PDF attachment could not be retrieved.";
                    }
                }
                
                var response = await _amazonSimpleEmailService.SendEmailAsync(emailRequest);
                context.Logger.LogInformation($"Email sent. Message ID: {response.MessageId}");
            } 
            catch (Exception ex)
            {
                context.Logger.LogError($"Error sending email: {ex.Message}");
                // Log the full exception for debugging
                context.Logger.LogError(ex.ToString());
            }
        }

    }
}