using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ElectronicCertificateUpload.Core
{
    internal class LogMessageKind
    {
        [JsonProperty(PropertyName = "time", Order = 1)]
        internal string Time => DateTime.Now.ToString("G");

        [JsonProperty(PropertyName = "level", Order = 2)]
        internal string Level { get; set; }

        [JsonProperty(PropertyName = "query_comment", Order = 5)]
        internal string QueryComment { get; set; }

        [JsonProperty(PropertyName = "query_status", Order = 5)]
        internal string QueryStatus { get; set; }

        [JsonProperty(PropertyName = "class_name", Order = 3)]
        internal string ClassName { get; set; }

        [JsonProperty(PropertyName = "function_name", Order = 4)]
        internal string FunctionName { get; set; }

        [JsonProperty(PropertyName = "error_message", Order = 6)]
        internal string ErrorMessage { get; set; }
    }
}
