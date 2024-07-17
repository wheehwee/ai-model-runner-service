using Microsoft.ML.OnnxRuntime.Tensors;
using System.Numerics.Tensors;

namespace StableDiffusion.ML.OnnxRuntime.Helpers
{
    public static class TensorHelper
    {
        /// <summary>
        /// Sums the tensors.
        /// </summary>
        /// <param name="tensors">The tensor array.</param>
        /// <param name="dimensions">The dimensions.</param>
        /// <returns></returns>
        public static DenseTensor<float> SumTensors(this DenseTensor<float>[] tensors, ReadOnlySpan<int> dimensions)
        {
            var result = new DenseTensor<float>(dimensions);
            for (int m = 0; m < tensors.Length; m++)
            {
                TensorPrimitives.Add(result.Buffer.Span, tensors[m].Buffer.Span, result.Buffer.Span);
            }
            return result;
        }

        public static DenseTensor<T> CreateTensor<T>(T[] data, int[] dimensions)
        {
            return new DenseTensor<T>(data, dimensions); ;
        }
        // function to cover float to float16 tensor
        public static DenseTensor<Float16> ConvertFloatToFloat16(Tensor<float> tensor)
        {
            var float16Tensor = new DenseTensor<Float16>(tensor.Dimensions.ToArray());
            var float16Array = float16Tensor.ToArray();
            var floatArray = tensor.ToArray();

            for (int i = 0; i < floatArray.Length; i++)
            {
                Half halfValue = (Half)floatArray[i];
                byte[] bytes = BitConverter.GetBytes(halfValue);
                ushort ushortValue = BitConverter.ToUInt16(bytes);
                float16Array[i] = (Float16)ushortValue;
            }

            return CreateTensor<Float16>(float16Array, tensor.Dimensions.ToArray());
        }
        public static DenseTensor<float> DivideTensorByFloat(float[] data, float value, int[] dimensions)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (float)(data[i] / value);
            }

