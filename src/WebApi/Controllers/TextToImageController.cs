using Application.ImageGeneration.Interfaces;
using Application.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class TextToImageController : ControllerBase
    {
        private readonly ITextToImageGenerator _textToImageGenerator;

        public TextToImageController(ITextToImageGenerator textToImageGenerator)
        {
            _textToImageGenerator = textToImageGenerator;
        }

        [HttpPost("generate")]
        //[Authorize(AppConstants.PolicyWrite)]
        public async Task<IActionResult> GenerateImageFromText([FromQuery] string prompt, [FromQuery] string? negativePrompt)
        {
            //var uid = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

            try
            {
                var result = await _textToImageGenerator.GenerateImage(prompt, negativePrompt);
                return Ok(new BaseApiResponse<string, string>
                {
                    Data = result,
                    Message = "Successfully created orders",
                    ErrorData = string.Empty,
                });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
