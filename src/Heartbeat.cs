using System;
using System.Runtime.Serialization;

#if NET35
using Newtonsoft.Json;
#endif

namespace WakaTime
{
    [DataContract]
    public class Heartbeat
    {

#if NET35
        [JsonProperty(PropertyName = "entity")]
#else
        [DataMember(Name = "entity")]
#endif
        public string FileName { get; internal set; }

#if NET35
        [JsonProperty(PropertyName = "timestamp")]
#else
        [DataMember(Name = "timestamp")]
#endif
        public string Timestamp { get { return ToUnixEpoch(DateTime); } }

#if NET35
        [JsonIgnore]
#else
        [IgnoreDataMember]
#endif
        public DateTime DateTime { get; internal set; }

#if NET35
        [JsonProperty(PropertyName = "project")]
#else
        [DataMember(Name = "project")]
#endif
        public string Project { get; internal set; }

#if NET35
        [JsonProperty(PropertyName = "is_write")]
#else
        [DataMember(Name = "is_write")]
#endif
        public bool IsWrite { get; internal set; }

        public Heartbeat Clone()
        {
            return new Heartbeat
            {
                FileName = FileName,
                DateTime = DateTime,
                Project = Project,
                IsWrite = IsWrite,
            };
        }

        private static string ToUnixEpoch(DateTime date)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan timestamp = date - epoch;
            long seconds = Convert.ToInt64(Math.Floor(timestamp.TotalSeconds));
            string milliseconds =
#if NET35
                string.Format("{0:00}:{1:00}:{2:00}",
                   (int)timestamp.TotalHours,
                        timestamp.Minutes,
                        timestamp.Seconds);
#else
                timestamp.ToString("ffffff");
#endif
            return string.Format("{0}.{1}", seconds, milliseconds);
        }
    }
}
