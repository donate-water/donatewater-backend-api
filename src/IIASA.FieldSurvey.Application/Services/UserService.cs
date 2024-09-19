using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using IIASA.FieldSurvey.Config;
using IIASA.FieldSurvey.core;
using IIASA.FieldSurvey.Dtos;
using IIASA.FieldSurvey.Entities;
using IIASA.FieldSurvey.Enum;
using IIASA.FieldSurvey.OpenIddict;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace IIASA.FieldSurvey.services;

[Route("user")]
[Authorize]
public class UserService : FieldSurveyAppService
{
    private const string AdminRole = "admin";
    private readonly CurrentUser _currentUser;
    private readonly IRepository<Survey, long> _surveyRepository;
    private readonly ScoreConfig _scoreConfig;
    private readonly IRepository<Image, long> _imageRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IRepository<SurveyReview, long> _reviewRepository;
    private readonly IYomaTokenManager _yomaTokenManager;
    private readonly IRepository<Payout, long> _payoutRepository;

    public UserService(CurrentUser currentUser, IRepository<Survey, long> surveyRepository, ScoreConfig scoreConfig,
        IRepository<Image, long> imageRepository, IIdentityUserRepository userRepository,
        IdentityUserManager identityUserManager, IRepository<SurveyReview, long> reviewRepository,
        IYomaTokenManager yomaTokenManager,
        IRepository<Payout, long> payoutRepository)
    {
        _currentUser = currentUser;
        _surveyRepository = surveyRepository;
        _scoreConfig = scoreConfig;
        _imageRepository = imageRepository;
        _userRepository = userRepository;
        _identityUserManager = identityUserManager;
        _reviewRepository = reviewRepository;
        _yomaTokenManager = yomaTokenManager;
        _payoutRepository = payoutRepository;
    }

    [HttpGet]
    public Guid GetCurrentUserId()
    {
        return _currentUser.GetId();
    }

    [HttpGet("tokens/payout/status")]
    public bool GetTokenPayoutStatus()
    {
        return _scoreConfig.EnableTokensPayout;
    }

    [HttpGet("list")]
    public async Task<SurveyCreatorDto[]> GetSurveyUserList()
    {
        var userIds = (await _surveyRepository.GetQueryableAsync()).Select(x => x.CreatorId).Where(x => x.HasValue)
            .Select(x => x.Value)
            .Distinct().ToArray();

        var surveyUserList = new List<SurveyCreatorDto>();
        foreach (var userId in userIds)
        {
            var identityUser = await _identityUserManager.GetByIdAsync(userId);
            surveyUserList.Add(new SurveyCreatorDto
            {
                SurveyCreatorId = userId, SurveyCreator = identityUser.UserName,
                SurveyCreatorEmailId = identityUser.NormalizedEmail
            });
        }

        return surveyUserList.OrderBy(x=>x.SurveyCreator).ToArray();
    }

    [HttpGet("score")]
    public async Task<IActionResult> GetUserScoreDetails()
    {
        var userId = _currentUser.GetId();
        var accumulatedUserTokens =
            await _surveyRepository.CountAsync(c => c.CreatorId == userId && c.Status == ReviewStatus.Completed);

        var payouts = await GetPayouts(userId);

        var calculatedUserTokens = accumulatedUserTokens - payouts.Sum(x => x.TokensPaid);

        var payoutResults = ObjectMapper.Map<Payout[], PayoutDto[]>(payouts.ToArray());
        var userCryptoAccount = await UserCryptoAccount(userId);
        var balance = string.IsNullOrEmpty(userCryptoAccount)
            ? "0"
            : await _yomaTokenManager.GetUserTokenBalance(userCryptoAccount);
        var score = await GetUserScore(userId);
        return new OkObjectResult(new
        {
            TotalAccumulatedScore = score, Score = score, userId, userTokens = calculatedUserTokens,
            Payouts = payoutResults,
            SurveyCount = await _surveyRepository.CountAsync(survey => survey.CreatorId == userId),
            UserTokenBalance = balance,
            AcceptedSurveyCount = await _surveyRepository.CountAsync(survey =>
                survey.CreatorId == userId && survey.Status == ReviewStatus.Completed),
        });
    }

