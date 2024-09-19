using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using IIASA.FieldSurvey.Config;
using IIASA.FieldSurvey.core;
using IIASA.FieldSurvey.Dtos;
using IIASA.FieldSurvey.Entities;
using IIASA.FieldSurvey.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace IIASA.FieldSurvey.services;

[Authorize]
public class SurveyReviewService : FieldSurveyAppService
{
    private readonly IRepository<SurveyReview, long> _reviewRepository;
    private readonly IRepository<QuestionItem, int> _questionRepo;
    private readonly IRepository<Survey, long> _surveyRepository;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IRepository<MetaItem, int> _metaItemsRepository;
    private readonly ScoreConfig _scoreConfig;
    private readonly IRepository<Image, long> _imageRepository;

    public SurveyReviewService(IRepository<SurveyReview, long> reviewRepository,
        IRepository<QuestionItem, int> questionRepo, IRepository<Survey, long> surveyRepository,
        IdentityUserManager identityUserManager, IRepository<MetaItem, int> metaItemsRepository,
        ScoreConfig scoreConfig,
        IRepository<Image, long> imageRepository)
    {
        _reviewRepository = reviewRepository;
        _questionRepo = questionRepo;
        _surveyRepository = surveyRepository;
        _identityUserManager = identityUserManager;
        _metaItemsRepository = metaItemsRepository;
        _scoreConfig = scoreConfig;
        _imageRepository = imageRepository;
    }


    [HttpPost("survey/{surveyId:long}/review")]
    public async Task SaveReview(long surveyId, [FromBody] SaveReviewDto reviewDto)
    {
        if (await _surveyRepository.AnyAsync(x => x.Id == surveyId) == false)
        {
            throw new ArgumentException("Invalid SurveyId");
        }

        if (reviewDto.SurveyRating <= 7)
        {
            reviewDto.Status = ReviewStatus.Rejected;
        }

        if (reviewDto.SurveyRating > 7)
        {
            reviewDto.Status = ReviewStatus.Completed;
        }

        var survey = await _surveyRepository.GetAsync(x => x.Id == surveyId);
        survey.Status = reviewDto.Status;

        if (await _reviewRepository.AnyAsync(x => x.Survey.Id == surveyId))
        {
            var review = await _reviewRepository.FindAsync(x => x.Survey.Id == surveyId);
            review.Status = reviewDto.Status;
            review.Comments = reviewDto.Comment;
            review.SurveyRating = reviewDto.SurveyRating;
            review.Score = reviewDto.SurveyRating * _scoreConfig.RatingToScoreFactor;
            await _reviewRepository.UpdateAsync(review);
            await _surveyRepository.UpdateAsync(survey);
            return;
        }

        var newReview = new SurveyReview
        {
            Status = reviewDto.Status, Comments = reviewDto.Comment, Survey = survey,
            SurveyRating = reviewDto.SurveyRating, Score = reviewDto.SurveyRating * _scoreConfig.RatingToScoreFactor
        };
        await _reviewRepository.InsertAsync(newReview);
        await _surveyRepository.UpdateAsync(survey);
    }


