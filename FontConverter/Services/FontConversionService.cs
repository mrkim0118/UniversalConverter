using System.IO;
using System.IO.Compression;
using System.Text;

namespace UniversalConverter.Services;

/// <summary>
/// Converts between TTF, OTF, WOFF, WOFF2, and EOT font formats.
/// WOFF2 reading supports only non-transformed glyf tables (transform version 3).
/// </summary>
public static class FontConversionService
{
    private const uint WOFF_SIG  = 0x774F4646u; // 'wOFF'
    private const uint WOFF2_SIG = 0x774F4632u; // 'wOF2'

    // SFNT version signatures
    private static readonly uint[] SfntSigs =
        [0x00010000u, 0x4F54544Fu, 0x74727565u, 0x74797031u];

    // WOFF2 known tag table (indices 0–61; 63 = arbitrary tag follows)
    private static readonly uint[] W2KnownTags =
    [
        0x636d6170u, 0x68656164u, 0x68686561u, 0x686d7478u, 0x6d617870u, // 0  cmap head hhea hmtx maxp
        0x6e616d65u, 0x4f532f32u, 0x706f7374u, 0x63767420u, 0x6670676du, // 5  name OS/2 post cvt  fpgm
        0x676c7966u, 0x6c6f6361u, 0x70726570u, 0x43464620u, 0x564f5246u, // 10 glyf loca prep CFF  VORF
        0x45424454u, 0x45424c43u, 0x67617370u, 0x68646d78u, 0x6b65726eu, // 15 EBDT EBLC gasp hdmx kern
        0x4c544348u, 0x50434c54u, 0x56444d58u, 0x76686561u, 0x766d7478u, // 20 LTSH PCLT VDMX vhea vmtx
        0x42415345u, 0x47444546u, 0x47504f53u, 0x47535542u, 0x45425343u, // 25 BASE GDEF GPOS GSUB EBSC
        0x4a535446u, 0x4d415448u, 0x43424454u, 0x43424c43u, 0x434f4c52u, // 30 JSTF MATH CBDT CBLC COLR
        0x4350414cu, 0x53564720u, 0x73626978u, 0x61636e74u, 0x61766172u, // 35 CPAL SVG  sbix acnt avar
        0x62646174u, 0x626c6f63u, 0x62736c6eu, 0x63766172u, 0x66656174u, // 40 bdat bloc bsln cvar feat
        0x666d7478u, 0x66766172u, 0x67766172u, 0x68737479u, 0x6a757374u, // 45 fmtx fvar gvar hsty just
        0x6c636172u, 0x6d6f7274u, 0x6d6f7278u, 0x6f706264u, 0x70726f70u, // 50 lcar mort morx opbd prop
        0x7472616bu, 0x5a617066u, 0x53696c66u, 0x476c6174u, 0x476c6f63u, // 55 trak Zapf Silf Glat Gloc
        0x46656174u, 0x53696c6cu,                                          // 60 Feat Sill
    ];

    private const int GLYF_IDX = 10;
    private const int LOCA_IDX = 11;

    // ═══════════════════════════════════════════════
    //  Public entry point
    // ═══════════════════════════════════════════════
    public static void Convert(string inputPath, string outputPath, string fromFmt, string toFmt)
    {
        byte[] sfnt = fromFmt.ToUpper() switch
        {
            "TTF" or "OTF" => File.ReadAllBytes(inputPath),
            "WOFF"         => WoffToSfnt(File.ReadAllBytes(inputPath)),
            "WOFF2"        => Woff2ToSfnt(File.ReadAllBytes(inputPath)),
            "EOT"          => EotToSfnt(File.ReadAllBytes(inputPath)),
            _ => throw new NotSupportedException($"입력 형식 미지원: {fromFmt}")
        };

        byte[] output = toFmt.ToUpper() switch
        {
            "TTF" or "OTF" => sfnt,
            "WOFF"         => SfntToWoff(sfnt),
            "WOFF2"        => SfntToWoff2(sfnt),
            "EOT"          => SfntToEot(sfnt),
            _ => throw new NotSupportedException($"출력 형식 미지원: {toFmt}")
        };

        File.WriteAllBytes(outputPath, output);
    }

