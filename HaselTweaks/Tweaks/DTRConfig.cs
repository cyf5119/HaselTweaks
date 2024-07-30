using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Config;
using ImGuiNET;
using GameFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace HaselTweaks.Tweaks;

public class DTRConfiguration
{
    public string FpsFormat = "{0} fps";
}

public partial class DTR
{
    private DTRConfiguration Config => PluginConfig.Tweaks.DTR;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }

    public void OnConfigChange(string fieldName)
    {
        ResetCache();
    }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();

        TextService.Draw("DTR.Config.Explanation.Pre");
        TextService.Draw(HaselColor.From(ImGuiColors.DalamudRed), "DTR.Config.Explanation.DalamudSettings");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        if (ImGui.IsItemClicked())
        {
            void OpenSettings()
            {
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    Framework.RunOnTick(OpenSettings, delayTicks: 2);
                    return;
                }

                DalamudPluginInterface.OpenDalamudSettingsTo(SettingsOpenKind.ServerInfoBar);
            }
            Framework.RunOnTick(OpenSettings, delayTicks: 2);
        }
        ImGuiUtils.SameLineSpace();
        TextService.Draw("DTR.Config.Explanation.Post");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ConfigGui.DrawString("FpsFormat", ref Config.FpsFormat, "{0} fps");

        ImGui.Spacing();
        TextService.Draw("DTR.Config.Format.Example.Label");

        var size = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetStyle().WindowPadding.Y * 2 + ImGui.GetTextLineHeight() + 2);
        using var child = ImRaii.Child("##FormatExample", size, true);
        if (!child) return;

        try
        {
            unsafe
            {
                ImGui.TextUnformatted(string.Format(Config.FpsFormat, (int)(GameFramework.Instance()->FrameRate + 0.5f)));
            }
        }
        catch (FormatException)
        {
            TextService.Draw(Colors.Red, "DTR.Config.FpsFormat.Invalid");
        }
    }
}
