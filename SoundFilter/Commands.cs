using System;
using System.Linq;
using Dalamud.Game.Command;
using SoundFilter.Resources;

namespace SoundFilter {
    internal class Commands : IDisposable {
        private const string Name = "/soundfilter";

        private SoundFilterPlugin Plugin { get; }

        public Commands(SoundFilterPlugin plugin) {
            this.Plugin = plugin;

            this.Plugin.Interface.CommandManager.AddHandler(Name, new CommandInfo(this.OnCommand) {
                HelpMessage = $"Toggle the {SoundFilterPlugin.Name} config",
            });
        }

        public void Dispose() {
            this.Plugin.Interface.CommandManager.RemoveHandler(Name);
        }

        private void OnCommand(string command, string args) {
            if (string.IsNullOrWhiteSpace(args)) {
                this.Plugin.Ui.Settings.Toggle();
                return;
            }

            var chat = this.Plugin.Interface.Framework.Gui.Chat;

            var split = args.Split(' ');
            if (split.Length < 1) {
                chat.PrintError($"[{SoundFilterPlugin.Name}] {Language.CommandNotEnoughArguments}");
                return;
            }

            var filterName = split.Length > 1 ? string.Join(" ", split.Skip(1)) : null;
            var filter = filterName == null ? null : this.Plugin.Config.Filtered.Values.FirstOrDefault(filter => filter.Name == filterName);
            if (filterName != null && filter == null) {
                chat.PrintError($"[{SoundFilterPlugin.Name}] {Language.CommandNoSuchFilter}");
                return;
            }

            bool? enabled = split[0] switch {
                "enable" => true,
                "disable" => false,
                "toggle" when filter == null => !this.Plugin.Config.Enabled,
                "toggle" => !filter.Enabled,
                _ => null,
            };
            if (enabled == null) {
                chat.PrintError($"[{SoundFilterPlugin.Name}] {Language.CommandInvalidSubcommand}");
                return;
            }

            if (filter != null) {
                filter.Enabled = enabled.Value;
            } else {
                this.Plugin.Config.Enabled = enabled.Value;
            }

            this.Plugin.Config.Save();
        }
    }
}
