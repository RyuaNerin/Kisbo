using System.IO;
using System.Threading;

namespace Kisbo.Utilities
{
    public static class Base64Stream
    {
        // ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=
        private static byte[] Base64Table = {
            0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a,
            0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 0x50, 0x51, 0x52, 0x53, 0x54,
            0x55, 0x56, 0x57, 0x58, 0x59, 0x5a,
            0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a,
            0x6b, 0x6c, 0x6d, 0x6e, 0x6f, 0x70, 0x71, 0x72, 0x73, 0x74,
            0x75, 0x76, 0x77, 0x78, 0x79, 0x7a,
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
            0x2d, 0x5f, 0x3d };
        public static void WriteTo(Stream from, Stream to, CancellationToken token)
        {
            var buff = new byte[4096];
            int read = -1;
            int i = 0;
            int b;

            long pos = to.Position;

            while (!token.IsCancellationRequested)
            {
                if (i >= read) if (!WriteByBase64_Read(buff, from, ref i, ref read, token)) return;
                if (i >= read) break;

                b = (buff[i] & 0xFC) >> 2;
                to.WriteByte(Base64Table[b]);
                b = (buff[i] & 0x03) << 4;

                if (i + 1 >= read) if (!WriteByBase64_Read(buff, from, ref i, ref read, token)) return;
                if (i + 1 < read)
                {
                    b |= (buff[i + 1] & 0xF0) >> 4;
                    to.WriteByte(Base64Table[b]);

                    b = (buff[i + 1] & 0x0F) << 2;

                    if (i + 2 >= read) if (!WriteByBase64_Read(buff, from, ref i, ref read, token)) return;
                    if (i + 2 < read)
                    {
                        b |= (buff[i + 2] & 0xC0) >> 6;
                        to.WriteByte(Base64Table[b]);
                        b = buff[i + 2] & 0x3F;
                        to.WriteByte(Base64Table[b]);
                    }
                    else
                    {
                        to.WriteByte(Base64Table[b]);
                        to.WriteByte(Base64Table[64]);
                    }
                }
                else
                {
                    to.WriteByte(Base64Table[b]);
                    to.WriteByte(Base64Table[64]);
                    to.WriteByte(Base64Table[64]);
                }

                i += 3;
            }

            pos = to.Position - pos;

            while (pos++ % 4 != 0)
                to.WriteByte(Base64Table[64]);
        }
        private static bool WriteByBase64_Read(byte[] buff, Stream from, ref int i, ref int read, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return false;

            int index = 0;
            int k;
            for (k = i; k < read; ++k) buff[index++] = buff[k];

            var nr = from.Read(buff, index, buff.Length - index);
            if (nr > 0)
            {
                read = nr + index;
                i = 0;
            }

            return true;
        }
    }
}
