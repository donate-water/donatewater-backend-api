using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using IIASA.FieldSurvey.core;
using IIASA.FieldSurvey.Dtos;
using IIASA.FieldSurvey.Entities;
using IIASA.FieldSurvey.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace IIASA.FieldSurvey.services;

[Route("data")]
[Authorize]
public class DataService : FieldSurveyAppService
{
    private readonly IRepository<Survey, long> _surveyRepository;
    private readonly IRepository<Image, long> _imageRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IRepository<MetaItem, int> _metaItemRepository;
    private readonly IRepository<SurveyReview, long> _reviewRepository;

    public DataService(IRepository<Survey, long> surveyRepository, IRepository<Image, long> imageRepository,
        IMemoryCache memoryCache, IdentityUserManager identityUserManager,
        IRepository<MetaItem, int> metaItemRepository, IRepository<SurveyReview, long> reviewRepository)
    {
        _surveyRepository = surveyRepository;
        _imageRepository = imageRepository;
        _memoryCache = memoryCache;
        _identityUserManager = identityUserManager;
        _metaItemRepository = metaItemRepository;
        _reviewRepository = reviewRepository;
    }

    [HttpGet("/leaderboard")]
    public async Task<UserScore[]> GetLeaderBoard(PagedResultRequestDto pagedResultRequestDto)
    {
        return await LeaderBoard(pagedResultRequestDto);
    }

    [HttpGet("/leaderboard/csv")]
    public async Task<IActionResult> GetLeaderBoardCsv(PagedResultRequestDto pagedResultRequestDto)
    {
        var results = await LeaderBoard(pagedResultRequestDto);
        var memoryStream = new MemoryStream();
        using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
        {
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
            {
                csvWriter.WriteField(
                    "Rank,UserName,EmailId,Score,SurveyCount,AcceptedSurveyCount,RejectedSurveyCount,ImagesUploaded");
                foreach (var result in results)
                {
                    await csvWriter.NextRecordAsync();
                    csvWriter.WriteField(
                        $"{result.Rank},{result.UserName},{result.UserEmail},{result.Score},{result.SurveyCount},{result.AcceptedSurveyCount},{result.RejectedSurveyCount},{result.TotalImagesUploadedCount}");
                }

                await csvWriter.FlushAsync();
            }
        }

        return new FileContentResult(memoryStream.ToArray(), "application/octet-stream")
            { FileDownloadName = "leaderboard.csv" };
    }


    [HttpGet("download/csv")]
    public async Task<IActionResult> DownlaodCsv(PagedResultRequestDto request, [FromQuery] Guid[] userIds)
    {
        var queryableAsync = await _surveyRepository.GetQueryableAsync();
        if (userIds.Length > 0)
        {
            queryableAsync = queryableAsync.Where(x => x.CreatorId.HasValue && userIds.Contains(x.CreatorId.Value));
        }

        var surveys = queryableAsync.OrderBy(x => x.Id).Skip(request.SkipCount).Take(request.MaxResultCount).ToArray();
        var surveyIds = surveys.Select(x => x.Id);
        var imagesList = await _imageRepository.GetListAsync(x => surveyIds.Contains(x.SurveyId));
        var metaItems = await _metaItemRepository.GetListAsync();

        if (surveys.Any() == false)
        {
            return new NotFoundResult();
        }

        var memoryStream = new MemoryStream();
        var keys = Array.Empty<string>();
        using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
        {
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
            {
                var index = 0;
                foreach (var survey in surveys)
                {
                    if (survey.Data.Length < 200)
                    {
                        continue;
                    }

                    var images = string.Join(";", imagesList.Where(x => x.SurveyId == survey.Id).Select(x => x.Url));
                    var extraData = DataHelper.GetAllData(survey.Data);
                    if (keys.Length == 0)
                    {
                        keys = extraData.Keys.ToArray();
                    }

                    var stringValues = keys
                        .Select(x =>
                            DataHelper.GetValue(extraData, x, metaItems)
                                .Replace(",", ";"))
                        .ToArray();

                    if (index == 0)
                    {
                        var strData = $"ID,CreatorId,Location,{string.Join(",", keys)},ImageUrls";
                        csvWriter.WriteField(strData);
                        await csvWriter.NextRecordAsync();
                    }

                    csvWriter.WriteField(
                        $"{survey.Id},{survey.CreatorId},{GetCsvString(survey.Location.ToText())},{string.Join(",", stringValues)},{images}");
                    await csvWriter.NextRecordAsync();
                    index++;
                }

                await csvWriter.FlushAsync();
            }
        }

        return new FileContentResult(memoryStream.ToArray(), "application/octet-stream")
            { FileDownloadName = "surveys.csv" };
    }

