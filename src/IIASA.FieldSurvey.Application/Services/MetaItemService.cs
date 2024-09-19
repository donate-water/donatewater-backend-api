using System.Threading.Tasks;
using IIASA.FieldSurvey.Dtos;
using IIASA.FieldSurvey.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Domain.Repositories;

namespace IIASA.FieldSurvey.services;

[Route("metaitems")]
[Authorize]
public class MetaItemService : FieldSurveyAppService
{
    private readonly IRepository<MetaItem, int> _metaItemRepository;
    private readonly IRepository<QuestionItem, int> _questionItemRepository;

    public MetaItemService(IRepository<MetaItem, int> metaItemRepository,
        IRepository<QuestionItem, int> questionItemRepository)
    {
        _metaItemRepository = metaItemRepository;
        _questionItemRepository = questionItemRepository;
    }

    [HttpPost]
    public async Task PostMetaItems([FromBody] MetaItemDto[] metaItemDtos)
    {
        var entities = ObjectMapper.Map<MetaItemDto[], MetaItem[]>(metaItemDtos);
        await _metaItemRepository.InsertManyAsync(entities);
    }

    [HttpGet]
    public async Task<MetaItemDto[]> GetAllMetaItems()
    {
        var entities = await _metaItemRepository.GetListAsync();
        return ObjectMapper.Map<MetaItem[], MetaItemDto[]>(entities.ToArray());
    }

    [HttpGet("{metaKey}")]
    public async Task<MetaItemDto[]> GetMetaItems(string metaKey)
    {
        var entities = await _metaItemRepository.GetListAsync(x => x.Key == metaKey);
        return ObjectMapper.Map<MetaItem[], MetaItemDto[]>(entities.ToArray());
    }

    [HttpDelete("{metaKey}")]
    public async Task DeleteMetaItems(string metaKey)
    {
        await _metaItemRepository.DeleteAsync(x => x.Key == metaKey);
    }

    [HttpDelete("item/{id:int}")]
    public async Task DeleteMetaItem(int id)
    {
        await _metaItemRepository.DeleteAsync(x => x.Id == id);
    }

    [HttpPost("questions")]
    public async Task PostQuestionItems([FromBody] QuestionDto[] questionDtos)
    {
        var entities = ObjectMapper.Map<QuestionDto[], QuestionItem[]>(questionDtos);
        await _questionItemRepository.InsertManyAsync(entities);
    }

    [HttpGet("questions")]
    public async Task<QuestionDto[]> GetAllQuestionItems()
    {
        var entities = await _questionItemRepository.GetListAsync();
        return ObjectMapper.Map<QuestionItem[], QuestionDto[]>(entities.ToArray());
    }

    [HttpGet("questions/{metaKey}")]
    public async Task<QuestionDto[]> GetQuestionItems(string metaKey)
    {
        var entities = await _questionItemRepository.GetListAsync(x => x.Key == metaKey);
        return ObjectMapper.Map<QuestionItem[], QuestionDto[]>(entities.ToArray());
    }

    [HttpDelete("questions/item/{id:int}")]
    public async Task DeleteQuestionItem(int id)
    {
        await _questionItemRepository.DeleteAsync(x => x.Id == id);
    }
}