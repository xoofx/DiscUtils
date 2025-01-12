//
// Copyright (c) 2008-2011, Kenneth Bell
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


using System;
using System.IO;
using DiscUtils.Vfs;
using DiscUtils.Internal;
using DiscUtils.CoreCompat;

namespace DiscUtils.Xfs;
internal class DirEntry : VfsDirEntry
{
    private readonly IDirectoryEntry _entry;
    private readonly Context _context;
    private string _name;

    internal Directory CachedDirectory { get; set; }

    private DirEntry(Context context)
    {
        _context = context;
    }

    public DirEntry(IDirectoryEntry entry, Context context):this(context)
    {
        _entry = entry;
        _name = _context.Options.FileNameEncoding.GetString(_entry.Name).SanitizeFileName();
        Inode = _context.GetInode(_entry.Inode);
    }
    public Inode Inode { get; private set; }

    public override bool IsDirectory => Inode.FileType == UnixFileType.Directory;

    public override bool IsSymlink => Inode.FileType == UnixFileType.Link;

    public override string FileName => _name;

    public override bool HasVfsTimeInfo => true;

    public override DateTime LastAccessTimeUtc => Inode.AccessTime;

    public override DateTime LastWriteTimeUtc => Inode.ModificationTime;

    public override DateTime CreationTimeUtc => Inode.CreationTime;

    public override bool HasVfsFileAttributes => true;

    public override FileAttributes FileAttributes
    {
        get
        {
            return Utilities.FileAttributesFromUnixFileType(Inode.FileType);
            ;
        }
    }

    public override long UniqueCacheId => ((long)Inode.AllocationGroup) << 32 | Inode.RelativeInodeNumber;
}
