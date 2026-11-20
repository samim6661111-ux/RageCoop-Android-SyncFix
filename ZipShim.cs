// Drop-in for the small part of ICSharpCode.SharpZipLib.Zip that RageCoop.Client uses (ZipFile, ZipEntry,
// GetInputStream, enumeration). Pure managed: its own zip central-directory reader and RFC 1951 inflater, because
// System.IO.Compression's DeflateStream needs a native zlib wrapper (libMonoPosixHelper) that the Android APK
// does not ship. Supports stored + deflated entries (what every zip tool produces), no zip64, no encryption.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ICSharpCode.SharpZipLib.Zip
{
    public class ZipEntry
    {
        public string Name;
        public bool IsDirectory;
        internal long LocalHeaderOffset;
        internal long CompressedSize;
        internal long Size;
        internal int Method;
    }

    public class ZipFile : IEnumerable, IDisposable
    {
        private readonly FileStream _fs;
        private readonly List<ZipEntry> _entries = new List<ZipEntry>();
        public string Name { get; private set; }

        public ZipFile(string path)
        {
            Name = path;
            _fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            ReadCentralDirectory();
        }

        private static uint U32(byte[] b, int o) { return (uint)(b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24)); }
        private static int U16(byte[] b, int o) { return b[o] | (b[o + 1] << 8); }

        private void ReadCentralDirectory()
        {
            long len = _fs.Length;
            int tail = (int)Math.Min(len, 65557);
            byte[] buf = new byte[tail];
            _fs.Seek(len - tail, SeekOrigin.Begin);
            ReadFully(_fs, buf, tail);
            int eocd = -1;
            for (int i = tail - 22; i >= 0; i--)
            {
                if (buf[i] == 0x50 && buf[i + 1] == 0x4b && buf[i + 2] == 5 && buf[i + 3] == 6) { eocd = i; break; }
            }
            if (eocd < 0) throw new InvalidDataException("not a zip file: " + Name);
            int count = U16(buf, eocd + 10);
            uint cdSize = U32(buf, eocd + 12);
            uint cdOffset = U32(buf, eocd + 16);
            if (cdOffset == 0xFFFFFFFF || count == 0xFFFF) throw new NotSupportedException("zip64 archives are not supported");
            byte[] cd = new byte[cdSize];
            _fs.Seek(cdOffset, SeekOrigin.Begin);
            ReadFully(_fs, cd, (int)cdSize);
            int p = 0;
            for (int n = 0; n < count; n++)
            {
                if (p + 46 > cd.Length || U32(cd, p) != 0x02014b50) throw new InvalidDataException("corrupt zip central directory");
                int flags = U16(cd, p + 8);
                int method = U16(cd, p + 10);
                uint csize = U32(cd, p + 20);
                uint usize = U32(cd, p + 24);
                int nameLen = U16(cd, p + 28);
                int extraLen = U16(cd, p + 30);
                int commentLen = U16(cd, p + 32);
                uint lho = U32(cd, p + 42);
                if ((flags & 1) != 0) throw new NotSupportedException("encrypted zip entries are not supported");
                string name = Encoding.UTF8.GetString(cd, p + 46, nameLen).Replace('\\', '/');
                _entries.Add(new ZipEntry
                {
                    Name = name,
                    IsDirectory = name.EndsWith("/"),
                    LocalHeaderOffset = lho,
                    CompressedSize = csize,
                    Size = usize,
                    Method = method
                });
                p += 46 + nameLen + extraLen + commentLen;
            }
        }

        private static void ReadFully(Stream s, byte[] buf, int count)
        {
            int off = 0;
            while (off < count)
            {
                int r = s.Read(buf, off, count - off);
                if (r <= 0) throw new EndOfStreamException();
                off += r;
            }
        }

        public Stream GetInputStream(ZipEntry entry)
        {
            byte[] hdr = new byte[30];
            _fs.Seek(entry.LocalHeaderOffset, SeekOrigin.Begin);
            ReadFully(_fs, hdr, 30);
            if (U32(hdr, 0) != 0x04034b50) throw new InvalidDataException("corrupt zip local header for " + entry.Name);
            long dataStart = entry.LocalHeaderOffset + 30 + U16(hdr, 26) + U16(hdr, 28);
            byte[] comp = new byte[entry.CompressedSize];
            _fs.Seek(dataStart, SeekOrigin.Begin);
            ReadFully(_fs, comp, comp.Length);
            if (entry.Method == 0) return new MemoryStream(comp, false);
            if (entry.Method == 8) return new MemoryStream(Inflater.Inflate(comp, (int)entry.Size), false);
            throw new NotSupportedException("zip compression method " + entry.Method + " is not supported (" + entry.Name + ")");
        }

        public IEnumerator GetEnumerator() { return _entries.GetEnumerator(); }
        public void Close() { Dispose(); }
        public void Dispose() { _fs.Dispose(); }
    }

    // RFC 1951 inflate (port of the structure of zlib's puff.c)
    internal sealed class Inflater
    {
        private sealed class Huffman { public short[] Count = new short[16]; public short[] Symbol; }

        private static readonly short[] LenBase = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258 };
        private static readonly short[] LenExtra = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };
        private static readonly short[] DistBase = { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577 };
        private static readonly short[] DistExtra = { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };
        private static readonly byte[] ClOrder = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

        private readonly byte[] _in;
        private int _inPos;
        private int _bitBuf;
        private int _bitCnt;
        private byte[] _out;
        private int _outPos;

        private Inflater(byte[] input, int expected)
        {
            _in = input;
            _out = new byte[Math.Max(expected, 16)];
        }

        public static byte[] Inflate(byte[] input, int expectedSize)
        {
            Inflater inf = new Inflater(input, expectedSize);
            inf.Run();
            if (inf._outPos == inf._out.Length) return inf._out;
            byte[] r = new byte[inf._outPos];
            Array.Copy(inf._out, r, inf._outPos);
            return r;
        }

        private int Bits(int need)
        {
            int val = _bitBuf;
            while (_bitCnt < need)
            {
                if (_inPos >= _in.Length) throw new InvalidDataException("deflate stream truncated");
                val |= _in[_inPos++] << _bitCnt;
                _bitCnt += 8;
            }
            _bitBuf = val >> need;
            _bitCnt -= need;
            return val & ((1 << need) - 1);
        }

        private void Put(int b)
        {
            if (_outPos == _out.Length) Array.Resize(ref _out, _out.Length * 2);
            _out[_outPos++] = (byte)b;
        }

        private void Run()
        {
            int last;
            do
            {
                last = Bits(1);
                int type = Bits(2);
                if (type == 0) Stored();
                else if (type == 1) Codes(FixedLen, FixedDist);
                else if (type == 2) Dynamic();
                else throw new InvalidDataException("invalid deflate block type");
            } while (last == 0);
        }

        private void Stored()
        {
            _bitBuf = 0; _bitCnt = 0;
            if (_inPos + 4 > _in.Length) throw new InvalidDataException("deflate stream truncated");
            int len = _in[_inPos] | (_in[_inPos + 1] << 8);
            int nlen = _in[_inPos + 2] | (_in[_inPos + 3] << 8);
            if (len != (~nlen & 0xFFFF)) throw new InvalidDataException("deflate stored block length mismatch");
            _inPos += 4;
            if (_inPos + len > _in.Length) throw new InvalidDataException("deflate stream truncated");
            for (int i = 0; i < len; i++) Put(_in[_inPos++]);
        }

        private static int Construct(Huffman h, short[] length, int offset, int n)
        {
            for (int len = 0; len <= 15; len++) h.Count[len] = 0;
            for (int s = 0; s < n; s++) h.Count[length[offset + s]]++;
            h.Symbol = new short[n];
            if (h.Count[0] == n) return 0;
            int left = 1;
            for (int len = 1; len <= 15; len++)
            {
                left <<= 1;
                left -= h.Count[len];
                if (left < 0) return left;
            }
            short[] offs = new short[16];
            for (int len = 1; len < 15; len++) offs[len + 1] = (short)(offs[len] + h.Count[len]);
            for (int s = 0; s < n; s++)
                if (length[offset + s] != 0) h.Symbol[offs[length[offset + s]]++] = (short)s;
            return left;
        }

        private int Decode(Huffman h)
        {
            int code = 0, first = 0, index = 0;
            for (int len = 1; len <= 15; len++)
            {
                code |= Bits(1);
                int count = h.Count[len];
                if (code - count < first) return h.Symbol[index + (code - first)];
                index += count;
                first += count;
                first <<= 1;
                code <<= 1;
            }
            throw new InvalidDataException("invalid huffman code");
        }

        private void Codes(Huffman lencode, Huffman distcode)
        {
            while (true)
            {
                int symbol = Decode(lencode);
                if (symbol < 256) { Put(symbol); continue; }
                if (symbol == 256) return;
                symbol -= 257;
                if (symbol >= 29) throw new InvalidDataException("invalid length symbol");
                int len = LenBase[symbol] + Bits(LenExtra[symbol]);
                int ds = Decode(distcode);
                if (ds >= 30) throw new InvalidDataException("invalid distance symbol");
                int dist = DistBase[ds] + Bits(DistExtra[ds]);
                if (dist > _outPos) throw new InvalidDataException("distance too far back");
                for (int i = 0; i < len; i++) Put(_out[_outPos - dist]);
            }
        }

        private static readonly Huffman FixedLen = BuildFixed(true);
        private static readonly Huffman FixedDist = BuildFixed(false);

        private static Huffman BuildFixed(bool lit)
        {
            Huffman h = new Huffman();
            if (lit)
            {
                short[] l = new short[288];
                int s;
                for (s = 0; s < 144; s++) l[s] = 8;
                for (; s < 256; s++) l[s] = 9;
                for (; s < 280; s++) l[s] = 7;
                for (; s < 288; s++) l[s] = 8;
                Construct(h, l, 0, 288);
            }
            else
            {
                short[] l = new short[30];
                for (int s = 0; s < 30; s++) l[s] = 5;
                Construct(h, l, 0, 30);
            }
            return h;
        }

        private void Dynamic()
        {
            int nlen = Bits(5) + 257;
            int ndist = Bits(5) + 1;
            int ncode = Bits(4) + 4;
            if (nlen > 286 || ndist > 30) throw new InvalidDataException("bad dynamic block counts");
            short[] lengths = new short[320];
            for (int i = 0; i < ncode; i++) lengths[ClOrder[i]] = (short)Bits(3);
            Huffman lencode = new Huffman();
            if (Construct(lencode, lengths, 0, 19) != 0) throw new InvalidDataException("bad code length code");
            short[] ll = new short[320];
            int index = 0;
            while (index < nlen + ndist)
            {
                int symbol = Decode(lencode);
                if (symbol < 16) { ll[index++] = (short)symbol; continue; }
                int len = 0, rep;
                if (symbol == 16)
                {
                    if (index == 0) throw new InvalidDataException("repeat with no previous length");
                    len = ll[index - 1];
                    rep = 3 + Bits(2);
                }
                else if (symbol == 17) rep = 3 + Bits(3);
                else rep = 11 + Bits(7);
                if (index + rep > nlen + ndist) throw new InvalidDataException("too many code lengths");
                while (rep-- > 0) ll[index++] = (short)len;
            }
            if (ll[256] == 0) throw new InvalidDataException("missing end-of-block code");
            Huffman lit = new Huffman();
            Huffman dist = new Huffman();
            int err = Construct(lit, ll, 0, nlen);
            if (err < 0 || (err > 0 && nlen - lit.Count[0] != 1)) throw new InvalidDataException("bad literal/length code");
            err = Construct(dist, ll, nlen, ndist);
            if (err < 0 || (err > 0 && ndist - dist.Count[0] != 1)) throw new InvalidDataException("bad distance code");
            Codes(lit, dist);
        }
    }
}
