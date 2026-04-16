using System.Text.Json.Serialization;

namespace LocalhostTunnel.Core.Configuration;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProfileType
{
    Standard = 0,
    Tavily = 1
}

