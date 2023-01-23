namespace HaselTweaks.Structs;

// ctor "E8 ?? ?? ?? ?? BF ?? ?? ?? ?? 48 8D AB"
[StructLayout(LayoutKind.Explicit, Size = 0xB0)]
public unsafe partial struct TabSwitcher
{
    [FieldOffset(0x80)] public int CurrentTabIndex;
    [FieldOffset(0x84)] public int NumTabs;

    [FieldOffset(0x90)] public IntPtr CallbackPtr;
    [FieldOffset(0x98)] public IntPtr Addon;

    [FieldOffset(0xA8)] public bool Enabled;

    public delegate IntPtr CallbackDelegate(int tabIndex, IntPtr addon);
}