    // ═══════════════════════════════════════════════
    //  SFNT record
    // ═══════════════════════════════════════════════
    private sealed record SfntData(uint Flavor, List<uint> Tags, List<uint> Checksums, List<byte[]> Tables);

    private static SfntData ParseSfnt(byte[] data)
    {
        using var ms = new MemoryStream(data);
        uint flavor   = BeR32(ms);
        int  nTables  = (int)BeR16(ms);
        BeR16(ms); BeR16(ms); BeR16(ms); // searchRange, entrySelector, rangeShift

        var tags   = new List<uint>(nTables);
        var csums  = new List<uint>(nTables);
        var offs   = new List<uint>(nTables);
        var lens   = new List<uint>(nTables);

        for (int i = 0; i < nTables; i++)
        {
            tags.Add(BeR32(ms));
            csums.Add(BeR32(ms));
            offs.Add(BeR32(ms));
            lens.Add(BeR32(ms));
        }

        var tables = new List<byte[]>(nTables);
        for (int i = 0; i < nTables; i++)
        {
            var buf = new byte[lens[i]];
            Buffer.BlockCopy(data, (int)offs[i], buf, 0, (int)lens[i]);
            tables.Add(buf);
        }
        return new SfntData(flavor, tags, csums, tables);
    }

    private static byte[] BuildSfnt(SfntData d)
    {
        int n = d.Tags.Count;
        int sr = 1, es = 0;
        while (sr * 2 <= n) { sr *= 2; es++; }
        sr *= 16;

        int hdr = 12 + n * 16;
        var offsets = new int[n];
        int pos = hdr;
        for (int i = 0; i < n; i++)
        {
            offsets[i] = pos;
            pos += (d.Tables[i].Length + 3) & ~3;
        }

        using var ms = new MemoryStream(pos);
        BeW32(ms, d.Flavor);
        BeW16(ms, (ushort)n);
        BeW16(ms, (ushort)sr);
        BeW16(ms, (ushort)es);
        BeW16(ms, (ushort)(n * 16 - sr));

        for (int i = 0; i < n; i++)
        {
            BeW32(ms, d.Tags[i]);
            BeW32(ms, d.Checksums[i]);
            BeW32(ms, (uint)offsets[i]);
            BeW32(ms, (uint)d.Tables[i].Length);
        }
        for (int i = 0; i < n; i++)
        {
            ms.Write(d.Tables[i]);
            int pad = ((d.Tables[i].Length + 3) & ~3) - d.Tables[i].Length;
            for (int p = 0; p < pad; p++) ms.WriteByte(0);
        }
        return ms.ToArray();
    }

    // ═══════════════════════════════════════════════
    //  WOFF ↔ SFNT
    // ═══════════════════════════════════════════════
    private static byte[] WoffToSfnt(byte[] woff)
    {
        using var ms = new MemoryStream(woff);
        if (BeR32(ms) != WOFF_SIG) throw new InvalidDataException("WOFF 서명 오류");
        uint   flavor   = BeR32(ms);
        BeR32(ms);               // length
        int    nTables  = (int)BeR16(ms);
        BeR16(ms);               // reserved
        BeR32(ms);               // totalSfntSize
        BeR16(ms); BeR16(ms);    // version
        BeR32(ms); BeR32(ms); BeR32(ms); // meta
        BeR32(ms); BeR32(ms);    // priv

        var tags  = new uint[nTables];
        var coffs = new uint[nTables];
        var clens = new uint[nTables];
        var olens = new uint[nTables];
        var csums = new uint[nTables];
        for (int i = 0; i < nTables; i++)
        {
            tags[i]  = BeR32(ms);
            coffs[i] = BeR32(ms);
            clens[i] = BeR32(ms);
            olens[i] = BeR32(ms);
            csums[i] = BeR32(ms);
        }

        var tables = new List<byte[]>(nTables);
        for (int i = 0; i < nTables; i++)
        {
            var comp = new byte[clens[i]];
            Buffer.BlockCopy(woff, (int)coffs[i], comp, 0, (int)clens[i]);

            byte[] raw;
            if (clens[i] == olens[i])
            {
                raw = comp; // stored uncompressed
            }
            else
            {
                // zlib: skip 2-byte header, deflate stream
                using var ds  = new DeflateStream(new MemoryStream(comp, 2, comp.Length - 2), CompressionMode.Decompress);
                using var buf = new MemoryStream();
                ds.CopyTo(buf);
                raw = buf.ToArray();
            }
            tables.Add(raw);
        }
        return BuildSfnt(new SfntData(flavor, [.. tags], [.. csums], tables));
    }

