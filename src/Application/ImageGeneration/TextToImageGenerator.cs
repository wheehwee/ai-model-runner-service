using Application.ImageGeneration.Interfaces;
using Microsoft.Extensions.Logging;
using StableDiffusion.ML.OnnxRuntime.Interfaces;
using System.Collections.Concurrent;
using Vortice.DXGI;
using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces.TextToImageRuns;
using Domain.Enums;
using Common.Extensions;
using Domain.Entities;
using StableDiffusion.ML.OnnxRuntime.Configs;
using MassTransit.Mediator;
using Application.Events;

namespace Application.ImageGeneration
{
    public class TextToImageGenerator : ITextToImageGenerator
    {
        private SemaphoreSlim generateLock = new SemaphoreSlim(1, 1);
        private bool isGenerating = false;

        private const int DEQUEUE_INTERVAL_IN_MILLISECONDS = 500;
        private Timer _dequeueCheck;

        public class TextToImageRequest
        {
            public string RefId { get; set; }
            public string Prompt { get; set; }
            public string? NegativePrompt { get; set; }
        }

        private readonly ConcurrentQueue<TextToImageRequest> _requestQueue = new ConcurrentQueue<TextToImageRequest>();
        private IUNet _unet;
        private ILogger<TextToImageGenerator> _logger;
        private IServiceProvider _serviceProvider;
        private IMediator _mediator;

        private async Task SetIsGenerating(bool state)
        {
            try
            {
                await generateLock.WaitAsync();
                isGenerating = state;
            }
            finally
            {
                generateLock.Release();
            }
        }

        public TextToImageGenerator(IUNet unet, ILogger<TextToImageGenerator> logger, IServiceProvider serviceProvider, IMediator mediator)
        {
            _unet = unet;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _mediator = mediator;
            _dequeueCheck = new Timer(state =>
            {
                try
                {
                    if (_dequeueCheck == null || isGenerating) return;
                    var success = _requestQueue.TryDequeue(out var message);
                    if (!success || message == null) return;

                    SetIsGenerating(true).Wait();

                    Task.Run(async () =>
                    {
                        try
                        {
                            using (var scope = _serviceProvider.CreateScope())
                            {

                                var dbContext = scope.ServiceProvider.GetService<ITextToImageRunDataService>();
                                await StartGenerateImage(dbContext, message);
                            }
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError(ex.Message, ex);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing request queue for text to image generator: {ex.Message}", ex);
                }
                finally
                {
                    _dequeueCheck?.Change(TimeSpan.FromMilliseconds(DEQUEUE_INTERVAL_IN_MILLISECONDS), Timeout.InfiniteTimeSpan);
                }
            }, null, TimeSpan.FromMilliseconds(DEQUEUE_INTERVAL_IN_MILLISECONDS), Timeout.InfiniteTimeSpan);
        }

        public async Task<string> GenerateImage(string prompt, string negativePrompt = null)
        {
            string newRunId = string.Empty;

            await _mediator.Publish(new TextToImageRunDataEvent()
            {
                Task = async (serviceProvider) =>
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var textToImageDataService = scope.ServiceProvider.GetService<ITextToImageRunDataService>();

                        newRunId = await textToImageDataService.GetNextModelRunId();

                        var newT2IRun = new TextToImageRun
                        {
                            Id = newRunId,
                            State = ProcessingState.QUEUED,
                            Prompt = prompt,
                            NegativePrompt = negativePrompt,
                            CreationTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, "Asia/Bangkok".ToTimeZoneInfo())
                        };

                        await textToImageDataService.SaveTextToImageRun(newT2IRun.CreateDeepCopy());
                    }
                }
            });
            
            _requestQueue.Enqueue(new TextToImageRequest()
            {
                RefId = newRunId,
                Prompt = prompt,
                NegativePrompt = negativePrompt,
            });

            return newRunId;
        }

