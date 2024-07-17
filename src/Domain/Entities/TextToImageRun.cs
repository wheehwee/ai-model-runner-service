using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class TextToImageRun : ModelRun
    {
        private ModelRunType _type = ModelRunType.TEXT_TO_IMAGE;

        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public int? Step { get; set; }
        public string? ImageURL { get; set; }
        public override ModelRunType ModelRunType { get => _type; set => _type = value; }
    }
}