    private static byte[] SfntToWoff(byte[] sfnt)
    {
        var d = ParseSfnt(sfnt);
        int n = d.Tags.Count;

        var comp = new byte[n][];
        for (int i = 0; i < n; i++)
        {
            var z = ZlibDeflate(d.Tables[i]);
            comp[i] = z.Length < d.Tables[i].Length ? z : d.Tables[i];
        }

        int hdr = 44 + n * 20;
        var offsets = new int[n];
        int pos = hdr;
        for (int i = 0; i < n; i++)
        {
            offsets[i] = pos;
            pos += (comp[i].Length + 3) & ~3;
        }

        using var ms = new MemoryStream();
        BeW32(ms, WOFF_SIG);
        BeW32(ms, d.Flavor);
        BeW32(ms, (uint)pos);
        BeW16(ms, (ushort)n);
        BeW16(ms, 0);
        BeW32(ms, (uint)sfnt.Length);
        BeW16(ms, 1); BeW16(ms, 0);     // version
        BeW32(ms, 0); BeW32(ms, 0); BeW32(ms, 0); // meta
        BeW32(ms, 0); BeW32(ms, 0);     // priv

        for (int i = 0; i < n; i++)
        {
            BeW32(ms, d.Tags[i]);
            BeW32(ms, (uint)offsets[i]);
            BeW32(ms, (uint)comp[i].Length);
            BeW32(ms, (uint)d.Tables[i].Length);
            BeW32(ms, d.Checksums[i]);
        }
        for (int i = 0; i < n; i++)
        {
            ms.Write(comp[i]);
            int pad = ((comp[i].Length + 3) & ~3) - comp[i].Length;
            for (int p = 0; p < pad; p++) ms.WriteByte(0);
        }
        return ms.ToArray();
    }

