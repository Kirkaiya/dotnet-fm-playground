using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using DotnetFMPlayground.Core.Builders;
using DotnetFMPlayground.Core.Models;
using System.Net;
using System.Text.Json;
using System.Text;
using DotnetFMPlayground.Core.Models.InferenceParameters;
using DotnetFMPlayground.Core.Models.ModelResponse;
using Amazon;
using System.Runtime.CompilerServices;

namespace DotnetFMPlayground.Core
{
    public static class AmazonBedrockRuntimeClientExtension
    {
        public static RegionEndpoint Region { get; set; } = RegionEndpoint.USEast1;

        public static async Task<IFoundationModelResponse?> InvokeModelAsync(this AmazonBedrockRuntimeClient client,
            string modelId,
            Prompt prompt,
            BaseInferenceParameters inferenceParameters,
            CancellationToken cancellationToken = default
            )
        {
            if (client.Config.RegionEndpoint != Region)
                client = new AmazonBedrockRuntimeClient(new AmazonBedrockRuntimeConfig { RegionEndpoint = Region });

            var invokeModelResponse = await client.InvokeModelAsync(
                InvokeModelRequestBuilder.Build(modelId, prompt, inferenceParameters),
                cancellationToken
            );

            return await FoundationModelResponseBuilder.Build(modelId, invokeModelResponse.Body);
        }

        public static async Task InvokeModelWithResponseStreamAsync(
            this AmazonBedrockRuntimeClient client,
            string modelId,
            Prompt prompt, 
            BaseInferenceParameters inferenceParameters,
            Func<string?, Task> chunckReceived,
            Func<string?, Task> exceptionReceived, 
            CancellationToken cancellationToken = default)
        {
            if (client.Config.RegionEndpoint != Region)
                client = new AmazonBedrockRuntimeClient(new AmazonBedrockRuntimeConfig { RegionEndpoint = Region });

            var response = await client.InvokeModelWithResponseStreamAsync(
                InvokeModelRequestBuilder.BuildWithResponseStream(modelId, prompt, inferenceParameters),
                cancellationToken
            );
            response.Body.ChunkReceived += async (sender, e) =>
            {
                var chunk = await FoundationModelResponseBuilder.Build(modelId, e.EventStreamEvent.Bytes, true);
                await chunckReceived(chunk?.GetResponse());
            };
            response.Body.ExceptionReceived += async (sender, e) =>
            {
                await exceptionReceived(e.EventStreamException.Message);
            };

            response.Body.StartProcessing();
        }
    }
}
