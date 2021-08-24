using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Plugin;
using SoundFilter.Config;
using SoundFilter.Resources;
using SoundFilter.Ui;

namespace SoundFilter {
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SoundFilterPlugin : IDalamudPlugin {
        public string Name => "Sound Filter";

        [PluginService]
        internal DalamudPluginInterface Interface { get; init; } = null!;

        [PluginService]
        internal ChatGui ChatGui { get; init; } = null!;

        [PluginService]
        internal CommandManager CommandManager { get; init; } = null!;

        [PluginService]
        internal Framework Framework { get; init; } = null!;

        [PluginService]
        internal SigScanner SigScanner { get; init; } = null!;

        internal Configuration Config { get; }
        internal Filter Filter { get; }
        internal PluginUi Ui { get; }
        private Commands Commands { get; }

        public SoundFilterPlugin() {
            this.Config = Migrator.LoadConfiguration(this);
            this.Config.Initialise(this.Interface);

            this.Filter = new Filter(this);
            if (this.Config.Enabled) {
                this.Filter.Enable();
            }

            this.Ui = new PluginUi(this);
            this.Commands = new Commands(this);

            if (this.Interface.Reason != PluginLoadReason.Installer) {
                return;
            }

            var message = string.Format(Language.LoadWarning, this.Name);
            this.ChatGui.PrintChat(new XivChatEntry {
                Name = this.Name,
                Message = new SeString(
                    new UIForegroundPayload(502),
                    new TextPayload($"[{this.Name}] {message}"),
                    new UIForegroundPayload(0)
                ),
            });
        }

        public void Dispose() {
            this.Commands.Dispose();
            this.Ui.Dispose();
            this.Filter.Dispose();
        }
    }
}
