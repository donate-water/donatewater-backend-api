using System.Text.Json.Serialization;

namespace IIASA.FieldSurvey.Config;

public class AzureConfig
{
    public string StorageConnectionStrings { get; set; }
    public string ContainerName { get; set; }
    public string DataProtectionContainer { get; set; }
    public string DataProtectionFileName { get; set; }
}

public class ScoreConfig
{
    public int RatingToScoreFactor { get; set; }

    public double ScoreToTokenFactor { get; set; }
    public int MinScoreForPayout { get; set; }

    public bool EnableTokensPayout { get; set; }
}

public class YomaTokenConfig
{
    [JsonPropertyName("ServiceUrl")]
    public string ServiceUrl { get; set; }
    
    [JsonPropertyName("AppCredentials")]
    public string AppCredentials { get; set; }

    [JsonPropertyName("AppOwnerPublicKey")]
    public string AppOwnerPublicKey { get; set; }

    [JsonPropertyName("AppOwnerPrivateKey")]
    public string AppOwnerPrivateKey { get; set; }

    [JsonPropertyName("ContractAddress")]
    public string ContractAddress { get; set; }

    [JsonPropertyName("ProjectWalletAddress")]
    public string ProjectWalletAddress { get; set; }
    
    [JsonPropertyName("ChainId")]
    public int ChainId { get; set; }
}