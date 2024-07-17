namespace Application.ImageGeneration.Interfaces
{
    public interface ITextToImageGenerator
    {
        public Task<string> GenerateImage(string prompt, string negativePrompt = null);
    }
}
