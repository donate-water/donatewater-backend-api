using System;
using System.Linq;
using System.Threading.Tasks;
using IIASA.FieldSurvey.Entities;
using IIASA.FieldSurvey.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Volo.Abp.Domain.Repositories;

namespace IIASA.FieldSurvey.services;

[Route("leaderboard")]
public class LeaderboardService : FieldSurveyAppService
{
    private readonly IRepository<Survey, long> _surveyRepository;
    private readonly IRepository<Image, long> _imageRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly IRepository<SurveyReview, long> _reviewRepository;

    public LeaderboardService(IRepository<Survey, long> surveyRepository, IRepository<Image, long> imageRepository,
        IMemoryCache memoryCache,
        IRepository<SurveyReview, long> reviewRepository)
    {
        _surveyRepository = surveyRepository;
        _imageRepository = imageRepository;
        _memoryCache = memoryCache;
        _reviewRepository = reviewRepository;
    }

    [HttpGet("stats")]
    public async Task<Stats> GetStats()
    {
        const string memKey = "dwstats";
        if (_memoryCache.TryGetValue(memKey, out Stats result))
        {
            return result;
        }

        var userCount = (await _surveyRepository.GetQueryableAsync()).Select(x => x.CreatorId).Distinct().Count();
        var surveyCount = await _surveyRepository.CountAsync();
        var imageCount = await _imageRepository.CountAsync();
        var reviewCount = (await _surveyRepository.GetQueryableAsync()).Count(x =>
            x.Status == ReviewStatus.Completed || x.Status == ReviewStatus.Rejected);

        var stats = new Stats
            { UserCount = userCount, SurveyCount = surveyCount, ImageCount = imageCount, ReviewCount = reviewCount };
        _memoryCache.Set(memKey, stats, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5)));
        return stats;
    }

    public class Stats
    {
        public int UserCount { get; set; }
        public int SurveyCount { get; set; }
        public int ImageCount { get; set; }
        public int ReviewCount { get; set; }
    }
}