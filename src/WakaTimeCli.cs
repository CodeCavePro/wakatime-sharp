using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

#if NET35 || NET40
using Ionic.Zip;
#else
using System.IO.Compression;
#endif

namespace WakaTime
{
    internal static class WakaTimeCli
    {
        const string CliUrl = "https://github.com/wakatime/wakatime/archive/master.zip";
        static readonly string CliFolder;
        static readonly string ConfigDir;

        static WakaTimeCli()
        {
            ConfigDir = GetAppDataDirectory();
            CliFolder = string.Join(Path.DirectorySeparatorChar.ToString(), new[]{
                "wakatime-master",
                "wakatime",
                "cli.py"
            });
        }

        public static void Initialize()
        {
            if (!DoesCliExist() || !IsCliLatestVersion())
            {
                try
                {
                    var cliFolder = Path.Combine(ConfigDir, "wakatime-master");
                    if (Directory.Exists(cliFolder))
                        Directory.Delete(cliFolder, true);
                }
                finally
                {
                    DownloadCli();
                }
            }
            else
            {
                OnInitialized();
            }
        }

        private static string GetAppDataDirectory()
        {
            var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(roamingFolder, "WakaTime");

            // Create folder if it does not exist
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            return appFolder;
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
            var regex = new Regex(@"(__version_info__ = )(\(( ?\'[0-9]+\'\,?){3}\))");
            var aboutFileUrl = new Uri("https://raw.githubusercontent.com/wakatime/wakatime/master/wakatime/__about__.py");

            try
            {

                var client = new WebClient
                {
                    Proxy = WakaTimeConfigFile.GetProxy()
                };
                var about = client.DownloadString(aboutFileUrl);
                var match = regex.Match(about);
                if (match.Success)
                {
                    var groupVersion = match.Groups[2];
                    var regexVersion = new Regex(@"\'(?<major>\d+)\'\,\s?\'(?<minor>\d+)\'\,\s?\'(?<build>\d+)\'");
                    var versionMatch = regexVersion.Match(groupVersion.Value);
                    return new Version(
                        int.Parse(versionMatch.Groups["major"].Value),
                        int.Parse(versionMatch.Groups["minor"].Value),
                        int.Parse(versionMatch.Groups["build"].Value));
                }
                Logger.Warning("Couldn't auto resolve wakatime cli version");

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

            var tempDir = Path.GetTempPath();
            var localZipFile = Path.Combine(tempDir, "wakatime-cli.zip");
            var client = new WebClient
            {
                Proxy = WakaTimeConfigFile.GetProxy() // Check for proxy setting
            };

            // Download wakatime cli
            DownloadProgress.Show(CliUrl);

            client.DownloadProgressChanged += (s, e) => { DownloadProgress.Report(e); };
            client.DownloadFileCompleted += (s, e) =>
            {
                try
                {
                    DownloadProgress.Complete(e);
                    Logger.Debug("Finished downloading wakatime cli.");

                    // Extract wakatime cli zip file
#if NET35 || NET40
                    using (var zipFile = new ZipFile(localZipFile))
                    {
                        zipFile.ExtractAll(ConfigDir, ExtractExistingFileAction.OverwriteSilently);
                    }
#else
					ZipFile.ExtractToDirectory(localZipFile, ConfigDir);
#endif

                    Logger.Debug(string.Format("Finished extracting wakatime cli: {0}", GetCliPath()));

                    // Delete downloaded file
                    File.Delete(localZipFile);
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to download and/or extract WakaTime Cli", ex);
                }
                finally
                {
                    OnInitialized();
                }
            };

            Logger.Warning("DownloadProgress.Show");

            try
            {
                client.DownloadFileAsync(new Uri(CliUrl), localZipFile);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to download WakaTime Cli", ex);
            }
        }

        private static void OnInitialized()
        {
            var handler = Initialized;
            if (handler != null)
                handler.Invoke(null, EventArgs.Empty);
        }

        public static event EventHandler<EventArgs> Initialized;
    }
}
