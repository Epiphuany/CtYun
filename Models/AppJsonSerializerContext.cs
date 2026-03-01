using CtYun.Models;
using System.Text.Json.Serialization;

namespace CtYun
{
    //Aot编译需要
    [JsonSerializable(typeof(ConnecMessage))]
    [JsonSerializable(typeof(ResultBase<ChallengeData>))]
    [JsonSerializable(typeof(ResultBase<ClientInfo>))]
    [JsonSerializable(typeof(ResultBase<ConnectInfo>))]
    [JsonSerializable(typeof(ResultBase<bool>))]
    [JsonSerializable(typeof(ResultBase<LoginInfo>))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
