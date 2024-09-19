using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IIASA.FieldSurvey.Entities;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace IIASA.FieldSurvey;

public class DataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<MetaItem, int> _metaItemRepository;
    private readonly IRepository<QuestionItem, int> _questionRepository;

    public DataSeedContributor(IRepository<MetaItem, int> metaItemRepository,
        IRepository<QuestionItem, int> questionRepository)
    {
        _metaItemRepository = metaItemRepository;
        _questionRepository = questionRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        Console.WriteLine("Seeding metadata item Values");
        var allItems = await _metaItemRepository.GetListAsync();
        var files = Directory.GetFiles(".\\metadata", "*.txt");
        foreach (var file in files)
        {
            var key = file.Split("\\").Last().Replace(".txt", string.Empty);
            await SeedMetadata(file, key, allItems);
        }

        Console.WriteLine("Completed Seeding metadata item Values");
        Console.WriteLine("Seeding questions");
        if (await _questionRepository.AnyAsync() == false)
        {
            var questions = Directory.GetFiles(".\\questions", "*.txt")
                .SelectMany(x => File.ReadAllLines(x)
                    .Select(y => QuestionItem(y.Split(",")))).ToArray();
            await _questionRepository.InsertManyAsync(questions);
        }
        else
        {
            Console.WriteLine("Skipping Seeding questions");
        }
    }

    private static QuestionItem QuestionItem(IReadOnlyList<string> strings)
    {
        return new QuestionItem
        {
            Order = int.Parse(strings[0]), Key = strings[1], LangCode = "en", Type = strings[2],
            Question = strings[3].Replace(";", ",")
        };
    }

    private async Task SeedMetadata(string fileName, string key, IEnumerable<MetaItem> metaItems)
    {
        var data = GetStore(fileName);
        if (metaItems.Any(x => x.Key == key))
        {
            Console.WriteLine($"Skipping seed for key: {key}");
            return;
        }

        var entities = data.Keys.Select(x => new MetaItem
            { Index = x, IndexValue = data[x], Key = key });
        await _metaItemRepository.InsertManyAsync(entities);
    }

    private static Dictionary<int, string> GetStore(string fileName)
    {
        var list = File.ReadAllLines(fileName);
        var store = new Dictionary<int, string>();
        foreach (var item in list)
        {
            var values = item.Split(",");
            store.Add(int.Parse(values[0]), values[1].Replace(";", ","));
        }

        return store;
    }
}