    [HttpGet("survey/review")]
    public async Task<PagedResultDto<ReviewDto>> GetSurveysForReview(PagedResultRequestDto pagedResultRequestDto,
        [FromQuery] ReviewStatus? reviewStatus, [FromQuery] Guid? userId)
    {
        List<Survey> surveys;
        var queryableAsync = await _surveyRepository.GetQueryableAsync();
        if (reviewStatus.HasValue == false && userId.HasValue == false)
        {
            surveys = queryableAsync
                .OrderByDescending(x => x.CreationTime)
                .Skip(pagedResultRequestDto.SkipCount)
                .Take(pagedResultRequestDto.MaxResultCount).ToList();
        }
        else if (reviewStatus.HasValue && userId.HasValue == false)
        {
            surveys = queryableAsync.Where(x => x.Status == reviewStatus.Value)
                .OrderByDescending(x => x.CreationTime).Skip(pagedResultRequestDto.SkipCount)
                .Take(pagedResultRequestDto.MaxResultCount).ToList();
        }
        else if (reviewStatus.HasValue == false && userId.HasValue)
        {
            surveys = queryableAsync.Where(x => x.CreatorId == userId.Value)
                .OrderByDescending(x => x.CreationTime).Skip(pagedResultRequestDto.SkipCount)
                .Take(pagedResultRequestDto.MaxResultCount).ToList();
        }
        else
        {
            surveys = queryableAsync.Where(x => x.CreatorId == userId.Value && x.Status == reviewStatus.Value)
                .OrderByDescending(x => x.CreationTime).Skip(pagedResultRequestDto.SkipCount)
                .Take(pagedResultRequestDto.MaxResultCount).ToList();
        }

        var surveyIds = surveys.Select(x => x.Id).ToArray();
        var reviews = (await _reviewRepository.GetQueryableAsync()).Where(x => surveyIds.Contains(x.Survey.Id));

        var reviewDtos = new List<ReviewDto>();
        foreach (var survey in surveys)
        {
            var creator = string.Empty;
            var creatorEmail = string.Empty;
            if (survey.CreatorId != null)
            {
                var identityUser = await _identityUserManager.GetByIdAsync(survey.CreatorId.Value);
                creator = identityUser.UserName;
                creatorEmail = identityUser.Email;
            }

            var review = new ReviewDto
            {
                SurveyId = survey.Id, SurveyCreatorId = survey.CreatorId, SurveyCreator = creator,
                SurveyCreationTime = survey.CreationTime, SurveyCreatorEmailId = creatorEmail, Status = survey.Status
            };
            if (reviews.Any(x => x.Survey.Id == survey.Id))
            {
                var reviewEntity = reviews.First(x => x.Survey.Id == survey.Id);
                review.CreateOrUpdateTime = reviewEntity.LastModificationTime ?? reviewEntity.CreationTime;
                review.LastUpdatedById = reviewEntity.LastModifierId ?? reviewEntity.CreatorId;
                if (review.LastUpdatedById != null)
                {
                    var identityUser = await _identityUserManager.GetByIdAsync(review.LastUpdatedById.Value);
                    review.LastUpdatedBy = identityUser.UserName;
                    review.LastUpdatedByEmailId = identityUser.Email;
                }

                review.Id = reviewEntity.Id;
                review.Status = reviewEntity.Status;
                review.SurveyRating = reviewEntity.SurveyRating;
                review.Score = reviewEntity.Score;
            }

            reviewDtos.Add(review);
        }

        return new PagedResultDto<ReviewDto>
            { Items = reviewDtos.ToArray(), TotalCount = await _surveyRepository.GetCountAsync() };
    }

    [HttpGet("survey/{surveyId:long}/review/next")]
    public async Task<long> GetNextSurveyForReview(long surveyId,
        [FromQuery] ReviewStatus reviewStatus = ReviewStatus.NotReviewed, [FromQuery] bool next = true)
    {
        var queryableAsync = await _surveyRepository.GetQueryableAsync();
        queryableAsync = queryableAsync.Where(x => x.Status == reviewStatus);

        queryableAsync = next switch
        {
            true => queryableAsync.OrderByDescending(x => x.CreationTime).Where(x => x.Id < surveyId),
            _ => queryableAsync.OrderBy(x => x.CreationTime).Where(x => x.Id > surveyId)
        };

        var result = queryableAsync.Select(x => x.Id).FirstOrDefault();
        if (result == default)
        {
            return surveyId;
        }

        return result;
    }

    [HttpGet("survey/{surveyId:long}/review")]
    public async Task<ReviewDetailsDto> GetSurveyForNewReview(long surveyId)
    {
        var survey = await _surveyRepository.GetAsync(x => x.Id == surveyId);
        var reviewDetailsDto = await GetReviewDetailsDto(survey);
        return reviewDetailsDto;
    }

