using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BervProject.MergePDF.Lambda
{
    /// <summary>
    /// A collection of sample Lambda functions that provide a REST api for doing simple math calculations. 
    /// </summary>
    public class Functions
    {
        private readonly IMerger _merger;
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Functions(IMerger merger)
        {
            _merger = merger;
        }

        /// <summary>
        /// Root route that provides information about the other requests that can be made.
        /// </summary>
        /// <returns></returns>
        [LambdaFunction()]
        public async Task Default(ILambdaContext context)
        {
            var generatedTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            var destinationPath = $"merged/certificates-{generatedTimestamp}.pdf";
            var result = await _merger.Merge("certificates", destinationPath);
            context.Logger.LogInformation($"Result: {result}");
        }

    }
}