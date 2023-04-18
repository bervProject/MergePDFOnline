using Amazon.CDK;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using System.Collections.Generic;
using System.IO;

namespace BervProject.MergePDF.Infra
{
    public class BervProjectMergePDFInfraStack : Stack
    {
        internal BervProjectMergePDFInfraStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // The code that defines your stack goes here

            var role = Role.FromRoleName(this, "PDFMergeLambdaRole", "S3RoleLambda");
            var buildOption = new BundlingOptions()
            {
                Image = Runtime.DOTNET_6.BundlingImage,
                User = "root",
                OutputType = BundlingOutput.ARCHIVED,
                Command = new string[] {
                   "/bin/sh",
                    "-c",
                    " dotnet tool install -g Amazon.Lambda.Tools"+
                    " && cd BervProject.MergePDF.Lambda/src/BervProject.MergePDF.Lambda" +
                    " && dotnet build --configuration Release"+
                    " && dotnet lambda package --output-package /asset-output/function.zip"
                    }
            };
            var pdfMergerLambdaFunction = new Function(this, "PdfMerger", new FunctionProps
            {
                Runtime = Runtime.DOTNET_6,
                Timeout = Duration.Minutes(1),
                MemorySize = 512,
                Handler = "BervProject.MergePDF.Lambda::BervProject.MergePDF.Lambda.Functions_Default_Generated::Default",
                Code = Code.FromAsset(Directory.GetCurrentDirectory(), new Amazon.CDK.AWS.S3.Assets.AssetOptions
                {
                    Bundling = buildOption
                }),
                Environment = new Dictionary<string, string>
                {
                    { "S3__BucketName", System.Environment.GetEnvironmentVariable("BucketName") ?? "" }
                },
                Role = role,
            });
            var rule = new Rule(this, "CronRule", new RuleProps
            {
                Schedule = Schedule.Cron(new CronOptions
                {
                    Minute = "0",
                    Hour = "10",
                    Day = "1",
                    Month = "*",
                    Year = "*",
                }),
            });
            var lambdaFunctionTarget = new Amazon.CDK.AWS.Events.Targets.LambdaFunction(pdfMergerLambdaFunction, new LambdaFunctionProps());
            rule.AddTarget(lambdaFunctionTarget);
        }
    }
}
