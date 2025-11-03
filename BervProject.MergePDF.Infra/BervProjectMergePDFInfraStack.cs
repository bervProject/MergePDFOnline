using Amazon.CDK;
using Amazon.CDK.AWS.AppConfig;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using System.Collections.Generic;
using System.IO;
using Environment = Amazon.CDK.AWS.AppConfig.Environment;

namespace BervProject.MergePDF.Infra
{
    public class BervProjectMergePDFInfraStack : Stack
    {
        internal BervProjectMergePDFInfraStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // The code that defines your stack goes here

            // Configure AppConfig
            
            var appConfig = new Application(this, "PdfMergerAppConfig", new ApplicationProps
            {
                ApplicationName = "pdf_merger"
            });

            var appConfigEnvironment = new Environment(this, "PdfMergerAppConfigEnvironment", new EnvironmentProps
            {
                Application = appConfig,
                EnvironmentName = "dev",
                Description = "PDF Merger Development Environment"
            });

            appConfig.AddHostedConfiguration("FeatureA", new HostedConfigurationOptions
            {
                Name = "feature_a",
                Type = ConfigurationType.FEATURE_FLAGS,
                Content = ConfigurationContent.FromInlineJson(@"{
                    ""flags"": {
                        ""use_enhanced_message"": {
                             ""name"": ""Use Enhanced Message""
                          }
                    },
                    ""values"": {
                        ""use_enhanced_message"": {
                            ""enabled"": true
                        }
                    },
                    ""version"": ""1""
                }"),
                DeployTo = [appConfigEnvironment],
                
            });
            
            // Configure Role

            var role = Role.FromRoleName(this, "PDFMergeLambdaRole", "S3RoleLambda");

            appConfigEnvironment.GrantReadConfig(role);
            
            // Configure Lambda

            var appConfigLayer = LayerVersion.FromLayerVersionArn(this, "AppConfigExtension",
                "arn:aws:lambda:ap-southeast-3:418787028745:layer:AWS-AppConfig-Extension:120");
            
            var buildOption = new BundlingOptions
            {
                Image = Runtime.DOTNET_9.BundlingImage,
                User = "root",
                OutputType = BundlingOutput.ARCHIVED,
                Command =
                [
                    "/bin/sh",
                    "-c",
                    " dotnet tool install -g Amazon.Lambda.Tools"+
                    " && cd BervProject.MergePDF.Lambda/src/BervProject.MergePDF.Lambda" +
                    " && dotnet build --configuration Release"+
                    " && dotnet lambda package --output-package /asset-output/function.zip"
                ]
            };
            var pdfMergerLambdaFunction = new Function(this, "PdfMerger", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Timeout = Duration.Minutes(1),
                MemorySize = 512,
                Handler = "BervProject.MergePDF.Lambda::BervProject.MergePDF.Lambda.Functions_Default_Generated::Default",
                Code = Code.FromAsset(Directory.GetCurrentDirectory(), new Amazon.CDK.AWS.S3.Assets.AssetOptions
                {
                    Bundling = buildOption
                }),
                Layers = [appConfigLayer],
                Environment = new Dictionary<string, string>
                {
                    { "S3__BucketName", System.Environment.GetEnvironmentVariable("BucketName") ?? "" },
                    { "TO_EMAIL", System.Environment.GetEnvironmentVariable("TO_EMAIL") ?? "" },
                    { "FROM_EMAIL", System.Environment.GetEnvironmentVariable("FROM_EMAIL") ?? "" }
                },
                Role = role,
            });
            var rule = new Rule(this, "CronRule", new RuleProps
            {
                Schedule = Schedule.Cron(new CronOptions
                {
                    Minute = "0",
                    Hour = "10",
                    WeekDay = "1",
                    Month = "*",
                    Year = "*",
                }),
            });
            var lambdaFunctionTarget = new Amazon.CDK.AWS.Events.Targets.LambdaFunction(pdfMergerLambdaFunction, new LambdaFunctionProps());
            rule.AddTarget(lambdaFunctionTarget);
        }
    }
}
