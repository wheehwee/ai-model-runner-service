using Application.Interfaces.TextToImageRuns;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Events
{
    public class TextToImageRunDataEvent
    {
        public Func<IServiceProvider, Task> Task { get; set; }
    }
}
