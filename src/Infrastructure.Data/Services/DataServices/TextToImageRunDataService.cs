using Application.Interfaces.TextToImageRuns;
using AutoMapper;
using Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Infrastructure.Data.Services.DataServices
{
    public class TextToImageRunDataService : ModelRunDataService, ITextToImageRunDataService
    {
        private const string TEXT_TO_IMAGE_RUN_HASH_KEY = "TEXT_TO_IMAGE";

        
        private readonly IServiceProvider _serviceProvider;

        public TextToImageRunDataService(IRedisDatabase redisDatabase,
            IServiceProvider serviceProvider) : base(redisDatabase)
        {
            _serviceProvider = serviceProvider;
        }

        private static Dictionary<string, SemaphoreSlim> textToImageRunsLocks = new Dictionary<string, SemaphoreSlim>();
        private static SemaphoreSlim textToImageRunDictionaryLock = new SemaphoreSlim(1, 1);

        private static Dictionary<string, SemaphoreSlim> userLocks = new Dictionary<string, SemaphoreSlim>();
        private static SemaphoreSlim userDictionaryLock = new SemaphoreSlim(1, 1);

        
        private static HashSet<string> statusProperties = new HashSet<string>(baseStatusProperties)
        {
            nameof(TextToImageRun.Prompt),
            nameof(TextToImageRun.NegativePrompt),
            nameof(TextToImageRun.ImageURL),
            nameof(TextToImageRun.Step)
        };

        public override ModelRunType ModelRunType => ModelRunType.TEXT_TO_IMAGE;

        private string GetTextToImageRunHashKey()
        {
            return $"{TEXT_TO_IMAGE_RUN_HASH_KEY}_{TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, "Asia/Bangkok".ToTimeZoneInfo()).Date.ToString("yyyy-MM-dd")}";
        }

        private async Task<SemaphoreSlim> GetLock(string refId)
        {
            try
            {
                await textToImageRunDictionaryLock.WaitAsync();
                if (!textToImageRunsLocks.ContainsKey($"T2I_{refId}"))
                {
                    textToImageRunsLocks.Add($"T2I_{refId}", new SemaphoreSlim(1, 1));
                }
                return textToImageRunsLocks[$"T2I_{refId}"];
            }
            finally
            {
                textToImageRunDictionaryLock.Release();
            }
        }

        public async Task<TextToImageRun> GetTextToImageRunByRefId(string refId)
        {
            throw new NotImplementedException();
        }

        public async Task<TextToImageRun> SaveTextToImageRun(TextToImageRun dto)
        {
            var textToImageRunLock = await GetLock(dto.Id);

            try
            {
                await textToImageRunLock.WaitAsync();
                var runData = await HashWrapGetAsync<TextToImageRun>(GetTextToImageRunHashKey(), dto.Id);
                
                if (runData != null)
                {
                    var existingRunData = runData;

                    bool updated = false;
                    #region Update 
                    if (!existingRunData.LastModificationTime.HasValue || !dto.LastModificationTime.HasValue || existingRunData.LastModificationTime <= dto.LastModificationTime)
                    {
                        existingRunData.LastModificationTime = dto.LastModificationTime;
                        existingRunData.RecentlyModifiedFields.ExceptWith(statusProperties);
                        foreach (var field in dto.RecentlyModifiedFields.Where(x => statusProperties.Contains(x)))
                        {
                            updated = true;
                            existingRunData.RecentlyModifiedFields.Add(field);
                            existingRunData.GetType().GetProperty(field).SetValue(existingRunData, dto.GetType().GetProperty(field).GetValue(dto));
                        }
                    }
                    else
                    {
                        var newlyModifiedFields = dto.RecentlyModifiedFields.Where(x => statusProperties.Contains(x) && !existingRunData.RecentlyModifiedFields.Contains(x));
                        foreach (var field in newlyModifiedFields)
                        {
                            updated = true;
                            existingRunData.RecentlyModifiedFields.Add(field);
                            existingRunData.GetType().GetProperty(field).SetValue(existingRunData, dto.GetType().GetProperty(field).GetValue(dto));
                        }
                    }

                    runData = existingRunData;
                    #endregion Update
                }
                else
                {
                    runData = dto;
                }

                await HashWrapSetAsync(GetTextToImageRunHashKey(), dto.Id, runData);

                return runData;
            }
            finally
            {
                textToImageRunLock.Release();
            }
        }

        
    }
}
