using System;

namespace CoreFx.Extensions
{
    public static class CollectionExtensions
    {
        public static void BlockCopy<T>(this T[] src, int srcOffset, T[] dst, int dstOffset, int length)
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            if (dst == null)
            {
                throw new ArgumentNullException(nameof(dst));
            }
            
            src.AsSpan(srcOffset, length).CopyTo(dst.AsSpan(dstOffset, length));
        }

        public static void BlockCopy<T>(this Span<T> src, int srcOffset, Span<T> dst, int dstOffset, int length)
        {
            if ((uint)src.Length < (uint)(srcOffset + length))
            {
                throw new ArgumentException("Source span is to small");
            }

            if ((uint)dst.Length < (uint)(dstOffset + length))
            {
                throw new ArgumentException("Destination span is to small");
            }

            src.Slice(srcOffset, length).CopyTo(dst[dstOffset..]);
        }
    }
}