            return CreateTensor(data, dimensions);
        }

        public static DenseTensor<float> DivideTensorByFloat(this DenseTensor<float> tensor, float value)
        {
            return MultipleTensorByFloat(tensor, 1/value);
        }

        /// <summary>
        /// Generate a random Tensor from a normal distribution with mean 0 and variance 1
        /// </summary>
        /// <param name="random">The random.</param>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="initNoiseSigma">The initialize noise sigma.</param>
        /// <returns></returns>
        public static DenseTensor<float> NextTensor(this Random random, ReadOnlySpan<int> dimensions, float initNoiseSigma = 1f)
        {
            var latents = new DenseTensor<float>(dimensions);
            for (int i = 0; i < latents.Length; i++)
            {
                // Generate a random number from a normal distribution with mean 0 and variance 1
                var u1 = random.NextSingle(); // Uniform(0,1) random number
                var u2 = random.NextSingle(); // Uniform(0,1) random number
                var radius = MathF.Sqrt(-2.0f * MathF.Log(u1)); // Radius of polar coordinates
                var theta = 2.0f * MathF.PI * u2; // Angle of polar coordinates
                var standardNormalRand = radius * MathF.Cos(theta); // Standard normal random number
                latents.SetValue(i, standardNormalRand * initNoiseSigma);
            }
            return latents;
        }

        /// <summary>
        /// Adds the tensors.
        /// </summary>
        /// <param name="tensor">The sample.</param>
        /// <param name="sumTensor">The sum tensor.</param>
        /// <returns></returns>
        public static DenseTensor<float> AddTensors(this DenseTensor<float> tensor, DenseTensor<float> sumTensor)
        {
            var result = new DenseTensor<float>(tensor.Dimensions);
            TensorPrimitives.Add(tensor.Buffer.Span, sumTensor.Buffer.Span, result.Buffer.Span);
            return result;
        }

        /// <summary>
        /// Multiplies the tensor by float.
        /// </summary>
        /// <param name="tensor">The data.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static DenseTensor<float> MultiplyTensorByFloat(this DenseTensor<float> tensor, float value)
        {
            var result = new DenseTensor<float>(tensor.Dimensions);
            TensorPrimitives.Multiply(tensor.Buffer.Span, value, result.Buffer.Span);
            return result;
        }

        public static DenseTensor<float> MultipleTensorByFloat(DenseTensor<float> tensor, float value)
        {
            var result = new DenseTensor<float>(tensor.Dimensions);
            TensorPrimitives.Multiply(tensor.Buffer.Span, value, result.Buffer.Span);
            return result;
        }

        public static DenseTensor<float> MultipleTensorByFloat(float[] data, float value, int[] dimensions)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (float)(data[i] * value);
            }

            return CreateTensor(data, dimensions);
        }

        public static DenseTensor<float> AddTensors(float[] sample, float[] sumTensor, int[] dimensions)
        {
            for (var i = 0; i < sample.Length; i++)
            {
                sample[i] = (float)(sample[i] + sumTensor[i]);
            }
            return CreateTensor(sample, dimensions); ;
        }

        public static DenseTensor<float> AddTensors(Tensor<float> sample, Tensor<float> sumTensor)
        {
            return AddTensors(sample.ToArray(), sumTensor.ToArray(), sample.Dimensions.ToArray());
        }

        public static Tuple<Tensor<float>, Tensor<float>> SplitTensor(Tensor<Float16> tensorToSplit, int[] dimensions)
        {
            var tensor1 = new DenseTensor<float>(dimensions);
            var tensor2 = new DenseTensor<float>(dimensions);

            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 512 / 8; k++)
                    {
                        for (int l = 0; l < 512 / 8; l++)
                        {
                            tensor1[i, j, k, l] = (float)BitConverter.UInt16BitsToHalf(tensorToSplit[i, j, k, l]);
                            tensor2[i, j, k, l] = (float)BitConverter.UInt16BitsToHalf(tensorToSplit[i, j + 4, k, l]);
                        }
                    }
                }
            }
            return new Tuple<Tensor<float>, Tensor<float>>(tensor1, tensor2);

        }

        public static DenseTensor<float> SumTensors(Tensor<float>[] tensorArray, int[] dimensions)
        {
            var sumTensor = new DenseTensor<float>(dimensions);
            var sumArray = new float[sumTensor.Length];

            for (int m = 0; m < tensorArray.Count(); m++)
            {
                var tensorToSum = tensorArray[m].ToArray();
                for (var i = 0; i < tensorToSum.Length; i++)
                {
                    sumArray[i] += (float)tensorToSum[i];
                }
            }

            return CreateTensor(sumArray, dimensions);
        }

        public static DenseTensor<float> Duplicate(float[] data, int[] dimensions)
        {
            data = data.Concat(data).ToArray();
            return CreateTensor(data, dimensions);
        }

        public static DenseTensor<float> Duplicate(DenseTensor<float> tensor1, int count)
        {
            var dimensions = tensor1.Dimensions.ToArray();
            dimensions[0] *= count;

            var length = (int)tensor1.Length;
            var totalLength = length * count;
            var buffer = new float[totalLength].AsMemory();
            for (int i = 0; i < count; i++)
            {
                tensor1.Buffer.CopyTo(buffer[(i * length)..]);
            }
            return new DenseTensor<float>(buffer, dimensions);
        }

        public static DenseTensor<float> SubtractTensors(float[] sample, float[] subTensor, int[] dimensions)
        {
            for (var i = 0; i < sample.Length; i++)
            {
                sample[i] = (float)(sample[i] - subTensor[i]);
            }
            return CreateTensor(sample, dimensions);
        }

        public static DenseTensor<float> SubtractTensors(Tensor<float> sample, Tensor<float> subTensor)
        {
            return SubtractTensors(sample.ToArray(), subTensor.ToArray(), sample.Dimensions.ToArray());
        }

        public static Tensor<float> GetRandomTensor(ReadOnlySpan<int> dimensions)
        {
            var random = new Random();
            var latents = new DenseTensor<float>(dimensions);
            var latentsArray = latents.ToArray();

            for (int i = 0; i < latentsArray.Length; i++)
            {
                // Generate a random number from a normal distribution with mean 0 and variance 1
                var u1 = random.NextDouble(); // Uniform(0,1) random number
                var u2 = random.NextDouble(); // Uniform(0,1) random number
                var radius = Math.Sqrt(-2.0 * Math.Log(u1)); // Radius of polar coordinates
                var theta = 2.0 * Math.PI * u2; // Angle of polar coordinates
                var standardNormalRand = radius * Math.Cos(theta); // Standard normal random number
                latentsArray[i] = (float)standardNormalRand;
            }

            latents = TensorHelper.CreateTensor(latentsArray, latents.Dimensions.ToArray());

            return latents;

        }

        public static DenseTensor<float> ConvertFloat16ToFloat(DenseTensor<Float16>? result)
        {
            var floatTensor = new DenseTensor<float>(result.Dimensions.ToArray());
            var floatArray = floatTensor.ToArray();
            var float16Array = result.ToArray();

            for (int i = 0; i < floatArray.Length; i++)
            {
                byte[] bytes = BitConverter.GetBytes(float16Array[i]);
                Half halfValue = System.Buffers.Binary.BinaryPrimitives.ReadHalfLittleEndian(bytes);
                floatArray[i] = (float)halfValue;
            }

            return CreateTensor<float>(floatArray, result.Dimensions.ToArray()); ;
        }

        /// <summary>
        /// Divides the tensor by float, mutates the original
        /// </summary>
        /// <param name="tensor">The tensor to mutate.</param>
        /// <param name="value">The value to divide by.</param>
        /// <returns></returns>
        public static DenseTensor<float> DivideBy(this DenseTensor<float> tensor, float value)
        {
            value = 1 / value;
            TensorPrimitives.Multiply(tensor.Buffer.Span, value, tensor.Buffer.Span);
            return tensor;
        }


        /// <summary>
        /// Multiples the tensor by float, mutates the original
        /// </summary>
        /// <param name="tensor">The tensor to mutate.</param>
        /// <param name="value">The value to multiply by.</param>
        /// <returns></returns>
        public static DenseTensor<float> MultiplyBy(this DenseTensor<float> tensor, float value) => DivideBy(tensor, 1 / value);


        /// <summary>
        /// Computes the absolute values of the Tensor
        /// </summary>
        /// <param name="tensor">The tensor to mutate.</param>
        /// <returns></returns>
        public static DenseTensor<float> Abs(this DenseTensor<float> tensor)
        {
            TensorPrimitives.Abs(tensor.Buffer.Span, tensor.Buffer.Span);
            return tensor;
        }


        /// <summary>
        /// Multiplies the specified tensor.
        /// </summary>
        /// <param name="tensor1">The tensor to mutate.</param>
        /// <param name="mulTensor">The tensor to multiply by.</param>
        /// <returns></returns>
        public static DenseTensor<float> Multiply(this DenseTensor<float> tensor, DenseTensor<float> mulTensor)
        {
            TensorPrimitives.Multiply(tensor.Buffer.Span, mulTensor.Buffer.Span, tensor.Buffer.Span);
            return tensor;
        }


        /// <summary>
        /// Divides the specified tensor.
        /// </summary>
        /// <param name="tensor">The tensor to mutate.</param>
        /// <param name="divTensor">The tensor to divide by.</param>
        /// <returns></returns>
        public static DenseTensor<float> Divide(this DenseTensor<float> tensor, DenseTensor<float> divTensor)
        {
            TensorPrimitives.Divide(tensor.Buffer.Span, divTensor.Buffer.Span, tensor.Buffer.Span);
            return tensor;
        }


        /// <summary>
        /// Adds the tensors, mutates the original
        /// </summary>
        /// <param name="tensor">The tensor to mutate.</param>
        /// <param name="addTensor">The tensor values to add to tensor.</param>
        /// <returns></returns>
        public static DenseTensor<float> Add(this DenseTensor<float> tensor, DenseTensor<float> addTensor)
        {
            TensorPrimitives.Add(tensor.Buffer.Span, addTensor.Buffer.Span, tensor.Buffer.Span);
            return tensor;
        }


        /// <summary>
        /// Subtracts the tensors, mutates the original
        /// </summary>
        /// <param name="tensor">The tensor to mutate.</param>
        /// <param name="subTensor">The tensor to subtract from tensor.</param>
        /// <param name="dimensions">The dimensions.</param>
        /// <returns></returns>
        public static DenseTensor<float> Subtract(this DenseTensor<float> tensor, DenseTensor<float> subTensor)
        {
            TensorPrimitives.Subtract(tensor.Buffer.Span, subTensor.Buffer.Span, tensor.Buffer.Span);
            return tensor;
        }


        /// <summary>
        /// Subtracts the tensors.
        /// </summary>
        /// <param name="tensor">The sample.</param>
        /// <param name="subTensor">The sub tensor.</param>
        /// <returns></returns>
        public static DenseTensor<float> SubtractTensors(this DenseTensor<float> tensor, DenseTensor<float> subTensor)
        {
            var result = new DenseTensor<float>(tensor.Dimensions);
            TensorPrimitives.Subtract(tensor.Buffer.Span, subTensor.Buffer.Span, result.Buffer.Span);
            return result;
        }
    }
}
