using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BervProject.MergePDF.Lambda
{
    /// <summary>
    /// A collection of sample Lambda functions that provide a REST api for doing simple math calculations. 
    /// </summary>
    public class Functions
    {
        private readonly IMerger _merger;
        private readonly IAmazonSimpleEmailService _amazonSimpleEmailService;
        private readonly HttpClient _httpClient;
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Functions(IMerger merger, IHttpClientFactory httpClientFactory)
        {
            _merger = merger;
            var region = RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("SES_REGION") ?? "ap-southeast-1");
            _amazonSimpleEmailService = new AmazonSimpleEmailServiceClient(region);
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
            try
            {
                var generatedTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                var destinationPath = $"merged/certificates-{generatedTimestamp}.pdf";
                (success, var path) = await _merger.Merge("certificates", destinationPath);
                context.Logger.LogInformation($"Result: {success}");
                if (success)
                {
                    message = useEnhancedEmail ? $"""Success merge the result to: <b>{destinationPath}</b>. You can check <a href="{path}">here</a>.""" : $"Success merge the result to: {destinationPath}. You can check here: {path}";
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError(ex.Message);
                message += ex.Message;
            }
            finally
            {
                if (useEnhancedEmail)
                {
                    await SendEnhancedEmail(success, message);
                }
                else
                {
                    await SendEmail(success, message);
                }
            }
        }

        private async Task SendEmail(bool status, string bodyMessage)
        {
            var emailRequest = new SendEmailRequest
            {
                Destination = new Destination
                {
                    ToAddresses = [Environment.GetEnvironmentVariable("TO_EMAIL") ?? ""]
                },
                Source = Environment.GetEnvironmentVariable("FROM_EMAIL"),
                Message = new Message
                {
                    Subject = new Content
                    {
                        Data = $"PDF Merge Result - {(status ? "Success" : "Failed")}"
                    },
                    Body = new Body
                    {
                        Text = new Content { Data = bodyMessage },
                    }
                }
            };
            await _amazonSimpleEmailService.SendEmailAsync(emailRequest);
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
        
        private async Task SendEnhancedEmail(bool status, string bodyMessage)
        {
            var emailRequest = new SendEmailRequest
            {
                Destination = new Destination
                {
                    ToAddresses = [Environment.GetEnvironmentVariable("TO_EMAIL") ?? ""]
                },
                Source = Environment.GetEnvironmentVariable("FROM_EMAIL"),
                Message = new Message
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
            };
            await _amazonSimpleEmailService.SendEmailAsync(emailRequest);
        }

    }
}