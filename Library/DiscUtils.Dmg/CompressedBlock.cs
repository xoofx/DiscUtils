//
// Copyright (c) 2008-2011, Kenneth Bell
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using DiscUtils.Streams;

namespace DiscUtils.Dmg;

internal class CompressedBlock : IByteArraySerializable
{
    public uint BlocksDescriptor;
    public UdifChecksum CheckSum;
    public ulong DataStart;
    public uint DecompressBufferRequested;
    public long FirstSector;
    public uint InfoVersion;
    public List<CompressedRun> Runs;
    public long SectorCount;
    public uint Signature;

    public int Size => throw new NotImplementedException();

    public int ReadFrom(ReadOnlySpan<byte> buffer)
    {
        Signature = EndianUtilities.ToUInt32BigEndian(buffer);
        InfoVersion = EndianUtilities.ToUInt32BigEndian(buffer.Slice(4));
        FirstSector = EndianUtilities.ToInt64BigEndian(buffer.Slice(8));
        SectorCount = EndianUtilities.ToInt64BigEndian(buffer.Slice(16));
        DataStart = EndianUtilities.ToUInt64BigEndian(buffer.Slice(24));
        DecompressBufferRequested = EndianUtilities.ToUInt32BigEndian(buffer.Slice(32));
        BlocksDescriptor = EndianUtilities.ToUInt32BigEndian(buffer.Slice(36));

        CheckSum = EndianUtilities.ToStruct<UdifChecksum>(buffer.Slice(60));

        Runs = [];
        var numRuns = EndianUtilities.ToInt32BigEndian(buffer.Slice(200));
        for (var i = 0; i < numRuns; ++i)
        {
            Runs.Add(EndianUtilities.ToStruct<CompressedRun>(buffer.Slice(204 + i * 40)));
        }

        return 0;
    }

    void IByteArraySerializable.WriteTo(Span<byte> buffer)
    {
        throw new NotImplementedException();
    }
}