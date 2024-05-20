// © Customize+.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using CustomizePlus.Helpers;
using Dalamud.Interface;
using ImGuiNET;

namespace CustomizePlus.UI.Windows
{
    public class MainWindow : WindowBase
    {
        private static Vector4 WarningColor = new Vector4(1, 0.5f, 0, 1);

        protected override string Title => "Customize+";
        protected override bool SingleInstance => true;

        public static void Show()
        {
            Plugin.InterfaceManager.Show<MainWindow>();
        }

        public static void Toggle()
        {
            Plugin.InterfaceManager.Toggle<MainWindow>();
        }

        protected override void DrawContents()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, WarningColor);
            CtrlHelper.StaticLabelWithIconOnBothSides(FontAwesomeIcon.ExclamationTriangle, "ATTENTION, PLEASE READ!", CtrlHelper.TextAlignment.Center);
            ImGui.PopStyleColor();
            CtrlHelper.StaticLabel("You need to update Customize+ to latest version. Currently installed version is no longer functional.", CtrlHelper.TextAlignment.Center);
            CtrlHelper.StaticLabel("Please delete your current version, remove XIV-Tools repository if you are using it and install Customize+ from the following url: https://github.com/Aether-Tools/CustomizePlus", CtrlHelper.TextAlignment.Center);
            ImGui.PushStyleColor(ImGuiCol.Text, WarningColor);
            CtrlHelper.StaticLabelWithIconOnBothSides(FontAwesomeIcon.ExclamationTriangle, "Your settings will be automatically migrated to new version.", CtrlHelper.TextAlignment.Center);
            ImGui.PopStyleColor();

            CtrlHelper.StaticLabel("If you need help, feel free to join support discord: https://discord.gg/KvGJCCnG8t", CtrlHelper.TextAlignment.Center);

            ImGui.Dummy(new Vector2((ImGui.GetContentRegionAvail().X / 2) - 150, 0));
            ImGui.SameLine();
            if (ImGui.Button("Copy Repository Link"))
            {
                Clipboard.SetText("https://raw.githubusercontent.com/Aether-Tools/DalamudPlugins/main/repo.json");
            }
            ImGui.SameLine();
            if (ImGui.Button("Open Link to GitHub"))
            {
                Process.Start(new ProcessStartInfo("https://github.com/Aether-Tools/CustomizePlus") { UseShellExecute = true });
            }
            ImGui.Dummy(new Vector2((ImGui.GetContentRegionAvail().X / 2) - 75, 0));
            ImGui.SameLine();
            if (ImGui.Button("Join Support Discord"))
            {
                Process.Start(new ProcessStartInfo("https://discord.gg/KvGJCCnG8t") { UseShellExecute = true });
            }
        }
    }
}