    // ═══════════════════════════════════════════════
    //  WOFF2 ↔ SFNT
    // ═══════════════════════════════════════════════
    private static byte[] Woff2ToSfnt(byte[] woff2)
    {
        using var ms = new MemoryStream(woff2);
        if (BeR32(ms) != WOFF2_SIG) throw new InvalidDataException("WOFF2 서명 오류");
        uint   flavor    = BeR32(ms);
        BeR32(ms);                   // length
        int    nTables   = (int)BeR16(ms);
        BeR16(ms);                   // reserved
        BeR32(ms);                   // totalSfntSize
        uint   compLen   = BeR32(ms);
        BeR16(ms); BeR16(ms);        // version
        BeR32(ms); BeR32(ms); BeR32(ms); // meta
        BeR32(ms); BeR32(ms);        // priv

        var entries = new List<(uint tag, int tagIdx, int xver, uint origLen, uint transLen, bool hasTrans)>();
        for (int i = 0; i < nTables; i++)
        {
            int flags  = ms.ReadByte();
            int tagIdx = flags & 0x3F;
            int xver   = (flags >> 6) & 0x03;

            uint tag = tagIdx == 63
                ? BeR32(ms)
                : (tagIdx < W2KnownTags.Length ? W2KnownTags[tagIdx] : 0u);

            uint origLen = ReadBase128(ms);

            bool hasTrans = false;
            uint transLen = 0;
            // transformLength present when glyf/loca has xver==0 (default transform applied)
            if ((tagIdx == GLYF_IDX || tagIdx == LOCA_IDX) && xver == 0)
            {
                // glyf transform is applied — need reverse transform (complex)
                // transLen field is only on glyf, not loca
                if (tagIdx == GLYF_IDX)
                    transLen = ReadBase128(ms);
                hasTrans = true;
            }
            entries.Add((tag, tagIdx, xver, origLen, transLen, hasTrans));
        }

        // Check for glyf transform (not currently supported for reconstruction)
        bool hasGlyfTransform = entries.Any(e => e.tagIdx == GLYF_IDX && e.xver == 0);
        if (hasGlyfTransform)
            throw new NotSupportedException(
                "이 WOFF2 파일에는 glyf 변환이 적용되어 있어 직접 변환이 불가합니다.\n" +
                "다른 온라인 도구(convertio.co 등)를 이용해 변환 후 사용하세요.");

        // Brotli decompress
        var compData = new byte[compLen];
        ms.ReadExactly(compData, 0, (int)compLen);
        byte[] uncomp;
        using (var brotliStream = new BrotliStream(new MemoryStream(compData), CompressionMode.Decompress))
        using (var outBuf = new MemoryStream())
        {
            brotliStream.CopyTo(outBuf);
            uncomp = outBuf.ToArray();
        }

        // Extract tables (no inter-table padding in WOFF2 compressed block)
        var tags   = new List<uint>();
        var csums  = new List<uint>();
        var tables = new List<byte[]>();
        int offset = 0;
        foreach (var (tag, _, _, origLen, transLen, hasTrans) in entries)
        {
            int len = hasTrans ? (int)transLen : (int)origLen;
            len = Math.Min(len, uncomp.Length - offset);
            var buf = uncomp[offset..(offset + len)];
            offset += len;
            tags.Add(tag);
            csums.Add(CalcChecksum(buf));
            tables.Add(buf);
        }
        return BuildSfnt(new SfntData(flavor, tags, csums, tables));
    }

    private static byte[] SfntToWoff2(byte[] sfnt)
    {
        var d = ParseSfnt(sfnt);
        int n = d.Tags.Count;

        // Concatenate tables without padding for Brotli compression
        using var fontBuf = new MemoryStream();
        var origLens = new uint[n];
        for (int i = 0; i < n; i++)
        {
            origLens[i] = (uint)d.Tables[i].Length;
            fontBuf.Write(d.Tables[i]);
            // No padding between tables in WOFF2 compressed block
        }
        byte[] uncomp = fontBuf.ToArray();

        byte[] comp;
        using (var compBuf = new MemoryStream())
        {
            using (var brotli = new BrotliStream(compBuf, CompressionLevel.Optimal))
                brotli.Write(uncomp);
            comp = compBuf.ToArray();
        }

        // Build table directory
        using var dirBuf = new MemoryStream();
        for (int i = 0; i < n; i++)
        {
            int tagIdx = Array.IndexOf(W2KnownTags, d.Tags[i]);
            bool isGlyfLoca = tagIdx == GLYF_IDX || tagIdx == LOCA_IDX;

            if (tagIdx < 0 || tagIdx > 61)
            {
                // Unknown tag: index 63
                dirBuf.WriteByte(63); // no transform (bits 6-7 = 0)
                BeW32(dirBuf, d.Tags[i]);
            }
            else if (isGlyfLoca)
            {
                // Transform version 3 = no transform (bits 6-7 = 0b11 = 0xC0)
                dirBuf.WriteByte((byte)(0xC0 | tagIdx));
            }
            else
            {
                dirBuf.WriteByte((byte)tagIdx); // transform version 0 = no transform for non-glyf tables
            }
            WriteBase128(dirBuf, origLens[i]);
            // No transformLength field (no transforms applied)
        }
        byte[] dir = dirBuf.ToArray();

        uint totalLen = (uint)(48 + dir.Length + comp.Length);
        using var outBuf = new MemoryStream();
        BeW32(outBuf, WOFF2_SIG);
        BeW32(outBuf, d.Flavor);
        BeW32(outBuf, totalLen);
        BeW16(outBuf, (ushort)n);
        BeW16(outBuf, 0);
        BeW32(outBuf, (uint)sfnt.Length);  // totalSfntSize
        BeW32(outBuf, (uint)comp.Length);  // totalCompressedSize
        BeW16(outBuf, 1); BeW16(outBuf, 0); // version
        BeW32(outBuf, 0); BeW32(outBuf, 0); BeW32(outBuf, 0); // meta
        BeW32(outBuf, 0); BeW32(outBuf, 0); // priv
        outBuf.Write(dir);
        outBuf.Write(comp);
        return outBuf.ToArray();
    }

