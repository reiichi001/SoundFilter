using System;
using System.Globalization;
using Dalamud.Logging;
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

            this.Plugin.Interface.UiBuilder.Draw += this.Draw;
            this.Plugin.Interface.LanguageChanged += this.ConfigureLanguage;
        }

        public void Dispose() {
            this.Plugin.Interface.LanguageChanged -= this.ConfigureLanguage;
            this.Plugin.Interface.UiBuilder.Draw -= this.Draw;

            this.Settings.Dispose();
        }

        private void ConfigureLanguage(string? langCode = null) {
            // ReSharper disable once ConstantNullCoalescingCondition
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
