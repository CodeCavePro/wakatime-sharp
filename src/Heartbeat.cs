using System;
using System.Runtime.Serialization;

namespace WakaTime
{
    [DataContract]
    public class Heartbeat
    {
        [DataMember(Name = "entity")]
        public string FileName { get; internal set; }

        [DataMember(Name = "timestamp")]
        public string Timestamp { get { return ToUnixEpoch(DateTime); } }

        [IgnoreDataMember]
        public DateTime DateTime { get; internal set; }

        [DataMember(Name = "project")]
        public string Project { get; internal set; }

        [DataMember(Name = "is_write")]
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
            string milliseconds = timestamp.ToString("ffffff");
            return string.Format("{0}.{1}", seconds, milliseconds);
        }
    }
}
