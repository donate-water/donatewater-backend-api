using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IIASA.FieldSurvey.Dtos;
using IIASA.FieldSurvey.Entities;
using Newtonsoft.Json;
using Volo.Abp.Domain.Repositories;

namespace IIASA.FieldSurvey.core;

public static class DataHelper
{
    public static async Task<ImageDto[]> GetImageUrls(long surveyId, IRepository<Image, long> imageRepository)
    {
        var queryable = await imageRepository.GetQueryableAsync();
        var urls = queryable.Where(x => x.SurveyId == surveyId).Select(x => new ImageDto{StorageUrl = x.Url, Data = x.Data, Id = x.Id}).ToArray();
        return urls;
    }

    public static IDictionary<string, string> GetData(string encodedData, IEnumerable<string> properties)
    {
        var data = new Dictionary<string, string>();
        try
        {
            var decoded = WebUtility.UrlDecode(encodedData);
            dynamic jsonArray = JsonConvert.DeserializeObject(decoded);
            if (jsonArray == null)
            {
                return data;
            }

            foreach (var property in properties)
            {
                var value = jsonArray[property];
                if (value == null || data.ContainsKey(property))
                {
                    continue;
                }

                data.Add(property, value.ToString());
            }

            return data;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return data;
        }
    }

    public static IDictionary<string, string> GetAllData(string encodedData)
    {
        var data = new Dictionary<string, string>();
        var decoded = WebUtility.UrlDecode(encodedData);
        var deserializeObject = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(decoded);
        foreach (var keyValue in deserializeObject)
        {
            if (keyValue.Value is not null)
            {
                data.Add(keyValue.Key, keyValue.Value.ToString());
            }
        }

        return data;
    }

    public static string GetValue(IDictionary<string, string> decodedData, string key, List<MetaItem> metaItems)
    {
        if (decodedData.ContainsKey(key) == false)
        {
            return string.Empty;
        }

        var decodedValue = decodedData[key];
        if (metaItems.Any(x => x.Key == key) == false)
        {
            return decodedValue;
        }

        var value = int.Parse(decodedValue.Trim());
        var item = metaItems.FirstOrDefault(x => x.Key == key && x.Index == value);
        return item == default ? decodedValue : item.IndexValue;
    }
}