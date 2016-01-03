using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WakaTime
{
    internal static class WakaTimeCli
    {
        const string CliUrl = "https://github.com/wakatime/wakatime/archive/master.zip";
        const string CliFolder = @"wakatime-master\wakatime\cli.py";
        static readonly string ConfigDir;

        static WakaTimeCli()
        {
            ConfigDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        public static void Initialize()
        {
            if (DoesCliExist() && IsCliLatestVersion())
                return;

            try
            {
                Directory.Delete(ConfigDir + "wakatime-master", true);
            }
            catch
            {
                /* ignored */
            }

            DownloadCli();
        }

        internal static string GetCliPath()
        {
            return Path.Combine(ConfigDir, CliFolder);
        }

        public static void SendHeartbeat(PythonCliParameters cliParameters)
        {
            var pythonBinary = PythonManager.GetPython();
            if (pythonBinary != null)
            {
                var process = new RunProcess(pythonBinary, cliParameters.ToArray());
                if (WakaTimeConfigFile.Debug)
                {
                    Logger.Debug(string.Format("[\"{0}\", \"{1}\"]", pythonBinary, string.Join("\", \"", cliParameters.ToArray(true))));
                    process.Run();
                    Logger.Debug(string.Format("CLI STDOUT: {0}", process.Output));
                    Logger.Debug(string.Format("CLI STDERR: {0}", process.Error));
                }
                else
                    process.RunInBackground();

                if (!process.Success)
                    Logger.Error(string.Format("Could not send heartbeat: {0}", process.Error));
            }
            else
            {
                Logger.Error("Could not send heartbeat because python is not installed");
            }
        }

        static bool DoesCliExist()
        {
            return File.Exists(GetCliPath());
        }

        static bool IsCliLatestVersion()
        {
            var process = new RunProcess(PythonManager.GetPython(), GetCliPath(), "--version");
            process.Run();

            var version = GetLatestWakaTimeCliVersion();
            return process.Success && process.Error.Equals(version.ToString());
        }

        static Version GetLatestWakaTimeCliVersion()
        {
            var regex = new Regex(@"(__version_info__ = )(\(( ?\'[0-9]\'\,?){3}\))");
            var client = new WebClient
            {
                Proxy = WakaTimeConfigFile.GetProxy()
            };

            try
            {
                var about = client.DownloadString("https://raw.githubusercontent.com/wakatime/wakatime/master/wakatime/__about__.py");
                var match = regex.Match(about);

                if (match.Success)
                {
                    var groupVersion = match.Groups[2];
                    var regexVersion = new Regex(@"(?<major>/d)\.(?<minor>/d)\.(?<build>/d))");
                    var versionMatch = regexVersion.Match(groupVersion.Value);
                    return new Version(
                        int.Parse(versionMatch.Groups["major"].Value),
                        int.Parse(versionMatch.Groups["minor"].Value),
                        int.Parse(versionMatch.Groups["build"].Value));
                }
                else
                {
                    Logger.Warning("Couldn't auto resolve wakatime cli version");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Exception when checking current wakatime cli version: ", ex);
            }
            return new Version();
        }

        static public void DownloadCli()
        {
            Logger.Debug("Downloading wakatime cli...");

            var localZipFile = Path.Combine(ConfigDir, "wakatime-cli.zip");
            var client = new WebClient
            {
                Proxy = WakaTimeConfigFile.GetProxy() // Check for proxy setting
            };

            // Download wakatime cli
            client.DownloadFile(CliUrl, localZipFile);

            Logger.Debug("Finished downloading wakatime cli.");

            // Extract wakatime cli zip file
            ZipFile.ExtractToDirectory(localZipFile, ConfigDir);

            try
            {
                File.Delete(localZipFile);
            }
            catch { /* ignored */ }
        }
    }
}
