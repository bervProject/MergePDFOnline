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
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Functions(IMerger merger)
        {
            _merger = merger;
            var region = RegionEndpoint.GetBySystemName(Environment.GetEnvironmentVariable("SES_REGION") ?? "ap-southeast-1");
            _amazonSimpleEmailService = new AmazonSimpleEmailServiceClient(region);
        }

        /// <summary>
        /// Root route that provides information about the other requests that can be made.
        /// </summary>
        /// <returns></returns>
        [LambdaFunction()]
        public async Task Default(ILambdaContext context)
        {
            var success = false;
            var message = "Failed to merge. ";
            try
            {
                var generatedTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
                var destinationPath = $"merged/certificates-{generatedTimestamp}.pdf";
                success = await _merger.Merge("certificates", destinationPath);
                context.Logger.LogInformation($"Result: {success}");
                message = $"Success merge the result to: {destinationPath}";
            }
            catch (Exception ex)
            {
                context.Logger.LogError(ex.Message);
                message += ex.Message;
            }
            finally
            {
                await SendEmail(success, message);
            }


        }

        private async Task SendEmail(bool status, string bodyMessage)
        {
            var emailRequest = new SendEmailRequest
            {
                Destination = new Destination
                {
                    ToAddresses = new List<string> { Environment.GetEnvironmentVariable("TO_EMAIL") ?? "" }
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

    }
}