    [HttpGet("review/data")]
    public async Task<FileContentResult> GetReviewData(Guid[] reviewerIds)
    {
        var reviews = (await _reviewRepository.GetQueryableAsync()).Where(x =>
            x.CreatorId.HasValue && reviewerIds.Contains(x.CreatorId.Value)).ToArray();

        var memoryStream = new MemoryStream();
        using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
        {
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
            {
                var strData = $"ID,CreatorId,ReviewTime,UserName,Status,Score,SurveyRating,Comments";
                csvWriter.WriteField(strData);
                foreach (var review in reviews)
                {
                    var userName = "";
                    if (review.CreatorId != null)
                    {
                        var user = await  _identityUserManager.GetByIdAsync(review.CreatorId.Value);
                        userName = user.UserName;
                    }

                    csvWriter.WriteField(
                        $"{review.Id},{review.CreatorId},{review.CreationTime},{userName},{review.Status},{review.Score},{review.SurveyRating},{review.Comments}");
                    await csvWriter.NextRecordAsync();
                }

                await csvWriter.FlushAsync();
            }
        }

        return new FileContentResult(memoryStream.ToArray(), "application/octet-stream")
            { FileDownloadName = "reviews.csv" };
    }

    private async Task<ReviewDetailsDto> GetReviewDetailsDto(Survey survey)
    {
        var surveyId = survey.Id;
        var creator = string.Empty;
        var creatorEmail = string.Empty;
        if (survey.CreatorId != null)
        {
            var identityUser = (await _identityUserManager.GetByIdAsync(survey.CreatorId.Value));
            creator = identityUser.UserName;
            creatorEmail = identityUser.Email;
        }

        var reviewDetailsDto = new ReviewDetailsDto
        {
            SurveyId = survey.Id, SurveyCreatorId = survey.CreatorId, SurveyCreator = creator,
            SurveyCreationTime = survey.CreationTime,
            SurveyCreatorEmailId = creatorEmail,
            Location = FieldSurveyApplicationAutoMapperProfile.GetCoordinates(survey.Location.Coordinates).ToArray()
        };
        reviewDetailsDto.Images = (await _imageRepository.GetListAsync(x => x.SurveyId == surveyId))
            .Select(x => new ImageDto { StorageUrl = x.Url, Data = x.Data, Id = x.Id }).ToArray();
        reviewDetailsDto.SurveyQandAs = await GetSurveyQuestions(survey.Data);
        if (await _reviewRepository.AnyAsync(x => x.Survey.Id == surveyId) == false)
        {
            return reviewDetailsDto;
        }

        var reviewEntity = await _reviewRepository.FindAsync(x => x.Survey.Id == surveyId);
        reviewDetailsDto.CreateOrUpdateTime = reviewEntity.LastModificationTime ?? reviewEntity.CreationTime;
        await UpdateReviewerDetails(reviewDetailsDto, reviewEntity);
        reviewDetailsDto.Id = reviewEntity.Id;
        reviewDetailsDto.Status = reviewEntity.Status;
        reviewDetailsDto.Comment = reviewEntity.Comments;
        reviewDetailsDto.SurveyRating = reviewEntity.SurveyRating;
        reviewDetailsDto.Score = reviewEntity.Score;
        reviewDetailsDto.Comment = reviewEntity.Comments;
        return reviewDetailsDto;
    }

    private async Task UpdateReviewerDetails(ReviewDetailsDto reviewDetailsDto, SurveyReview reviewEntity)
    {
        var userId = reviewEntity.LastModifierId ?? reviewEntity.CreatorId;
        if (userId != null)
        {
            var identityUser = await _identityUserManager.GetByIdAsync(userId.Value);
            reviewDetailsDto.LastUpdatedBy = identityUser.UserName;
            reviewDetailsDto.LastUpdatedByEmailId = identityUser.Email;
        }
    }

    private async Task<SurveyQnA[]> GetSurveyQuestions(string surveyData)
    {
        const string other = "Other";
        var metaItems = await _metaItemsRepository.ToListAsync();
        var questionItems = (await _questionRepo.GetListAsync()).ToArray();
        var answers = DataHelper.GetAllData(surveyData);
        var qas = questionItems.Select(x =>
            new SurveyQnA
            {
                Answer = DataHelper.GetValue(answers, x.Key, metaItems),
                OtherAnswer = DataHelper.GetValue(answers, x.Key + other, metaItems),
                Question = x.Question,
                Order = x.Order,
                Key = x.Key,
                Type = x.Type,
                LangCode = x.LangCode
            }
        );

        return qas.ToArray();
    }
}