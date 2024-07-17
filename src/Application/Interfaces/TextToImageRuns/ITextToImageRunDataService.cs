using Domain.Entities;

namespace Application.Interfaces.TextToImageRuns
{
    public interface ITextToImageRunDataService : IModelRunDataService
    {
        public Task<TextToImageRun> GetTextToImageRunByRefId(string refId);

        public Task<TextToImageRun> SaveTextToImageRun(TextToImageRun dto);
    }
}
