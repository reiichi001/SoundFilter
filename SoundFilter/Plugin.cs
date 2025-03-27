using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using SoundFilter.Config;
using SoundFilter.Resources;
using SoundFilter.Ui;

namespace SoundFilter;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Plugin : IDalamudPlugin
{
    public static string Name => "Sound Filter";

    internal Configuration Config { get; }
    internal Filter Filter { get; }
    internal PluginUi Ui { get; }
    private Commands Commands { get; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Services>();

        Config = Migrator.LoadConfiguration();

        Filter = new Filter(this);
        if (Config.Enabled)
        {
            Filter.Enable();
        }

        Ui = new PluginUi(this);
        Commands = new Commands(this);

        if (Services.PluginInterface.Reason != PluginLoadReason.Installer)
        {
            return;
        }

        var message = string.Format(Language.LoadWarning, Name);
        Services.ChatGui.Print(
            new XivChatEntry
            {
                Name = Name,
                Message = new SeString(
                    new UIForegroundPayload(502),
                    new TextPayload($"[{Name}] {message}"),
                    new UIForegroundPayload(0)
                ),
            }
        );
    }

    public void Dispose()
    {
        Commands.Dispose();
        Ui.Dispose();
        Filter.Dispose();
    }
}