    [HttpGet("download/geojson")]
    public async Task<IActionResult> GetSurveyGeojson(PagedResultRequestDto request)
    {
        var queryableAsync = await _surveyRepository.GetQueryableAsync();
        var surveys = queryableAsync.OrderBy(x => x.Id).Skip(request.SkipCount).Take(request.MaxResultCount).ToArray();
        var surveyIds = surveys.Select(x => x.Id);
        var imagesList = await _imageRepository.GetListAsync(x => surveyIds.Contains(x.SurveyId));
        var metaItems = await _metaItemRepository.GetListAsync();

        var featureCollection = new FeatureCollection();

        foreach (var survey in surveys)
        {
            if (survey.Data.Length < 200)
            {
                continue;
            }

            var images = string.Join(";", imagesList.Where(x => x.SurveyId == survey.Id).Select(x => x.Url));
            var extraData = DataHelper.GetAllData(survey.Data);

            var email = string.Empty;
            if (survey.CreatorId != null)
            {
                email = (await _identityUserManager.GetByIdAsync(survey.CreatorId.Value)).Email;
            }

            var attributesTable = new AttributesTable(extraData.Select(x =>
                new KeyValuePair<string, object>(x.Key, DataHelper.GetValue(extraData, x.Key, metaItems))))
            {
                { "Id", survey.Id },
                { "Status", survey.Status.ToString("G") },
                { "ImageUrls", images },
                { "UserEmail", email }
            };
            featureCollection.Add(new Feature(survey.Location, attributesTable));
        }

        var geoJsonString = GetGeoJsonString(featureCollection);
        dynamic jObject = JsonConvert.DeserializeObject(geoJsonString);
        return new ContentResult
        {
            Content = JsonConvert.SerializeObject(jObject), ContentType = "application/json",
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    private async Task<UserScore[]> LeaderBoard(PagedResultRequestDto pagedResultRequestDto)
    {
        var leader = $"leader{pagedResultRequestDto.SkipCount}{pagedResultRequestDto.MaxResultCount}";
        if (_memoryCache.TryGetValue(leader, out UserScore[] results))
        {
            return results;
        }

        var queryableAsync = await _reviewRepository.GetQueryableAsync();
        var scores = queryableAsync.GroupBy(x => x.Survey.CreatorId).Select(x =>
                new UserScore
                {
                    UserId = x.Key.Value, Score = x.Sum(y => y.Score)
                })
            .OrderByDescending(x => x.Score).Skip(pagedResultRequestDto.SkipCount)
            .Take(pagedResultRequestDto.MaxResultCount).ToArray();

        var position = 1;
        foreach (var userScore in scores)
        {
            userScore.SurveyCount = await _surveyRepository.CountAsync(x => x.CreatorId == userScore.UserId);
            userScore.AcceptedSurveyCount = await _surveyRepository.CountAsync(x =>
                x.CreatorId == userScore.UserId && x.Status == ReviewStatus.Completed);
            userScore.RejectedSurveyCount = await _surveyRepository.CountAsync(x =>
                x.CreatorId == userScore.UserId && x.Status == ReviewStatus.Rejected);
            userScore.Rank = position++;
            var user = await _identityUserManager.GetByIdAsync(userScore.UserId);
            userScore.UserName = user.UserName;
            userScore.UserEmail = user.Email;
            userScore.TotalImagesUploadedCount =
                await _imageRepository.CountAsync(x => x.CreatorId == userScore.UserId);
        }

        _memoryCache.Set(leader, scores, new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(10)));

        return results;
    }

    public static string GetGeoJsonString(object featureCollection)
    {
        var stringBuilder = new StringBuilder();
        using (var writer = new StringWriter(stringBuilder))
        {
            var serializer = GeoJsonSerializer.Create();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.Serialize(writer, featureCollection);
            writer.Flush();
        }

        return stringBuilder.ToString();
    }

    private static string GetCsvString(string value)
    {
        return $"\"{value}\"";
    }
}