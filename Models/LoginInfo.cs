using System.Text.Json.Serialization;

namespace CtYun
{

    public class LoginInfo
    {
        public string UserAccount { get; set; }

        [JsonPropertyName("bondedDevice")]
        public bool BondedDevice { get; set; }

        [JsonPropertyName("secretKey")]
        public string SecretKey { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("tenantId")]
        public int TenantId { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }

    }
}
