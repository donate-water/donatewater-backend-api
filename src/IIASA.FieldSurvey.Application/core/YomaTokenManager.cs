using System;
using System.Numerics;
using System.Threading.Tasks;
using Flurl.Http;
using IIASA.FieldSurvey.Config;
using Nethereum.ABI;
using Nethereum.Signer;

namespace IIASA.FieldSurvey.core;

public interface IYomaTokenManager
{
    Task<bool> CanSendYomaTokens(string requestedUser, int amount);
    Task<bool> SendYomaTokens(string requestedUser, int amount);

    Task<string> GetUserTokenBalance(string userPublicKey);
}

public class YomaTokenManager : IYomaTokenManager
{
    private readonly YomaTokenConfig _yomaTokenConfig;

    public YomaTokenManager(YomaTokenConfig yomaTokenConfig)
    {
        _yomaTokenConfig = yomaTokenConfig;
    }

    public async Task<string> GetUserTokenBalance(string userPublicKey)
    {
        var userWallet = await GetUserWallet(userPublicKey);
        return userWallet.Success ? userWallet.Wallet.Balance : "0";
    }

    public async Task<bool> CanSendYomaTokens(string requestedUser, int amount)
    {
        return await PostYomaTokens(requestedUser, amount, "/yoma/transfer/simulate");
    }
    
    public async Task<bool> SendYomaTokens(string requestedUser, int amount)
    {
        return await PostYomaTokens(requestedUser, amount, "/yoma/transfer");
    }

    private async Task<bool> PostYomaTokens(string requestedUser, int amount, string requestedEndpoint)
    {
        var userWallet = await GetUserWallet(_yomaTokenConfig.AppOwnerPublicKey);
        var nonce = int.Parse(userWallet.Wallet.Nonce);
        var signature = Signature(requestedUser, amount, nonce);
        var txData = new YomaTransaction
        {
            Sig = signature, From = _yomaTokenConfig.ProjectWalletAddress, Sender = _yomaTokenConfig.AppOwnerPublicKey,
            To = requestedUser, Amount = amount, Nonce = nonce
        };
        var res = await GetFlurlClient().Request(requestedEndpoint).PostJsonAsync(txData);
        try
        {
            res.ResponseMessage.EnsureSuccessStatusCode();
            var result = await res.GetJsonAsync<TransferResponse>();
            return result.Success;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<WalletModel> GetUserWallet(string userPublicKey)
    {
        return await GetFlurlClient().Request($"/yoma/wallets/{userPublicKey}")
            .GetJsonAsync<WalletModel>();
    }

    private string Signature(string requestedUser, int amount, int nonce)
    {
        return GetSignature(_yomaTokenConfig.ChainId, _yomaTokenConfig.ContractAddress,
            _yomaTokenConfig.AppOwnerPublicKey, _yomaTokenConfig.ProjectWalletAddress, requestedUser, amount, nonce,
            _yomaTokenConfig.AppOwnerPrivateKey);
    }

    private FlurlClient GetFlurlClient()
    {
        return 
            new FlurlClient { BaseUrl = _yomaTokenConfig.ServiceUrl }.AllowAnyHttpStatus().WithHeader("app-credentials",
                _yomaTokenConfig.AppCredentials);
    }
    
    private string GetSignature(BigInteger chainId, string contractAddress, string sender, string from, string to,
        BigInteger amount, BigInteger nonce, string privateKey)
    {
        var encoder = new ABIEncode();
        var hash = encoder.GetSha3ABIEncodedPacked(
            new ABIValue("uint256", chainId),
            new ABIValue("address", contractAddress),
            new ABIValue("address", sender),
            new ABIValue("address", from),
            new ABIValue("address", to),
            new ABIValue("uint256", amount),
            new ABIValue("uint256", nonce)
        );
        var signer = new EthereumMessageSigner();
        var ecKey = new EthECKey(privateKey);

        var signature = signer.Sign(hash, ecKey);
        return signature;
    }
}