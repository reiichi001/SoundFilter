using System;
using Dalamud.Game.Command;

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
            this.Plugin.Ui.Settings.Toggle();
        }
    }
}
