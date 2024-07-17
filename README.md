# Introduction 
Local WebApi to run stable diffusion models on your local machine with your local model (download your favorite stable diffusion model from huggingface) 

# Getting Started
1.	Install redis either through cmd or Docker and ensure it runs on port 6379 locally
2.	Download your models and put it in "src/Application/ImageGeneration/Models/..."
3.  Test around with inputs/configs like Width Height GuidanceScale InteferenceSteps in TextToImageGenerator.cs (default is 512, 512, 12, 50)
4.	Run

# Build and Test
Ensure build config is x64 (not Any CPU) and then run in Debug or Release with Visual Studio 

# TODO
1. Write service to upload generated file to image cloud and save URL to redis for ease of querying
2. Write Api to Get TextToImage run details by generated Id from "generate Text to Image" API => return image cloud URL above