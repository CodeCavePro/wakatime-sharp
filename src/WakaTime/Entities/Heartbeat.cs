using System;
using System.Runtime.Serialization;

#if !NET45
using Newtonsoft.Json;
#endif

namespace WakaTime
{
    [DataContract]
    public class Heartbeat : ICloneable
    {

#if !NET45
        [JsonProperty(PropertyName = "entity")]
#else
        [DataMember(Name = "entity")]
#endif
        public string FileName { get; internal set; }

#if !NET45
        [JsonProperty(PropertyName = "timestamp")]
#else
        [DataMember(Name = "timestamp")]
#endif
        public string Timestamp { get { return ToUnixEpoch(DateTime); } }

#if !NET45
        [JsonIgnore]
#else
        [IgnoreDataMember]
#endif
        public DateTime DateTime { get; internal set; }

#if !NET45
        [JsonProperty(PropertyName = "project")]
#else
        [DataMember(Name = "project")]
#endif
        public string Project { get; internal set; }

#if !NET45
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
        object ICloneable.Clone()
        {
            return Clone();
        }

        private static string ToUnixEpoch(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var timestamp = date - epoch;
            var seconds = Convert.ToInt64(Math.Floor(timestamp.TotalSeconds));
            var milliseconds =
#if !NET45
                $"{(int)timestamp.TotalHours:00}:{timestamp.Minutes:00}:{timestamp.Seconds:00}";
#else
                timestamp.ToString("ffffff");
#endif
            return string.Format("{0}.{1}", seconds, milliseconds);
        }
    }
}
