using System;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using SoundFilter.Config;
using SoundFilter.Resources;
using SoundFilter.Ui;

namespace SoundFilter {
    internal class SoundFilterPlugin : IDisposable {
        internal const string Name = "Sound Filter";

        internal DalamudPluginInterface Interface { get; }
        internal Configuration Config { get; }
        internal Filter Filter { get; }
        internal PluginUi Ui { get; }
        private Commands Commands { get; }

        internal SoundFilterPlugin(DalamudPluginInterface @interface) {
            this.Interface = @interface;

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

            var message = string.Format(Language.LoadWarning, Name);
            this.Interface.Framework.Gui.Chat.PrintChat(new XivChatEntry {
                Name = Name,
                MessageBytes = new SeString(new Payload[] {
                    new UIForegroundPayload(this.Interface.Data, 502),
                    new TextPayload($"[{Name}] {message}"),
                    new UIForegroundPayload(this.Interface.Data, 0),
                }).Encode(),
            });
        }

        public void Dispose() {
            this.Commands.Dispose();
            this.Ui.Dispose();
            this.Filter.Dispose();
        }
    }
}
