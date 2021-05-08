using System;
using System.Globalization;
using Dalamud.Plugin;
using SoundFilter.Resources;

namespace SoundFilter.Ui {
    public class PluginUi : IDisposable {
        private SoundFilterPlugin Plugin { get; }
        internal Settings Settings { get; }
        private SoundLog SoundLog { get; }

        internal PluginUi(SoundFilterPlugin plugin) {
            this.Plugin = plugin;

            this.ConfigureLanguage();

            this.Settings = new Settings(this.Plugin);
            this.SoundLog = new SoundLog(this.Plugin);

            this.Plugin.Interface.UiBuilder.OnBuildUi += this.Draw;
            this.Plugin.Interface.OnLanguageChanged += this.ConfigureLanguage;
        }

        public void Dispose() {
            this.Plugin.Interface.OnLanguageChanged -= this.ConfigureLanguage;
            this.Plugin.Interface.UiBuilder.OnBuildUi -= this.Draw;

            this.Settings.Dispose();
        }

        private void ConfigureLanguage(string? langCode = null) {
            langCode ??= this.Plugin.Interface.UiLanguage ?? "en";
            try {
                Language.Culture = new CultureInfo(langCode);
            } catch (Exception ex) {
                PluginLog.LogError(ex, $"Could not set culture to {langCode} - falling back to default");
                Language.Culture = CultureInfo.DefaultThreadCurrentUICulture;
            }
        }

        private void Draw() {
            this.Settings.Draw();
            this.SoundLog.Draw();
        }
    }
}
