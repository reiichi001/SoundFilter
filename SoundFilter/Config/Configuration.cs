using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using DotNet.Globbing;

namespace SoundFilter.Config;

[Serializable]
internal class Configuration : IPluginConfiguration
{
    internal const int LatestVersion = 2;

    public int Version { get; set; } = LatestVersion;

    private Dictionary<string, Glob> CachedGlobs { get; } = [];

    public bool Enabled = true;
    public bool ShowLog;
    public bool LogEnabled = true;
    public bool LogFiltered = false;
    public uint LogEntries = 250;
    public List<CustomFilter> Filters { get; set; } = [];

    internal IReadOnlyDictionary<Glob, bool> Globs
    {
        get
        {
            var dictionary = new Dictionary<Glob, bool>();
            foreach (var filter in Filters)
            {
                foreach (var globString in filter.Globs)
                {
                    if (CachedGlobs.TryGetValue(globString, out var cached))
                    {
                        dictionary[cached] = filter.Enabled;
                        continue;
                    }

                    var glob = Glob.Parse(globString);
                    CachedGlobs[globString] = glob;
                    dictionary[glob] = filter.Enabled;
                }
            }

            return dictionary;
        }
    }

    internal void Save()
    {
        Services.PluginInterface.SavePluginConfig(this);
    }
}
