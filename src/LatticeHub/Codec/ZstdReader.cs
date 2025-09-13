using System;
using System.IO;
using System.IO.Compression;

namespace Lattice.Hub.Codec
{
    public static class StreamCodec
    {
        public static byte[] Inflate(byte[] payload, int expectedLength)
        {
            using (var src = new MemoryStream(payload))
            using (var gz = new GZipStream(src, CompressionMode.Decompress))
            using (var dst = new MemoryStream(expectedLength))
            {
                gz.CopyTo(dst);
                return dst.ToArray();
            }
        }

        public static byte[] Deflate(byte[] raw)
        {
            using (var dst = new MemoryStream())
            {
                using (var gz = new GZipStream(dst, CompressionLevel.Fastest, leaveOpen: true))
                {
                    gz.Write(raw, 0, raw.Length);
                }
                return dst.ToArray();
            }
        }
    }
}
