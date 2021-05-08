using System;
using System.Linq;
using Dalamud.Interface;
using DotNet.Globbing;
using ImGuiNET;
using SoundFilter.Resources;

namespace SoundFilter.Ui {
    public class Settings : IDisposable {
        private SoundFilterPlugin Plugin { get; }

        private bool _showWindow;
        private string _soundPath = string.Empty;

        internal Settings(SoundFilterPlugin plugin) {
            this.Plugin = plugin;

            this.Plugin.Interface.UiBuilder.OnOpenConfigUi += this.Toggle;
        }

        public void Dispose() {
            this.Plugin.Interface.UiBuilder.OnOpenConfigUi -= this.Toggle;
        }

        internal void Toggle(object? sender = null, object? args = null) {
            this._showWindow = !this._showWindow;
        }

        internal void Draw() {
            if (!this._showWindow) {
                return;
            }

            var windowTitle = string.Format(Language.SettingsWindowTitle, SoundFilterPlugin.Name);
            if (!ImGui.Begin($"{windowTitle}###soundfilter-settings", ref this._showWindow)) {
                ImGui.End();
                return;
            }

            var shouldSave = false;
            if (ImGui.Checkbox(Language.SettingsEnableSoundFilter, ref this.Plugin.Config.Enabled)) {
                if (this.Plugin.Config.Enabled) {
                    this.Plugin.Filter.Enable();
                } else {
                    this.Plugin.Filter.Disable();
                }

                shouldSave = true;
            }

            shouldSave |= ImGui.Checkbox(Language.SettingsShowSoundLogWindow, ref this.Plugin.Config.ShowLog);

            ImGui.Separator();

            ImGui.TextUnformatted("Sound path to filter");
            ImGui.InputText("##sound-path", ref this._soundPath, 255);
            ImGui.SameLine();
            if (Util.IconButton(FontAwesomeIcon.Plus, "add") && !string.IsNullOrWhiteSpace(this._soundPath)) {
                this.Plugin.Config.Filtered[this._soundPath] = true;
                shouldSave = true;
            }

            if (ImGui.BeginChild("filtered-sounds")) {
                foreach (var entry in this.Plugin.Config.Filtered.ToList()) {
                    var glob = entry.Key;
                    var enabled = entry.Value;

                    if (Util.IconButton(FontAwesomeIcon.Trash, $"delete-{glob}")) {
                        this.Plugin.Config.Filtered.Remove(glob);
                        shouldSave = true;
                    }

                    ImGui.SameLine();

                    if (ImGui.Checkbox(glob, ref enabled)) {
                        this.Plugin.Config.Filtered[glob] = enabled;
                        shouldSave = true;
                    }
                }

                ImGui.EndChild();
            }

            if (shouldSave) {
                this.Plugin.Config.Save();
            }

            ImGui.End();
        }
    }
}
