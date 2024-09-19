using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IIASA.FieldSurvey.core;
using IIASA.FieldSurvey.Dtos;
using IIASA.FieldSurvey.Entities;
using IIASA.FieldSurvey.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace IIASA.FieldSurvey.services;

[Route("survey")]
[Authorize]
public class SurveyService : FieldSurveyAppService
{
    private readonly IRepository<Survey, long> _surveyRepository;
    private readonly IAzureFileUploader _azureFileUploader;
    private readonly IRepository<Image, long> _imageRepository;

    public SurveyService(IRepository<Survey, long> surveyRepository, IAzureFileUploader azureFileUploader,
        IRepository<Image, long> imageRepository)
    {
        _surveyRepository = surveyRepository;
        _azureFileUploader = azureFileUploader;
        _imageRepository = imageRepository;
    }

    [HttpPost]
    public async Task<long> CreateSurvey([FromBody] SurveyDto surveyDto)
    {
        if (string.IsNullOrWhiteSpace(surveyDto.Data))
        {
            throw new ApplicationException("Data Filed is empty");
        }

        if (DataHelper.GetAllData(surveyDto.Data).Any() == false)
        {
            throw new ApplicationException("Parsed Data is empty");
        }

        var survey = ObjectMapper.Map<SurveyDto, Survey>(surveyDto);
        var result = await _surveyRepository.InsertAsync(survey, true);
        return result.Id;
    }

    [HttpGet]
    public async Task<PagedResultDto<SurveyDtoForGet>> GetSurveys(PagedResultRequestDto request)
    {
        var queryableAsync = await _surveyRepository.GetQueryableAsync();
        var surveys = queryableAsync.OrderBy(x => x.Id).Skip(request.SkipCount).Take(request.MaxResultCount).ToArray();
        var items = ObjectMapper.Map<Survey[], SurveyDtoForGet[]>(surveys);
        foreach (var surveyDtoForGet in items)
        {
            surveyDtoForGet.Images = await DataHelper.GetImageUrls(surveyDtoForGet.Id, _imageRepository);
        }

        long total = await _surveyRepository.CountAsync();
        return new PagedResultDto<SurveyDtoForGet> { Items = items, TotalCount = total };
    }

    [HttpGet("map")]
    public async Task<PagedResultDto<SurveyMapDto>> GetSurveysForMap(PagedResultRequestDto request,
        [FromQuery] string[] properties)
    {
        var queryableAsync = await _surveyRepository.GetQueryableAsync();
        var surveys = queryableAsync.Where(x => x.Status != ReviewStatus.Rejected).OrderBy(x => x.Id)
            .Skip(request.SkipCount).Take(request.MaxResultCount).ToArray();
        var items = new List<SurveyMapDto>();
        foreach (var survey in surveys)
        {
            var surveyMapDto = await GetSurveyMapDto(properties, survey);
            AddMarker(items, surveyMapDto);
        }

        long total = await _surveyRepository.CountAsync();
        return new PagedResultDto<SurveyMapDto> { Items = items, TotalCount = total };
    }

    private static void AddMarker(List<SurveyMapDto> items, SurveyMapDto surveyMapDto)
    {
        if (items.Any(x => AreSame(x.Location, surveyMapDto.Location)))
        {
            surveyMapDto.Location = new Coordinate[]
            {
                new() { XLng = surveyMapDto.Location[0].XLng + 0.001, YLat = surveyMapDto.Location[0].YLat + 0.001 }
            };
        }

        items.Add(surveyMapDto);
    }

    private static bool AreSame(Coordinate[] first, Coordinate[] second)
    {
        if (first.Length != 1 && second.Length != 1)
        {
            return false;
        }

        const double precessionValue = 0.00001;
        return Math.Abs(first[0].XLng - second[0].XLng) < precessionValue &&
               Math.Abs(first[0].YLat - second[0].YLat) < precessionValue;
    }

    [HttpGet("{surveyId:long}")]
    public async Task<SurveyDtoForGet> GetSurveyWithId(long surveyId)
    {
        var survey = await GetSurvey(surveyId);
        var surveyDto = ObjectMapper.Map<Survey, SurveyDtoForGet>(survey);
        surveyDto.Images = await DataHelper.GetImageUrls(surveyId, _imageRepository);
        return surveyDto;
    }

    [HttpPost("{surveyId:long}/image")]
    public async Task<long> AddImage(long surveyId, IFormFile imageFile, [FromForm] string data)
    {
        try
        {
            var survey = await GetSurvey(surveyId);
            var image = await AddImage(imageFile, data ?? string.Empty, survey.Id);
            var result = await _imageRepository.InsertAsync(image, true);
            return result.Id;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [HttpDelete("{surveyId:long}")]
    public async Task<bool> Delete(long surveyId)
    {
        try
        {
            await _imageRepository.DeleteAsync(x => x.SurveyId == surveyId);
            await _surveyRepository.DeleteAsync(x => x.Id == surveyId);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    [HttpDelete("{surveyId:long}/image/{imageId:long}")]
    public async Task<bool> DeleteImage(long surveyId, long imageId)
    {
        try
        {
            await GetSurvey(surveyId);
            await _imageRepository.DeleteAsync(x => x.Id == imageId);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    private async Task<SurveyMapDto> GetSurveyMapDto(string[] properties, Survey survey)
    {
        var surveyMapDto = new SurveyMapDto
        {
            Id = survey.Id,
            CreationTime = survey.CreationTime,
            Status = survey.Status
        };
        surveyMapDto.Images = await DataHelper.GetImageUrls(surveyMapDto.Id, _imageRepository);
        surveyMapDto.Data = DataHelper.GetData(survey.Data, properties);
        surveyMapDto.Location = FieldSurveyApplicationAutoMapperProfile.GetCoordinates(survey.Location.Coordinates)
            .ToArray();
        return surveyMapDto;
    }

    private async Task<Image> AddImage(IFormFile imageFile, string extData, long surveyId)
    {
        await using var fileStream = imageFile.OpenReadStream();
        var url = await _azureFileUploader.UploadFileAsync(surveyId.ToString(), fileStream, imageFile.FileName);
        await fileStream.DisposeAsync();
        return new Image { Url = url, Data = extData, SurveyId = surveyId };
    }

    private async Task<Survey> GetSurvey(long surveyId)
    {
        var queryResult = await _surveyRepository.FirstOrDefaultAsync(x => x.Id == surveyId);
        if (queryResult == default)
        {
            throw new EntityNotFoundException("Survey not found!");
        }

        return queryResult;
    }
}