using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace WakaTime
{
    internal class PythonCliParameters
    {
        public string File { get; set; }
        public string Plugin { get; set; }
        public bool IsWrite { get; set; }
        public string Project { get; set; }

        public string[] ToArray(bool obfuscate = false)
        {
            var key = WakaTimeConfigFile.ApiKey;
            var parameters = new Collection<string>
            {
                WakaTimeCli.GetCliPath(),
                "--key",
                obfuscate ? string.Format("XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXX{0}", key.Substring(key.Length - 4)) : key,
                "--file",
                File,
                "--plugin",
                Plugin
            };

            if (IsWrite)
                parameters.Add("--write");

            // ReSharper disable once InvertIf
            if (!string.IsNullOrEmpty(Project))
            {
                parameters.Add("--project");
                parameters.Add(Project);
            }

            return parameters.ToArray();
        }
    }
}
