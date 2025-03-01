//
// Copyright (c) 2016, Bianco Veigel
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


using DiscUtils.Streams;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DiscUtils.Xfs;
internal class ExtentStream : BuiltStream
{
    /// <inheritdoc />
    public ExtentStream(long length, List<BuilderExtent> extents) 
        : base(length, extents)
    {
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (Position + count > Length)
        {
            count = (int)(Length - Position);
        }

        return base.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (Position + count > Length)
        {
            count = (int)(Length - Position);
        }

        return base.ReadAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (Position + buffer.Length > Length)
        {
            buffer = buffer.Slice(0, (int)(Length - Position));
        }

        return base.ReadAsync(buffer, cancellationToken);
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        if (Position + buffer.Length > Length)
        {
            buffer = buffer.Slice(0, (int)(Length - Position));
        }

        return base.Read(buffer);
    }
}
