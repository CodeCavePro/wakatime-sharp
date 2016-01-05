using System;
using System.IO;
using IniParser;
using IniParser.Model;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace WakaTime
{
    public static class WakaTimeConfigFile
    {
        private static readonly string _configFilepath;
        private static readonly FileIniDataParser _configParser;
        private static readonly IniData _configData;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="WakaTime.WakaTimeConfigFile"/> class.
        /// </summary>
        static WakaTimeConfigFile()
        {
            _configParser = new FileIniDataParser();
            _configFilepath = GetConfigFilePath();
            _configData = (File.Exists(_configFilepath))
                ? _configParser.ReadFile(_configFilepath, new UTF8Encoding(false))
                : new IniData();
            Read();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        /// <value>The API key.</value>
        public static string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the proxy.
        /// </summary>
        /// <value>The proxy.</value>
        public static string Proxy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="WakaTime.WakaTimeConfigFile"/> is debug.
        /// </summary>
        /// <value><c>true</c> if debug; otherwise, <c>false</c>.</value>
        public static bool Debug { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Read the settings.
        /// </summary>
        public static void Read()
        {
            if (!_configData.Sections.ContainsSection("settings"))
                _configData.Sections.Add(new SectionData("settings"));
            
            ApiKey = _configData["settings"]["api_key"] ?? string.Empty;
            Proxy = _configData["settings"]["proxy"] ?? string.Empty;
            var debugRaw = _configData["settings"]["debug"];
            Debug = (debugRaw == null || debugRaw.Equals(true.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()));
        }

        /// <summary>
        /// Save the settings.
        /// </summary>
        public static void Save()
        {
            if (!_configData.Sections.ContainsSection("settings"))
                _configData.Sections.Add(new SectionData("settings"));
            
            _configData["settings"]["api_key"] = ApiKey.Trim();
            _configData["settings"]["proxy"] = Proxy.Trim();
            _configData["settings"]["debug"] = Debug.ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
            _configParser.WriteFile(_configFilepath, _configData, new UTF8Encoding(false));
        }

        public static WebProxy GetProxy()
        {
            WebProxy proxy = null;

            try
            {
                var proxyStr = Proxy;

                // Regex that matches proxy address with authentication
                var regProxyWithAuth = new Regex(@"\s*(https?:\/\/)?([^\s:]+):([^\s:]+)@([^\s:]+):(\d+)\s*");
                var match = regProxyWithAuth.Match(proxyStr);

                if (match.Success)
                {
                    var username = match.Groups[2].Value;
                    var password = match.Groups[3].Value;
                    var address = match.Groups[4].Value;
                    var port = match.Groups[5].Value;

                    var credentials = new NetworkCredential(username, password);
                    proxy = new WebProxy(string.Join(":", address, port), true, null, credentials);

                    Logger.Debug("A proxy with authentication will be used.");
                    return proxy;
                }

                // Regex that matches proxy address and port(no authentication)
                var regProxy = new Regex(@"\s*(https?:\/\/)?([^\s@]+):(\d+)\s*");
                match = regProxy.Match(proxyStr);

                if (match.Success)
                {
                    var address = match.Groups[2].Value;
                    var port = int.Parse(match.Groups[3].Value);

                    proxy = new WebProxy(address, port);

                    Logger.Debug("A proxy will be used.");
                    return proxy;
                }

                Logger.Debug("No proxy will be used. It's either not set or badly formatted.");
            }
            catch (Exception ex)
            {
                Logger.Error("Exception while parsing the proxy string from WakaTime config file. No proxy will be used.", ex);
            }

            return proxy;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets the config file path.
        /// </summary>
        /// <returns>The config file path.</returns>
        static string GetConfigFilePath()
        {
            var userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userHomeDir,".wakatime.cfg");
        }

        #endregion
    }
}

