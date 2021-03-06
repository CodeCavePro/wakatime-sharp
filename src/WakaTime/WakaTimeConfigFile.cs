﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Salaros.Configuration;

namespace WakaTime
{
    public static class WakaTimeConfigFile
    {
        private static readonly string ConfigFilepath;
        private static readonly ConfigParser ConfigParser;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="WakaTimeConfigFile"/> class.
        /// </summary>
        static WakaTimeConfigFile()
        {
            ConfigFilepath = GetConfigFilePath();
            ConfigParser = new ConfigParser(ConfigFilepath, new ConfigParserSettings
            {
                MultiLineValues = MultiLineValues.Simple,
                Encoding = new UTF8Encoding(false, false),
                NewLine = "\n"
            });
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
            ApiKey = ConfigParser.GetValue("settings", "api_key", string.Empty);
            Proxy = ConfigParser.GetValue("settings", "proxy", string.Empty);
            Debug = ConfigParser.GetValue("settings", "debug", false);
        }

        /// <summary>
        /// Save the settings.
        /// </summary>
        public static void Save()
        {
            ConfigParser.SetValue("settings", "api_key", ApiKey.Trim());
            ConfigParser.SetValue("settings", "proxy", Proxy.Trim());
            ConfigParser.SetValue("settings", "debug", Debug);
            ConfigParser.Save(ConfigFilepath);
        }

        public static IWebProxy GetProxy()
        {
            var proxy = WebRequest.DefaultWebProxy;

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
                    proxy = new WebProxy(string.Join(":", new[] { address, port }), true, null, credentials);

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
            var userHomeDir =
#if NET35
                (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                    ? Environment.GetEnvironmentVariable("HOME")
                    : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
#else
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
#endif
            return Path.Combine(userHomeDir, ".wakatime.cfg");
        }

        #endregion
    }
}

