﻿using FlatSharp.Attributes;

// ReSharper disable UnusedMember.Global
#pragma warning disable RCS1154 // Sort enum members.

namespace pkNX.Structures.FlatBuffers;

[FlatBufferEnum(typeof(ulong))]
public enum ThrowParamType8a : ulong
{
	Wing = 0x6b9627ecf52625da, //wing
	SnowCalm = 0xa086f61847441d29, //snowcalm
	Default=0xafc4d6c05abef19e, //default
	VolcanoCalm = 0xc82a786ba9e1c8ae, //volcanocalm
	Stealth = 0xe9bdc448876b25dc, //stealth
	None = 0xCBF29CE484222645, // ""
}
