﻿//
// Copyright (c) 2017, Quamotion
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

using DiscUtils.Nfs;
using System;
using System.IO;
using Xunit;

namespace LibraryTests.Nfs;

public class Nfs3FileSystemStatResultTest
{
    [Fact]
    public void RoundTripTest()
    {
        var result = new Nfs3FileSystemStatResult()
        {
            FileSystemStat = new Nfs3FileSystemStat()
            {
                AvailableFreeFileSlotCount = 1,
                AvailableFreeSpaceBytes = 2,
                FileSlotCount = 3,
                FreeFileSlotCount = 4,
                FreeSpaceBytes = 5,
                Invariant = TimeSpan.FromSeconds(7),
                TotalSizeBytes = 8
            },
            PostOpAttributes = new Nfs3FileAttributes()
            {
                AccessTime = new Nfs3FileTime(new DateTime(2017, 1, 1)),
                ChangeTime = new Nfs3FileTime(new DateTime(2017, 1, 2)),
                ModifyTime = new Nfs3FileTime(new DateTime(2017, 1, 3))
            }
        };

        Nfs3FileSystemStatResult clone = null;

        using (var stream = new MemoryStream())
        {
            var writer = new XdrDataWriter(stream);
            result.Write(writer);

            stream.Position = 0;
            var reader = new XdrDataReader(stream);
            clone = new Nfs3FileSystemStatResult(reader);
        }

        Assert.Equal(result, clone);
    }
}