    [HttpPost("payout")]
    public async Task<IActionResult> PayoutUserTokens([FromBody] PayoutRequest payoutRequest)
    {
        var userId = _currentUser.GetId();
        var score = await GetUserScore(userId);

        var accumulatedUserTokens =
            await _surveyRepository.CountAsync(c => c.CreatorId == userId && c.Status == ReviewStatus.Completed);
        var payouts = await GetPayouts(userId);
        var calculatedUserTokensToPay = accumulatedUserTokens - payouts.Sum(x => x.TokensPaid);

        if (calculatedUserTokensToPay < payoutRequest.TokensPaid)
        {
            return new BadRequestObjectResult(
                $"Requested tokens {payoutRequest.TokensPaid} are more than the earned tokens {accumulatedUserTokens}");
        }

        var userCryptoAccount = await UserCryptoAccount(userId);
        if (await _yomaTokenManager.CanSendYomaTokens(userCryptoAccount, payoutRequest.TokensPaid) == false)
        {
            return new BadRequestObjectResult(
                $"Requested tokens {payoutRequest.TokensPaid} cannot to transferred to yoma account. Contact Admin");
        }

        if (!await _yomaTokenManager.SendYomaTokens(userCryptoAccount, payoutRequest.TokensPaid))
        {
            return new BadRequestObjectResult($"Failed to transfer requested tokens {payoutRequest.TokensPaid}");
        }

        var newPayout = new Payout
        {
            UserId = userId, DeductedScore = 0, TokensPaid = payoutRequest.TokensPaid
        };

        var entity = await _payoutRepository.InsertAsync(newPayout);
        payouts.Add(entity);
        var payoutResults = ObjectMapper.Map<Payout[], PayoutDto[]>(payouts.ToArray());

        return new OkObjectResult(new
        {
            TotalAccumulatedScore = score, Score = score, userId, userTokens = 0, Payouts = payoutResults,
            SurveyCount = await _surveyRepository.CountAsync(c => c.CreatorId == userId)
        });
    }

    [HttpDelete("data/delete")]
    public async Task DeleteCurrentUserAndData([FromQuery] Guid? userId)
    {
        var isAdmin = await IsUserAdmin();
        if (isAdmin)
        {
            await DeleteUser(userId);
        }

        var currentUserId = _currentUser.GetId();
        await DeleteUser(currentUserId);
    }

    private async Task DeleteUser(Guid? userId)
    {
        if (userId != null)
        {
            var user = await _identityUserManager.GetByIdAsync(userId.Value);
            await _imageRepository.DeleteAsync(x => x.CreatorId == userId);
            await _surveyRepository.DeleteAsync(x => x.CreatorId == userId);
            await _identityUserManager.DeleteAsync(user);
        }
    }

    [HttpGet("details")]
    public async Task<IActionResult> GetUserDetailsFrom([FromQuery] ReviewerDto reviewerDto)
    {
        var isAdmin = await IsUserAdmin();
        if (isAdmin == false)
        {
            return new BadRequestObjectResult("User is not Admin, Only admin is allowed to assign roles");
        }

        IdentityUser identityUser = default;
        try
        {
            identityUser = await _userRepository.FindByNormalizedUserNameAsync(reviewerDto.UserName.ToUpperInvariant());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        if (identityUser == default)
        {
            try
            {
                identityUser =
                    await _userRepository.FindByNormalizedEmailAsync(reviewerDto.UserEmailId.ToUpperInvariant());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        if (identityUser == default)
        {
            return new NotFoundResult();
        }

        var userRoles = await _identityUserManager.GetRolesAsync(identityUser);
        return new OkObjectResult(new { identityUser.Id, identityUser.UserName, identityUser.Email, userRoles });
    }

    [HttpPost("crypto/address")]
    public async Task<IActionResult> SetUserCryptoAccountAddress([FromBody] CryptoAccount account)
    {
        var currentUserId = _currentUser.GetId();
        var user = await _identityUserManager.GetByIdAsync(currentUserId);
        user.SetUserCryptoAccount(account.Address);
        await _identityUserManager.UpdateAsync(user);
        return new OkResult();
    }

    [HttpGet("crypto/address")]
    public async Task<CryptoAccount> GetUserCryptoAddress()
    {
        var currentUserId = _currentUser.GetId();
        var userCryptoAccount = await UserCryptoAccount(currentUserId);
        return new CryptoAccount { Address = userCryptoAccount };
    }

    private async Task<string> UserCryptoAccount(Guid currentUserId)
    {
        var user = await _identityUserManager.GetByIdAsync(currentUserId);
        var userCryptoAccount = user.GetUserCryptoAccount();
        return userCryptoAccount;
    }

    private async Task<bool> IsUserAdmin()
    {
        var adminId = _currentUser.GetId();
        var admin = await _identityUserManager.GetByIdAsync(adminId);
        var roles = await _identityUserManager.GetRolesAsync(admin);
        var isAdmin = roles.Contains(AdminRole);
        return isAdmin;
    }

    private async Task<List<Payout>> GetPayouts(Guid userId)
    {
        return (await _payoutRepository.GetQueryableAsync()).Where(x => x.UserId == userId).ToList();
    }

    private async Task<int> GetUserScore(Guid userId)
    {
        return (await _reviewRepository.GetQueryableAsync())
            .Where(x => x.Status == ReviewStatus.Completed && x.Survey.CreatorId == userId).Sum(x => x.Score);
    }
}