using System;
using System.Linq;
using Dalamud.Interface;
using ImGuiNET;
using SoundFilter.Resources;

namespace SoundFilter.Ui {
    public class SoundLog {
        private SoundFilterPlugin Plugin { get; }

        private string _search = string.Empty;

        internal SoundLog(SoundFilterPlugin plugin) {
            this.Plugin = plugin;
        }

        internal void Draw() {
            if (!this.Plugin.Config.ShowLog) {
                return;
            }

            if (!ImGui.Begin(Language.LogWindowTitle, ref this.Plugin.Config.ShowLog)) {
                this.Plugin.Config.Save();
                ImGui.End();
                return;
            }

            ImGui.InputText(Language.LogSearch, ref this._search, 255);

            var entries = (int) this.Plugin.Config.LogEntries;
            if (ImGui.InputInt(Language.LogMaxRecentSounds, ref entries)) {
                this.Plugin.Config.LogEntries = (uint) Math.Min(10_000, Math.Max(0, entries));
                this.Plugin.Config.Save();
            }

            ImGui.Separator();

            if (ImGui.BeginChild("sounds")) {
                var i = 0;
                foreach (var recent in this.Plugin.Filter.Recent.Reverse()) {
                    if (!string.IsNullOrWhiteSpace(this._search) && !recent.ContainsIgnoreCase(this._search)) {
                        continue;
                    }

                    if (Util.IconButton(FontAwesomeIcon.Copy, $"copy-{recent}-{i}")) {
                        ImGui.SetClipboardText(recent);
                    }

                    ImGui.SameLine();
                    ImGui.TextUnformatted(recent);
                    i += 1;
                }

                ImGui.EndChild();
            }


            ImGui.End();
        }
    }
}