    // ═══════════════════════════════════════════════
    //  EOT ↔ SFNT  (simplified)
    // ═══════════════════════════════════════════════
    private static byte[] EotToSfnt(byte[] eot)
    {
        // Scan for an SFNT signature with a plausible numTables
        for (int i = 0; i <= eot.Length - 6; i++)
        {
            uint v = (uint)(eot[i] << 24 | eot[i + 1] << 16 | eot[i + 2] << 8 | eot[i + 3]);
            if (Array.IndexOf(SfntSigs, v) >= 0)
            {
                int nt = (eot[i + 4] << 8) | eot[i + 5];
                if (nt is > 0 and < 64)
                    return eot[i..];
            }
        }
        throw new InvalidDataException("EOT 파일에서 폰트 데이터를 찾을 수 없습니다.");
    }

    private static byte[] SfntToEot(byte[] sfnt)
    {
        string family = ReadFamilyName(sfnt);
        byte[] fName = Encoding.Unicode.GetBytes(family);
        byte[] sName = Encoding.Unicode.GetBytes("Regular");
        byte[] ver   = Encoding.Unicode.GetBytes("Version 1.00");

        using var ms = new MemoryStream();
        // Fixed part: 68 bytes
        // Variable: padding(2)+len(2)+data for FamilyName, StyleName, VersionString, FullName
        int varSize = (2 + 2 + fName.Length) * 2  // FamilyName + FullName (same)
                    + (2 + 2 + sName.Length)        // StyleName
                    + (2 + 2 + ver.Length);          // VersionString
        uint eotSize = (uint)(68 + varSize + sfnt.Length);

        LeW32(ms, eotSize);
        LeW32(ms, (uint)sfnt.Length);
        LeW32(ms, 0x00020001u); // version
        LeW32(ms, 0);           // flags
        ms.Write(new byte[10]); // PANOSE
        ms.WriteByte(0);        // Charset
        ms.WriteByte(0);        // Italic
        LeW32(ms, 400);         // Weight
        LeW16(ms, 0);           // fsType
        LeW16(ms, (ushort)0x504cu); // MagicNumber
        ms.Write(new byte[16]); // UnicodeRange[4]
        ms.Write(new byte[8]);  // CodePageRange[2]
        LeW32(ms, 0);           // CheckSumAdjustment
        ms.Write(new byte[16]); // Reserved[4]

        LeW16(ms, 0); LeW16(ms, (ushort)fName.Length); ms.Write(fName); // FamilyName
        LeW16(ms, 0); LeW16(ms, (ushort)sName.Length); ms.Write(sName); // StyleName
        LeW16(ms, 0); LeW16(ms, (ushort)ver.Length);   ms.Write(ver);   // VersionString
        LeW16(ms, 0); LeW16(ms, (ushort)fName.Length); ms.Write(fName); // FullName
        ms.Write(sfnt);
        return ms.ToArray();
    }

    // ═══════════════════════════════════════════════
    //  Font name extraction
    // ═══════════════════════════════════════════════
    private static string ReadFamilyName(byte[] sfnt)
    {
        try
        {
            var d = ParseSfnt(sfnt);
            int idx = d.Tags.IndexOf(0x6e616d65u); // 'name'
            if (idx < 0) return "Unknown";
            var nt = d.Tables[idx];
            using var ms = new MemoryStream(nt);
            BeR16(ms); // format
            int    count  = (int)BeR16(ms);
            ushort strOff = BeR16(ms);
            for (int i = 0; i < count; i++)
            {
                ushort platId = BeR16(ms);
                ushort encId  = BeR16(ms);
                BeR16(ms); // langId
                ushort nameId = BeR16(ms);
                ushort len    = BeR16(ms);
                ushort off    = BeR16(ms);
                if (nameId == 1 && platId == 3 && encId == 1 && strOff + off + len <= nt.Length)
                    return Encoding.BigEndianUnicode.GetString(nt, strOff + off, len);
            }
        }
        catch { /* fall through */ }
        return "Unknown Font";
    }

