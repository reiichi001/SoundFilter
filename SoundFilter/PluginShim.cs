using Dalamud.Plugin;

namespace SoundFilter {
    // ReSharper disable once UnusedType.Global
    public class PluginShim : IDalamudPlugin {
        public string Name => SoundFilterPlugin.Name;

        private SoundFilterPlugin? Plugin { get; set; }

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.Plugin = new SoundFilterPlugin(pluginInterface);
        }

        public void Dispose() {
            this.Plugin?.Dispose();
        }
    }
}
