using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace IIASA.FieldSurvey.core;

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
public class WalletModel
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("wallet")]
    public Wallet Wallet { get; set; }
}

public class Wallet
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("balance")]
    public string Balance { get; set; }

    [JsonPropertyName("walletType")]
    public string WalletType { get; set; }

    [JsonPropertyName("nonce")]
    public string Nonce { get; set; }
}

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
public class YomaTransaction
{
    [JsonProperty("sender")]
    [JsonPropertyName("sender")]
    public string Sender { get; set; }

    [JsonProperty("from")]
    [JsonPropertyName("from")]
    public string From { get; set; }

    [JsonProperty("to")]
    [JsonPropertyName("to")]
    public string To { get; set; }

    [JsonProperty("amount")]
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonProperty("sig")]
    [JsonPropertyName("sig")]
    public string Sig { get; set; }
    
    [JsonProperty("nonce")]
    [JsonPropertyName("nonce")]
    public int Nonce { get; set; }
}

public class TransferResponse
{
    [JsonProperty("success")]
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}





