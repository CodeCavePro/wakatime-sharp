using System;

namespace WakaTime
{
    public class EditorInfo
    {
        public EditorInfo(string pluginKey, string pluginName, Version pluginVersion)
        {
            if (string.IsNullOrWhiteSpace(pluginKey))
                throw new ArgumentException("Plugin key must be a valid non-empty string", nameof(pluginKey));

            if (string.IsNullOrWhiteSpace(pluginName))
                throw new ArgumentException("Plugin name must be a valid non-empty string", nameof(pluginName));

            Name = nameof(WakaTime);
            Version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;

            PluginKey = pluginKey;
            PluginName = pluginName;
            PluginVersion = pluginVersion ?? throw new ArgumentNullException(nameof(pluginVersion));
        }

        public string Name { get; set; }

        public Version Version { get; set; }

        public string PluginKey { get; set; }

        public string PluginName { get; set; }

        public Version PluginVersion { get; set; }
    }
}