        private async Task StartGenerateImage(ITextToImageRunDataService dbContext, TextToImageRequest request)
        {
            await _mediator.Publish(new TextToImageRunDataEvent()
            {
                Task = async (serviceProvider) =>
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var textToImageDataService = scope.ServiceProvider.GetService<ITextToImageRunDataService>();

                        var T2IRun = new TextToImageRun
                        {
                            Id = request.RefId,
                            State = ProcessingState.RUNNING,
                            LastModificationTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, "Asia/Bangkok".ToTimeZoneInfo())
                        };

                        T2IRun.RecentlyModifiedFields.Clear();
                        T2IRun.RecentlyModifiedFields.Add(nameof(TextToImageRun.State));
                        await textToImageDataService.SaveTextToImageRun(T2IRun.CreateDeepCopy());
                    }
                }
            });

            DXGI.CreateDXGIFactory1(out IDXGIFactory4 factory);

            int? deviceId = null;
            var adapterIndex = 0;
            while (true)
            {
                var result = factory.EnumAdapters(adapterIndex, out var queryAdapter);
                if (result.Failure)
                {
                    break;
                }
                if (queryAdapter.Description.Description.ToLower().Contains("nvidia"))
                {
                    deviceId = adapterIndex;
                    //break;
                }
                adapterIndex++;
            }
            if (deviceId == null) throw new Exception("No suitable GPU found");

            var config = new StableDiffusionConfig
            {
                Width = 512,
                Height = 512,
                // Number of denoising steps
                NumInferenceSteps = 50,
                // Scale for classifier-free guidance
                GuidanceScale = 12,
                // Set your preferred Execution Provider. Currently (GPU, DirectML, CPU) are supported in this project.
                // ONNX Runtime supports many more than this. Learn more here: https://onnxruntime.ai/docs/execution-providers/
                // The config is defaulted to CUDA. You can override it here if needed.
                // To use DirectML EP intall the Microsoft.ML.OnnxRuntime.DirectML and uninstall Microsoft.ML.OnnxRuntime.GPU
                ExecutionProviderTarget = StableDiffusionConfig.ExecutionProvider.DirectML,
                // Set GPU Device ID.
                DeviceId = deviceId.Value,
                // Update paths to your models
                TextEncoderOnnxPath = @"..\Application\ImageGeneration\Models\text_encoder\model.onnx",
                UnetOnnxPath = @"..\Application\ImageGeneration\Models\unet\model.onnx",
                VaeDecoderOnnxPath = @"..\Application\ImageGeneration\Models\vae_decoder\model.onnx",
                SafetyModelPath = @"..\Application\ImageGeneration\Models\safety_checker\model.onnx",
                CustomTokenizerPath = @"..\Application\ImageGeneration\Models\tokenizer"
            };

            // Inference Stable Diff
            var image = await _unet.Inference(request.RefId, request.Prompt, config, request.NegativePrompt, (step) => {
                Task.Run(() => _mediator.Publish(new TextToImageRunDataEvent() 
                { 
                    Task = async (serviceProvider) =>
                    {
                        var stepT2IRun = new TextToImageRun
                        {
                            Id = request.RefId,
                            Step = step,
                            LastModificationTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, "Asia/Bangkok".ToTimeZoneInfo())
                        };
                        stepT2IRun.RecentlyModifiedFields.Clear();
                        stepT2IRun.RecentlyModifiedFields.Add(nameof(TextToImageRun.Step));

                        using (var scope = serviceProvider.CreateScope())
                        {
                            var textToImageDataService = scope.ServiceProvider.GetService<ITextToImageRunDataService>();

                            await textToImageDataService.SaveTextToImageRun(stepT2IRun);
                        }
                    }
                }));
            });

            // TODO: Upload image to the internet and get image url


            // Update last status of run
            await _mediator.Publish(new TextToImageRunDataEvent()
            {
                Task = async (serviceProvider) =>
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var textToImageDataService = scope.ServiceProvider.GetService<ITextToImageRunDataService>();

                        var T2IRun = new TextToImageRun
                        {
                            Id = request.RefId,
                            State = ProcessingState.DONE,
                            LastModificationTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, "Asia/Bangkok".ToTimeZoneInfo())
                        };
                        T2IRun.RecentlyModifiedFields.Clear();
                        T2IRun.RecentlyModifiedFields.Add(nameof(TextToImageRun.State));

                        await textToImageDataService.SaveTextToImageRun(T2IRun.CreateDeepCopy());
                    }
                }
            });
            

            await SetIsGenerating(false);
        }
    }
}
