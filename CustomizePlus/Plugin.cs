// © Customize+.
// Licensed under the MIT license.

// Signautres stolen from:
// https://github.com/0ceal0t/DXTest/blob/8e9aef4f6f871e7743aafe56deb9e8ad4dc87a0d/SamplePlugin/Plugin.DX.cs
// I don't know how they work, but they do!

using System;
using CustomizePlus.Extensions;
using CustomizePlus.Helpers;
using CustomizePlus.Services;
using CustomizePlus.UI;
using CustomizePlus.UI.Windows;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace CustomizePlus
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Customize Plus";

        public static UserInterfaceManager InterfaceManager { get; } = new();

        public Plugin(DalamudPluginInterface pluginInterface)
        {
            try
            {
                DalamudServices.Initialize(pluginInterface);
                DalamudServices.PluginInterface.UiBuilder.DisableGposeUiHide = true;

                DalamudServices.CommandManager.AddCommand((s, t) => MainWindow.Toggle(), "/customize",
                    "Toggles the Customize+ configuration window.");
                DalamudServices.CommandManager.AddCommand((s, t) => MainWindow.Toggle(), "/c+",
                    "Toggles the Customize+ configuration window.");

                DalamudServices.PluginInterface.UiBuilder.Draw += InterfaceManager.Draw;
                DalamudServices.PluginInterface.UiBuilder.OpenConfigUi += MainWindow.Toggle;

                MainWindow.Show();

                ChatHelper.PrintInChat("Customize+ Started!");

                UIModule.PlayChatSoundEffect(11);
                ChatHelper.PrintInChat("CUSTOMIZE+ IS NO LONGER FUNCTIONAL, OPEN SETTINGS FOR DETAILS");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error instantiating plugin");
                ChatHelper.PrintInChat(
                    "An error occurred while starting Customize+. See the Dalamud log for more details");
            }
        }

        public void Dispose()
        {
            InterfaceManager.Dispose();

            CommandManagerExtensions.Dispose();

            DalamudServices.PluginInterface.UiBuilder.Draw -= InterfaceManager.Draw;
            DalamudServices.PluginInterface.UiBuilder.OpenConfigUi -= MainWindow.Show;
        }
    }
}