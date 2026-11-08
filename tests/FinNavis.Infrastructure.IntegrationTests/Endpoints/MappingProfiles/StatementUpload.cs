using System.Net.Http.Headers;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints.MappingProfiles;

// Wraps a synthetic file the way a browser sends it: one multipart part named "file", carrying the
// file name the owner picked.
internal static class StatementUpload
{
    public const string FilePartName = "file";

    public static MultipartFormDataContent Of(byte[] file, string fileName = "test-statement.csv")
    {
        ByteArrayContent filePart = new(file);
        filePart.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        MultipartFormDataContent form = new() { { filePart, FilePartName, fileName } };

        return form;
    }
}