    // ═══════════════════════════════════════════════
    //  Utility helpers
    // ═══════════════════════════════════════════════
    private static byte[] ZlibDeflate(byte[] data)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0x78); ms.WriteByte(0x9C); // CMF + FLG (deflate, default compression)
        using (var def = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            def.Write(data);
        uint a = Adler32(data);
        ms.WriteByte((byte)(a >> 24)); ms.WriteByte((byte)(a >> 16));
        ms.WriteByte((byte)(a >> 8));  ms.WriteByte((byte)a);
        return ms.ToArray();
    }

    private static uint Adler32(byte[] data)
    {
        uint s1 = 1, s2 = 0;
        foreach (byte b in data) { s1 = (s1 + b) % 65521; s2 = (s2 + s1) % 65521; }
        return (s2 << 16) | s1;
    }

    private static uint CalcChecksum(byte[] data)
    {
        uint sum = 0;
        for (int i = 0; i < data.Length; i += 4)
            sum += (uint)(
                ((i)     < data.Length ? data[i]     : 0u) << 24 |
                ((i + 1) < data.Length ? data[i + 1] : 0u) << 16 |
                ((i + 2) < data.Length ? data[i + 2] : 0u) << 8  |
                ((i + 3) < data.Length ? data[i + 3] : 0u));
        return sum;
    }

    private static uint ReadBase128(Stream s)
    {
        uint result = 0;
        for (int i = 0; i < 5; i++)
        {
            int b = s.ReadByte();
            if (b < 0) throw new EndOfStreamException();
            if (i == 0 && b == 0x80) throw new InvalidDataException("Base128 leading zero");
            result = (result << 7) | (uint)(b & 0x7F);
            if ((b & 0x80) == 0) return result;
        }
        throw new InvalidDataException("Base128 overflow");
    }

    private static void WriteBase128(Stream s, uint value)
    {
        if (value == 0) { s.WriteByte(0); return; }
        int len = 0; uint tmp = value;
        while (tmp > 0) { tmp >>= 7; len++; }
        for (int i = len - 1; i >= 0; i--)
        {
            byte b = (byte)((value >> (7 * i)) & 0x7F);
            if (i > 0) b |= 0x80;
            s.WriteByte(b);
        }
    }

    // ── Big-endian read helpers ──────────────────
    private static uint   BeR32(Stream s) { Span<byte> b = stackalloc byte[4]; s.ReadExactly(b); return (uint)(b[0]<<24|b[1]<<16|b[2]<<8|b[3]); }
    private static ushort BeR16(Stream s) { Span<byte> b = stackalloc byte[2]; s.ReadExactly(b); return (ushort)(b[0]<<8|b[1]); }

    // ── Big-endian write helpers ─────────────────
    private static void BeW32(Stream s, uint v)   { s.WriteByte((byte)(v>>24)); s.WriteByte((byte)(v>>16)); s.WriteByte((byte)(v>>8)); s.WriteByte((byte)v); }
    private static void BeW16(Stream s, ushort v) { s.WriteByte((byte)(v>>8)); s.WriteByte((byte)v); }

    // ── Little-endian write helpers (EOT) ───────
    private static void LeW32(Stream s, uint v)   { s.WriteByte((byte)v); s.WriteByte((byte)(v>>8)); s.WriteByte((byte)(v>>16)); s.WriteByte((byte)(v>>24)); }
    private static void LeW16(Stream s, ushort v) { s.WriteByte((byte)v); s.WriteByte((byte)(v>>8)); }
}
