﻿//
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiscUtils.Streams;
using DiscUtils.Streams.Compatibility;
using LTRData.Extensions.Buffers;
using Buffer=DiscUtils.Streams.Buffer;

namespace DiscUtils.HfsPlus;

internal sealed class FileBuffer : Buffer
{
    private readonly ForkData _baseData;
    private readonly CatalogNodeId _cnid;
    private readonly Context _context;

    public FileBuffer(Context context, ForkData baseData, CatalogNodeId catalogNodeId)
    {
        _context = context;
        _baseData = baseData;
        _cnid = catalogNodeId;
    }

    public override bool CanRead => true;

    public override bool CanWrite => false;

    public override long Capacity => (long)_baseData.LogicalSize;

    public IEnumerable<StreamExtent> EnumerateAllocationExtents()
    {
        var pos = 0;

        var totalRead = 0;

        var limitedCount = Capacity;

        while (totalRead < limitedCount)
        {
            var extent = FindExtent(pos, out var extentLogicalStart);
            var extentStreamStart = extent.StartBlock * (long)_context.VolumeHeader.BlockSize;
            var extentSize = extent.BlockCount * (long)_context.VolumeHeader.BlockSize;

            var extentOffset = pos + totalRead - extentLogicalStart;
            var toRead = (int)Math.Min(limitedCount - totalRead, extentSize - extentOffset);

            // Remaining in extent can create a situation where amount to read is zero, and that appears
            // to be OK, just need to exit thie while loop to avoid infinite loop.
            if (toRead == 0)
            {
                break;
            }

            yield return new(extentStreamStart + extentOffset, toRead);

            totalRead += toRead;
        }
    }

    public override int Read(long pos, byte[] buffer, int offset, int count)
    {
        var totalRead = 0;

        var limitedCount = (int)Math.Min(count, Math.Max(0, Capacity - pos));

        while (totalRead < limitedCount)
        {
            var extent = FindExtent(pos, out var extentLogicalStart);
            var extentStreamStart = extent.StartBlock * (long)_context.VolumeHeader.BlockSize;
            var extentSize = extent.BlockCount * (long)_context.VolumeHeader.BlockSize;

            var extentOffset = pos + totalRead - extentLogicalStart;
            var toRead = (int)Math.Min(limitedCount - totalRead, extentSize - extentOffset);

            // Remaining in extent can create a situation where amount to read is zero, and that appears
            // to be OK, just need to exit thie while loop to avoid infinite loop.
            if (toRead == 0)
            {
                break;
            }

            var volStream = _context.VolumeStream;
            volStream.Position = extentStreamStart + extentOffset;
            var numRead = volStream.Read(buffer, offset + totalRead, toRead);

            totalRead += numRead;
        }

        return totalRead;
    }

    public override async ValueTask<int> ReadAsync(long pos, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;

        var limitedCount = (int)Math.Min(buffer.Length, Math.Max(0, Capacity - pos));

        while (totalRead < limitedCount)
        {
            var extent = FindExtent(pos, out var extentLogicalStart);
            var extentStreamStart = extent.StartBlock * (long)_context.VolumeHeader.BlockSize;
            var extentSize = extent.BlockCount * (long)_context.VolumeHeader.BlockSize;

            var extentOffset = pos + totalRead - extentLogicalStart;
            var toRead = (int)Math.Min(limitedCount - totalRead, extentSize - extentOffset);

            // Remaining in extent can create a situation where amount to read is zero, and that appears
            // to be OK, just need to exit thie while loop to avoid infinite loop.
            if (toRead == 0)
            {
                break;
            }

            var volStream = _context.VolumeStream;
            volStream.Position = extentStreamStart + extentOffset;
            var numRead = await volStream.ReadAsync(buffer.Slice(totalRead, toRead), cancellationToken).ConfigureAwait(false);

            totalRead += numRead;
        }

        return totalRead;
    }

    public override int Read(long pos, Span<byte> buffer)
    {
        var totalRead = 0;

        var limitedCount = (int)Math.Min(buffer.Length, Math.Max(0, Capacity - pos));

        while (totalRead < limitedCount)
        {
            var extent = FindExtent(pos, out var extentLogicalStart);
            var extentStreamStart = extent.StartBlock * (long)_context.VolumeHeader.BlockSize;
            var extentSize = extent.BlockCount * (long)_context.VolumeHeader.BlockSize;

            var extentOffset = pos + totalRead - extentLogicalStart;
            var toRead = (int)Math.Min(limitedCount - totalRead, extentSize - extentOffset);

            // Remaining in extent can create a situation where amount to read is zero, and that appears
            // to be OK, just need to exit thie while loop to avoid infinite loop.
            if (toRead == 0)
            {
                break;
            }

            var volStream = _context.VolumeStream;
            volStream.Position = extentStreamStart + extentOffset;
            var numRead = volStream.Read(buffer.Slice(totalRead, toRead));

            totalRead += numRead;
        }

        return totalRead;
    }

    public override void Write(long pos, byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override void Write(long pos, ReadOnlySpan<byte> buffer) =>
        throw new NotSupportedException();

    public override void SetCapacity(long value)
    {
        throw new NotSupportedException();
    }

    public override IEnumerable<StreamExtent> GetExtentsInRange(long start, long count)
        => SingleValueEnumerable.Get(new StreamExtent(start, Math.Min(start + count, Capacity) - start));

    private ExtentDescriptor FindExtent(long pos, out long extentLogicalStart)
    {
        uint blocksSeen = 0;
        var block = (uint)(pos / _context.VolumeHeader.BlockSize);
        for (var i = 0; i < _baseData.Extents.Length; ++i)
        {
            if (blocksSeen + _baseData.Extents[i].BlockCount > block)
            {
                extentLogicalStart = blocksSeen * (long)_context.VolumeHeader.BlockSize;
                return _baseData.Extents[i];
            }

            blocksSeen += _baseData.Extents[i].BlockCount;
        }

        while (blocksSeen < _baseData.TotalBlocks)
        {
            var extentData = _context.ExtentsOverflow.Find(new ExtentKey(_cnid, blocksSeen, false));

            if (extentData != null)
            {
                var extentDescriptorCount = extentData.Length / 8;
                for (var a = 0; a < extentDescriptorCount; a++)
                {
                    var extentDescriptor = new ExtentDescriptor();
                    var bytesRead = extentDescriptor.ReadFrom(extentData, a * 8);

                    if (blocksSeen + extentDescriptor.BlockCount > block)
                    {
                        extentLogicalStart = blocksSeen * (long)_context.VolumeHeader.BlockSize;
                        return extentDescriptor;
                    }

                    blocksSeen += extentDescriptor.BlockCount;
                }
            }
            else
            {
                throw new IOException($"Missing extent from extent overflow file: cnid={_cnid}, blocksSeen={blocksSeen}");
            }
        }

        throw new InvalidOperationException("Requested file fragment beyond EOF");
    }
}