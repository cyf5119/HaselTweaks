using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.STD;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x228)]
public unsafe partial struct AtkComponentTreeList
{
    [FieldOffset(0)] public FFXIVClientStructs.FFXIV.Component.GUI.AtkComponentTreeList Base;

    [FieldOffset(0x1A8)] public StdVector<Pointer<AtkComponentTreeListItem>> Items;

    [MemberFunction("E8 ?? ?? ?? ?? 44 38 60 45")]
    public readonly partial AtkComponentTreeListItem* GetItem(uint index);
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct AtkComponentTreeListItem
{
    [FieldOffset(0)] public AtkComponentTreeListItemData* Data;

    [FieldOffset(0x18)] public byte** Title;

    [FieldOffset(0x30)] public AtkComponentListItemRenderer* Renderer;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct AtkComponentTreeListItemData
{
    [FieldOffset(0)] public AtkComponentTreeListItemType Type;
    // seems to be addon-specific data from here on
}

public enum AtkComponentTreeListItemType : uint
{
    Leaf = 0,
    LastLeafInGroup = 1,
    CollapsibleGroupHeader = 2, // see AddonMJICraftScheduleSetting
    GroupHeader = 4, // always expanded, see AddonTelepotTown
}
