using System;
using System.Globalization;
using SoundFilter.Resources;

namespace SoundFilter.Ui;

public class PluginUi : IDisposable
{
    private Plugin Plugin { get; }
    internal Settings Settings { get; }
    private SoundLog SoundLog { get; }

    internal PluginUi(Plugin plugin)
    {
        Plugin = plugin;

        ConfigureLanguage();

        Settings = new Settings(Plugin);
        SoundLog = new SoundLog(Plugin);

        Services.PluginInterface.UiBuilder.Draw += Draw;
        Services.PluginInterface.LanguageChanged += ConfigureLanguage;
    }

    public void Dispose()
    {
        Services.PluginInterface.LanguageChanged -= ConfigureLanguage;
        Services.PluginInterface.UiBuilder.Draw -= Draw;

        Settings.Dispose();
    }

    private static void ConfigureLanguage(string? langCode = null)
    {
        langCode ??= Services.PluginInterface.UiLanguage;
        try
        {
            Language.Culture = new CultureInfo(langCode);
        }
        catch (Exception ex)
        {
            Services.PluginLog.Error(
                ex,
                $"Could not set culture to {langCode} - falling back to default"
            );
            Language.Culture = CultureInfo.DefaultThreadCurrentUICulture;
        }
    }

    private void Draw()
    {
        Settings.Draw();
        SoundLog.Draw();
    }
}
