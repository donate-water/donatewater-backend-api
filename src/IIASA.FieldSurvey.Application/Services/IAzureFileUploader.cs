using System.IO;
using System.Threading.Tasks;

namespace IIASA.FieldSurvey.services;

public interface IAzureFileUploader
{
    Task<string> UploadFileAsync(string folderName, Stream stream, string fileName);
}