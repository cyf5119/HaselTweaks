using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.GeneratedSheets;
using GameFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace HaselTweaks.Tweaks;

public unsafe class DTR : Tweak
{
    public override string Name => "DTR";
    public override string Description => "Shows Instance, FPS and Busy status in DTR bar. To enable/disable/change order go into Dalamud Settings.";

    [Signature("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 3C 01 75 0C 48 8D 0D ?? ?? ?? ?? E8", ScanType = ScanType.StaticAddress)]
    private IntPtr InstanceIdAddress { get; init; }

    public DtrBarEntry DtrInstance = null!;
    public DtrBarEntry DtrFPS = null!;
    public DtrBarEntry DtrBusy = null!;

    public override void Setup()
    {
        DtrInstance = Service.DtrBar.Get("[HaselTweaks] Instance");
        DtrFPS = Service.DtrBar.Get("[HaselTweaks] FPS");
        DtrBusy = Service.DtrBar.Get("[HaselTweaks] Busy");
    }

    public override void Disable()
    {
        DtrInstance.Shown = false;
        DtrFPS.Shown = false;
        DtrBusy.Shown = false;
    }

    public override void Dispose()
    {
        DtrInstance.Dispose();
        DtrFPS.Dispose();
        DtrBusy.Dispose();
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        UpdateInstance();
        UpdateFPS();
        UpdateBusy();
    }

    private void UpdateInstance()
    {
        if (InstanceIdAddress == IntPtr.Zero)
        {
            if (DtrInstance.Shown) DtrInstance.Shown = false;
            return;
        }

        var instanceId = Marshal.ReadByte(InstanceIdAddress + 0x20);

        if (instanceId <= 0 || instanceId >= 10)
        {
            if (DtrInstance.Shown) DtrInstance.Shown = false;
            return;
        }

        var instanceIcon = (char)(SeIconChar.Instance1 + (instanceId - 1));
        DtrInstance.Text = instanceIcon.ToString();
        if (!DtrInstance.Shown) DtrInstance.Shown = true;
    }

    private void UpdateBusy()
    {
        var addr = Service.ClientState.LocalPlayer?.Address;
        if (addr == null || addr == IntPtr.Zero)
        {
            if (DtrBusy.Shown) DtrBusy.Shown = false;
            return;
        }

        var character = (Character*)addr;
        if (character == null || character->OnlineStatus != 12) // 12 = Busy
        {
            if (DtrBusy.Shown) DtrBusy.Shown = false;
            return;
        }

        var statusText = Service.Data.Excel.GetSheet<OnlineStatus>()?.GetRow(12);
        if (statusText == null)
        {
            if (DtrBusy.Shown) DtrBusy.Shown = false;
            return;
        }

        DtrBusy.Text = new SeString()
            .Append(new UIForegroundPayload(1))
            .Append(new UIGlowPayload(16))
            .Append(statusText.Name.ToString())
            .Append(UIGlowPayload.UIGlowOff)
            .Append(UIForegroundPayload.UIForegroundOff);

        if (!DtrBusy.Shown) DtrBusy.Shown = true;
    }

    private void UpdateFPS()
    {
        var fw = GameFramework.Instance();
        DtrFPS.Shown = fw != null;
        if (fw == null) return;

        var fps = MemoryHelper.Read<float>((IntPtr)fw + 0x17C4);
        DtrFPS.Text = $"{fps:0} fps";
    }
}
