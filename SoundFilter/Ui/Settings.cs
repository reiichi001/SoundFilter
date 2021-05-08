using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using SoundFilter.Config;
using SoundFilter.Resources;

namespace SoundFilter.Ui {
    public class Settings : IDisposable {
        private SoundFilterPlugin Plugin { get; }

        private bool _showWindow;
        private string _filterName = string.Empty;
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

            ImGui.SetNextWindowSize(new Vector2(500, 450), ImGuiCond.FirstUseEver);

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

            ImGui.TextUnformatted(Language.SettingsAddFilterName);
            ImGui.InputText("##sound-filter-name", ref this._filterName, 255);

            ImGui.TextUnformatted(Language.SettingsAddPathToFilter);
            ImGui.InputText("##sound-path", ref this._soundPath, 255);
            ImGui.SameLine();
            if (Util.IconButton(FontAwesomeIcon.Plus, "add") && !string.IsNullOrWhiteSpace(this._soundPath) && !string.IsNullOrWhiteSpace(this._filterName)) {
                this.Plugin.Config.Filtered[this._soundPath] = new CustomFilter {
                    Name = this._filterName,
                    Enabled = true,
                };
                shouldSave = true;
            }

            ImGui.Separator();

            if (ImGui.BeginChild("filtered-sounds")) {
                var i = 0;

                foreach (var entry in this.Plugin.Config.Filtered.ToList()) {
                    var glob = entry.Key;

                    if (Util.IconButton(FontAwesomeIcon.Trash, $"delete-{glob}")) {
                        this.Plugin.Config.Filtered.Remove(glob);
                        shouldSave = true;
                    }

                    ImGui.SameLine();

                    if (Util.IconButton(FontAwesomeIcon.Copy, $"copy-{glob}")) {
                        ImGui.SetClipboardText(glob);
                    }

                    ImGui.SameLine();

                    shouldSave |= ImGui.Checkbox($"{entry.Value.Name}##{i}-{glob}", ref entry.Value.Enabled);
                    if (ImGui.IsItemHovered()) {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted(glob);
                        ImGui.EndTooltip();
                    }

                    i += 1;
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
