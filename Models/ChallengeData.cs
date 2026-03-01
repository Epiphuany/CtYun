using System.Text.Json.Serialization;

namespace CtYun.Models
{
    internal class ChallengeData
    {
        [JsonPropertyName("challengeId")]
        public string ChallengeId { get; set; }

        [JsonPropertyName("challengeCode")]
        public string ChallengeCode { get; set; }
    }
}
