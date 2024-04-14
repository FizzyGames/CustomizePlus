// © Customize+.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Windows.Forms;
using CustomizePlus.Helpers;
using ImGuiNET;

namespace CustomizePlus.UI.Windows
{
    public class MainWindow : WindowBase
    {
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
            CtrlHelper.StaticLabel("ATTENTION, PLEASE READ! You need to update Customize+ to latest version. All functionality is disabled.", CtrlHelper.TextAlignment.Center);
            CtrlHelper.StaticLabel("Please delete your current version, remove XIV-Tools repository if you are using it and install Customize+ from the following url: https://github.com/Aether-Tools/CustomizePlus", CtrlHelper.TextAlignment.Center);
            CtrlHelper.StaticLabel("Your settings will be automatically migrated to new version.", CtrlHelper.TextAlignment.Center);

            ImGui.Dummy(new Vector2((ImGui.GetContentRegionAvail().X / 2) - 150, 0));
            ImGui.SameLine();
            if (ImGui.Button("Copy Repository Link"))
            {
                Clipboard.SetText("https://raw.githubusercontent.com/Aether-Tools/DalamudPlugins/main/repo.json");
            }
            ImGui.SameLine();
            if (ImGui.Button("Open Link to Github"))
            {
                Process.Start(new ProcessStartInfo("https://github.com/Aether-Tools/CustomizePlus") { UseShellExecute = true });
            }
        }
    }
}