using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CtYun.Models
{
    public class ClientInfo
    {
        [JsonPropertyName("desktopList")]
        public List<Desktop> DesktopList { get; set; }
    }


    public class Desktop
    {
        [JsonPropertyName("desktopId")]
        public string DesktopId { get; set; }

        [JsonPropertyName("desktopName")]
        public string DesktopName { get; set; }

        [JsonPropertyName("desktopCode")]
        public string DesktopCode { get; set; }

        public DesktopInfo DesktopInfo { get; set; }

        [JsonPropertyName("useStatusText")]
        public string UseStatusText { get; set; }
    }
}
