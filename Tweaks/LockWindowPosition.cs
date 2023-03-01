using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs.Addons;
using HaselTweaks.Utils;
using ImGuiNET;

namespace HaselTweaks.Tweaks;

public unsafe partial class LockWindowPosition : Tweak
{
    public override string Name => "Lock Window Position";
    public override string Description => "Lock window positions so they can't move.";
    public static Configuration Config => Plugin.Config.Tweaks.LockWindowPosition;

    public record LockedWindowSetting
    {
        public bool Enabled = true;
        public string Name = "";
    }

    public class Configuration
    {
        public bool Inverted = false;
        public bool AddLockUnlockContextMenuEntries = true;
        public List<LockedWindowSetting> LockedWindows = new();
    }

    private const int EventParamLock = 9901;
    private const int EventParamUnlock = 9902;
    private readonly string[] IgnoredAddons = new[] {
        "CharaCardEditMenu", // always opens docked to CharaCard (OnSetup)
    };

    private bool ShowPicker = false;
    private string HoveredWindowName = "";
    private Vector2 HoveredWindowPos;
    private Vector2 HoveredWindowSize;
    private int EventIndexToDisable = 0;

    public override bool HasCustomConfig => true;
    public override void DrawCustomConfig()
    {
        ImGui.Checkbox("Invert logic (locks all windows)##HaselTweaks_LockWindows_Inverted", ref Config.Inverted);
        if (ImGui.IsItemClicked())
        {
            Plugin.Config.Save();
        }

        ImGui.Checkbox("Add Lock/Unlock Position to windows context menu##HaselTweaks_LockWindows_AddLockUnlockContextMenuEntries", ref Config.AddLockUnlockContextMenuEntries);
        if (ImGui.IsItemClicked())
        {
            Plugin.Config.Save();
        }

        ImGuiUtils.DrawPaddedSeparator();
        if (Config.LockedWindows.Any())
        {
            ImGui.Text("Windows:");

            if (!ImGui.BeginTable("##HaselTweaks_LockWindowsTable", 3, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.NoPadOuterX))
            {
                return;
            }

            ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 24);

            var entryToRemove = -1;
            var i = 0;

            foreach (var entry in Config.LockedWindows)
            {
                var key = $"##HaselTweaks_LockWindowsTable_{i}";
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Checkbox($"{key}_Enabled", ref entry.Enabled);
                if (ImGui.IsItemHovered())
                {
                    var isLocked = entry.Enabled;

                    if (Config.Inverted)
                        isLocked = !isLocked;

                    ImGui.SetTooltip("Window is " + (isLocked ? "locked" : "unlocked"));
                }
                if (ImGui.IsItemClicked())
                {
                    Plugin.Config.Save();
                }

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Name}");

                ImGui.TableNextColumn();
                if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift)))
                {
                    if (ImGuiUtils.IconButton(FontAwesomeIcon.Trash, $"{key}_Delete"))
                    {
                        entryToRemove = i;
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Delete");
                    }
                }
                else
                {
                    ImGuiUtils.IconButtonDisabled(FontAwesomeIcon.Trash, $"{key}_Delete");

                    if (ImGui.IsItemHovered())
                    {
                        if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
                            ImGui.SetTooltip("Focus window and hold shift to delete");
                        else
                            ImGui.SetTooltip("Hold shift to delete");
                    }
                }

                i++;
            }

            ImGui.EndTable();

            if (entryToRemove != -1)
            {
                Config.LockedWindows.RemoveAt(entryToRemove);
                Plugin.Config.Save();
            }
        }
        else
        {
            ImGui.TextDisabled("No windows added yet.");
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 4);
        }

        if (ShowPicker)
        {
            if (ImGui.Button("Cancel"))
            {
                ShowPicker = false;
            }
        }
        else
        {
            if (ImGui.Button("Pick Window"))
            {
                HoveredWindowName = "";
                HoveredWindowPos = default;
                HoveredWindowSize = default;
                ShowPicker = true;
            }
        }

        if (ShowPicker && HoveredWindowPos != default)
        {
            ImGui.SetNextWindowPos(HoveredWindowPos);
            ImGui.SetNextWindowSize(HoveredWindowSize);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1.0f);
            ImGui.PushStyleColor(ImGuiCol.Border, ImGuiUtils.ColorGold);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.847f, 0.733f, 0.49f, 0.33f));

            if (ImGui.Begin("Lock Windows Picker", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
            {
                var drawList = ImGui.GetForegroundDrawList();
                var textPos = HoveredWindowPos + new Vector2(0, -ImGui.GetTextLineHeight());
                drawList.AddText(textPos + Vector2.One, 0xFF000000, HoveredWindowName);
                drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(ImGuiUtils.ColorGold), HoveredWindowName);

                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    ShowPicker = false;

                    if (HoveredWindowName != "" && !Config.LockedWindows.Any(entry => entry.Name == HoveredWindowName))
                    {
                        Config.LockedWindows.Add(new()
                        {
                            Name = HoveredWindowName
                        });
                        Plugin.Config.Save();
                    }
                }

                ImGui.End();
            }
            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar();
        }
    }

    public override void OnConfigWindowClose()
    {
        HoveredWindowName = "";
        HoveredWindowPos = default;
        HoveredWindowSize = default;
        ShowPicker = false;
    }

    // block GearSetList from moving when opened by Character
    [SigHook("40 53 56 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 89 AC 24 ?? ?? ?? ??")]
    public nint AddonGearSetList_OnSetup(AddonGearSetList* addon, int numAtkValues, AtkValue* atkValues)
    {
        var result = AddonGearSetList_OnSetupHook.Original(addon, numAtkValues, atkValues);

        var isLocked = Config.LockedWindows.Any(entry => entry.Enabled && entry.Name == "GearSetList");

        if (Config.Inverted)
            isLocked = !isLocked;

        if (isLocked)
            addon->ResetPosition = false;

        return result;
    }

    [SigHook("E8 ?? ?? ?? ?? 0F BF 8C 24 ?? ?? ?? ?? 01 8F ?? ?? ?? ??")]
    public bool Move(AtkUnitBase* atkUnitBase, nint xDelta, nint yDelta)
    {
        if (atkUnitBase != null)
        {
            var name = MemoryHelper.ReadStringNullTerminated((nint)atkUnitBase->Name);
            var isLocked = Config.LockedWindows.Any(entry => entry.Enabled && entry.Name == name);

            if (Config.Inverted)
                isLocked = !isLocked;

            if (isLocked)
                return false;
        }

        return MoveHook.Original(atkUnitBase, xDelta, yDelta);
    }

    [SigHook("48 89 5C 24 ?? 48 89 6C 24 ?? 57 48 83 EC 30 80 7A 37 00")]
    public bool RaptureAtkUnitManager_Vf6(RaptureAtkUnitManager* self, nint a2)
    {
        if (ShowPicker)
        {
            if (a2 != 0)
            {
                var atkUnitBase = *(AtkUnitBase**)(a2 + 8);
                if (atkUnitBase != null && atkUnitBase->WindowNode != null && atkUnitBase->WindowCollisionNode != null)
                {
                    var name = MemoryHelper.ReadStringNullTerminated((nint)atkUnitBase->Name);
                    if (!IgnoredAddons.Contains(name))
                    {
                        HoveredWindowName = name;
                        HoveredWindowPos = new(atkUnitBase->X, atkUnitBase->Y);
                        HoveredWindowSize = new(atkUnitBase->WindowNode->AtkResNode.Width, atkUnitBase->WindowNode->AtkResNode.Height - 7);
                    }
                    else
                    {
                        HoveredWindowName = "";
                        HoveredWindowPos = default;
                        HoveredWindowSize = default;
                    }
                }
                else
                {
                    HoveredWindowName = "";
                    HoveredWindowPos = default;
                    HoveredWindowSize = default;
                }
            }
            else
            {
                ShowPicker = false;
            }

            return false;
        }

        return RaptureAtkUnitManager_Vf6Hook.Original(self, a2);
    }

    [SigHook("E8 ?? ?? ?? ?? 44 8D 4E 0C")]
    public nint AgentContext_ClearMenu(AgentContext* agent)
    {
        if (EventIndexToDisable != 0)
            EventIndexToDisable = 0;

        return AgentContext_ClearMenuHook.Original(agent);
    }

    [SigHook("E8 ?? ?? ?? ?? 3B F6")]
    public nint AgentContext_AddMenuItem2(AgentContext* agent, uint addonRowId, nint handlerPtr, long handlerParam, int disabled, int submenu)
    {
        if (addonRowId == 8660 && agent->ContextMenuIndex == 0) // "Return to Default Position"
        {
            EventIndexToDisable = agent->CurrentContextMenu->CurrentEventIndex;
        }

        return AgentContext_AddMenuItem2Hook.Original(agent, addonRowId, handlerPtr, handlerParam, disabled, submenu);
    }

    [SigHook("E8 ?? ?? ?? ?? 66 89 6F 44")]
    public nint AgentContext_OpenContextMenuForAddon(AgentContext* agent, uint addonId, bool bindToOwner)
    {
        if (EventIndexToDisable == 7 && agent->ContextMenuIndex == 0)
        {
            var addon = GetAddon(addonId);
            if (addon != null)
            {
                var name = MemoryHelper.ReadStringNullTerminated((nint)addon->Name);

                if (!IgnoredAddons.Contains(name))
                {
                    var isLocked = Config.LockedWindows.Any(entry => entry.Enabled && entry.Name == name);

                    if (Config.Inverted)
                        isLocked = !isLocked;

                    if (isLocked)
                    {
                        agent->CurrentContextMenu->ContextItemDisabledMask |= 1; // keeping it simple. disables "Return to Default Position"

                        if (Config.AddLockUnlockContextMenuEntries)
                        {
                            var title = Service.ClientState.ClientLanguage switch
                            {
                                ClientLanguage.German => "Position entsperren",
                                // ClientLanguage.French => "",
                                // ClientLanguage.Japanese => "",
                                _ => "Unlock Position"
                            };

                            AddMenuEntry(title, EventParamUnlock);
                        }
                    }
                    else
                    {
                        if (Config.AddLockUnlockContextMenuEntries)
                        {
                            var title = Service.ClientState.ClientLanguage switch
                            {
                                ClientLanguage.German => "Position sperren",
                                // ClientLanguage.French => "",
                                // ClientLanguage.Japanese => "",
                                _ => "Lock Position"
                            };

                            AddMenuEntry(title, EventParamLock);
                        }
                    }
                }
            }
        }

        return AgentContext_OpenContextMenuForAddonHook.Original(agent, addonId, bindToOwner);
    }

    [SigHook("48 89 6C 24 ?? 48 89 54 24 ?? 56 41 54")]
    public AtkValue* WindowContextMenuEventHandler(nint self, AtkValue* result, nint a3, long a4, long eventParam)
    {
        if (EventIndexToDisable == 7 && eventParam is EventParamUnlock or EventParamLock)
        {
            var agent = GetAgent<AgentContext>(AgentId.Context);
            var addon = GetAddon(agent->OwnerAddon);
            if (addon != null)
            {
                var name = MemoryHelper.ReadStringNullTerminated((nint)addon->Name);

                var entry = Config.LockedWindows.FirstOrDefault(entry => entry?.Name == name, null);
                var isLocked = eventParam == EventParamLock;

                if (Config.Inverted)
                    isLocked = !isLocked;

                if (entry != null)
                {
                    entry.Enabled = isLocked;
                }
                else
                {
                    Config.LockedWindows.Add(new()
                    {
                        Enabled = isLocked,
                        Name = name,
                    });
                }
            }

            EventIndexToDisable = 0;

            result->Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Bool;
            result->Byte = 0;
            return result;
        }

        if (EventIndexToDisable != 0)
            EventIndexToDisable = 0;

        return WindowContextMenuEventHandlerHook.Original(self, result, a3, a4, eventParam);
    }

    private void AddMenuEntry(string text, int eventParam)
    {
        var bytes = new SeStringBuilder()
            .AddUiForeground("\uE078 ", 32)
            .AddText(text)
            .Encode();

        var textPtr = Marshal.AllocHGlobal(bytes.Length + 1);
        Unsafe.InitBlock((void*)textPtr, 0, (uint)bytes.Length + 1);
        MemoryHelper.WriteRaw(textPtr, bytes);

        var handler = (nint)AtkStage.GetSingleton()->RaptureAtkUnitManager + 0x9C88; // see vtbl ptr in ctor
        GetAgent<AgentContext>(AgentId.Context)
            ->AddMenuItem((byte*)textPtr, (void*)handler, eventParam);

        Marshal.FreeHGlobal(textPtr);
    }
}