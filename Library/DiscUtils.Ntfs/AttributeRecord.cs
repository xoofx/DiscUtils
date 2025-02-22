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
using System.IO;
using System.Text;
using DiscUtils.Streams;
using DiscUtils.Streams.Compatibility;

namespace DiscUtils.Ntfs;

internal abstract class AttributeRecord : IComparable<AttributeRecord>
{
    protected ushort _attributeId;
    protected AttributeFlags _flags;

    protected string _name;
    protected byte _nonResidentFlag;
    protected AttributeType _type;

    public AttributeRecord() {}

    public AttributeRecord(AttributeType type, string name, ushort id, AttributeFlags flags)
    {
        _type = type;
        _name = name;
        _attributeId = id;
        _flags = flags;
    }

    public abstract long AllocatedLength { get; set; }

    public ushort AttributeId
    {
        get => _attributeId;
        set => _attributeId = value;
    }

    public AttributeType AttributeType => _type;

    public abstract long DataLength { get; set; }

    public AttributeFlags Flags
    {
        get => _flags;
        set => _flags = value;
    }

    public abstract long InitializedDataLength { get; set; }

    public bool IsNonResident => _nonResidentFlag != 0;

    public string Name => _name;

    public abstract int Size { get; }

    public abstract long StartVcn { get; }

    public int CompareTo(AttributeRecord other)
    {
        var val = (int)_type - (int)other._type;
        if (val != 0)
        {
            return val;
        }

        val = string.Compare(_name, other._name, StringComparison.OrdinalIgnoreCase);
        if (val != 0)
        {
            return val;
        }

        return _attributeId - other._attributeId;
    }

    public static AttributeRecord FromBytes(ReadOnlySpan<byte> buffer, out int length)
    {
        if (EndianUtilities.ToUInt32LittleEndian(buffer) == 0xFFFFFFFF)
        {
            length = 0;
            return null;
        }

        if (buffer[0x08] != 0x00)
        {
            return new NonResidentAttributeRecord(buffer, out length);
        }

        return new ResidentAttributeRecord(buffer, out length);
    }

    public static int CompareStartVcns(AttributeRecord x, AttributeRecord y)
    {
        if (x.StartVcn < y.StartVcn)
        {
            return -1;
        }

        if (x.StartVcn == y.StartVcn)
        {
            return 0;
        }

        return 1;
    }

    public abstract IEnumerable<Range<long, long>> GetClusters();

    public abstract CookedDataRuns GetCookedDataRuns();

    public abstract IBuffer GetReadOnlyDataBuffer(INtfsContext context);

    public abstract int Write(Span<byte> buffer);

    public virtual void Dump(TextWriter writer, string indent)
    {
        writer.WriteLine($"{indent}ATTRIBUTE RECORD");
        writer.WriteLine($"{indent}            Type: {_type}");
        writer.WriteLine($"{indent}    Non-Resident: {_nonResidentFlag}");
        writer.WriteLine($"{indent}            Name: {_name}");
        writer.WriteLine($"{indent}           Flags: {_flags}");
        writer.WriteLine($"{indent}     AttributeId: {_attributeId}");
    }

    protected virtual void Read(ReadOnlySpan<byte> buffer, out int length)
    {
        _type = (AttributeType)EndianUtilities.ToUInt32LittleEndian(buffer.Slice(0x00));
        length = EndianUtilities.ToInt32LittleEndian(buffer.Slice(0x04));

        _nonResidentFlag = buffer[0x08];
        var nameLength = buffer[0x09];
        var nameOffset = EndianUtilities.ToUInt16LittleEndian(buffer.Slice(0x0A));
        _flags = (AttributeFlags)EndianUtilities.ToUInt16LittleEndian(buffer.Slice(0x0C));
        _attributeId = EndianUtilities.ToUInt16LittleEndian(buffer.Slice(0x0E));

        if (nameLength != 0x00)
        {
            if (nameLength + nameOffset > length)
            {
                throw new IOException("Corrupt attribute, name outside of attribute");
            }

            _name = Encoding.Unicode.GetString(buffer.Slice(nameOffset, nameLength * 2));
        }
    }
}