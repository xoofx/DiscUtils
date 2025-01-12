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
using DiscUtils.Streams;
using DiscUtils.Streams.Compatibility;

namespace DiscUtils.Vhd;

internal class ParentLocator
{
    public const string PlatformCodeWindowsRelativeUnicode = "W2ru";
    public const string PlatformCodeWindowsAbsoluteUnicode = "W2ku";

    public string PlatformCode;
    public int PlatformDataLength;
    public long PlatformDataOffset;
    public int PlatformDataSpace;

    public ParentLocator()
    {
        PlatformCode = string.Empty;
    }

    public ParentLocator(ParentLocator toCopy)
    {
        PlatformCode = toCopy.PlatformCode;
        PlatformDataSpace = toCopy.PlatformDataSpace;
        PlatformDataLength = toCopy.PlatformDataLength;
        PlatformDataOffset = toCopy.PlatformDataOffset;
    }

    public static ParentLocator FromBytes(ReadOnlySpan<byte> data)
    {
        var latin1Encoding = EncodingUtilities.GetLatin1Encoding();

        return new ParentLocator
        {
            PlatformCode = latin1Encoding.GetString(data.Slice(0, 4)),
            PlatformDataSpace = EndianUtilities.ToInt32BigEndian(data.Slice(4)),
            PlatformDataLength = EndianUtilities.ToInt32BigEndian(data.Slice(8)),
            PlatformDataOffset = EndianUtilities.ToInt64BigEndian(data.Slice(16))
        };
    }

    internal void ToBytes(Span<byte> data)
    {
        var latin1Encoding = EncodingUtilities.GetLatin1Encoding();

        latin1Encoding.GetBytes(PlatformCode.AsSpan(), data.Slice(0, 4));
        EndianUtilities.WriteBytesBigEndian(PlatformDataSpace, data.Slice(4));
        EndianUtilities.WriteBytesBigEndian(PlatformDataLength, data.Slice(8));
        EndianUtilities.WriteBytesBigEndian((uint)0, data.Slice(12));
        EndianUtilities.WriteBytesBigEndian(PlatformDataOffset, data.Slice(16));
    }
}