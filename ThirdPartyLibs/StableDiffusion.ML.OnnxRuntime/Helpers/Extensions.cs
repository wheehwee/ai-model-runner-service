using System;
using System.Collections.Generic;
using System.Text;

namespace StableDiffusion.ML.OnnxRuntime.Helpers
{
    public static class Extensions
    {
        /// <summary>
        /// Get the index of the specified item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public static int IndexOf<T>(this IReadOnlyList<T> list, T item) where T : IEquatable<T>
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(item))
                    return i;
            }
            return -1;
        }
    }
}
