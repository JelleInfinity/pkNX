using System;
using System.ComponentModel;
using FlatSharp.Attributes;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace pkNX.Structures.FlatBuffers;

[FlatBufferTable, TypeConverter(typeof(ExpandableObjectConverter))]
public class PlacementZone8MovementPathHolder
{
    [FlatBufferItem(00)] public PlacementZoneMetaTripleXYZ8 Field_00 { get; set; } = new();
    [FlatBufferItem(01)] public ulong PathName { get; set; }
    [FlatBufferItem(02)] public uint Field_02 { get; set; }
    [FlatBufferItem(03)] public uint Field_03 { get; set; }
    [FlatBufferItem(04)] public bool Field_04 { get; set; }
    [FlatBufferItem(05)] public PlacementZone8_V3f[] Field_05 { get; set; } = Array.Empty<PlacementZone8_V3f>();
}

[FlatBufferTable, TypeConverter(typeof(ExpandableObjectConverter))]
public class PlacementZone8_V3f
{
    [FlatBufferItem(00)] public float LocationX { get; set; }
    [FlatBufferItem(01)] public float LocationY { get; set; }
    [FlatBufferItem(02)] public float LocationZ { get; set; }

    public string Location3f => $"({LocationX}, {LocationY}, {LocationZ})";

    public override string ToString() => Location3f;
}
