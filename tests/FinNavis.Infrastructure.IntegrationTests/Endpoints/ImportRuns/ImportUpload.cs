using System.Net.Http.Headers;

namespace FinNavis.Infrastructure.IntegrationTests.Endpoints.ImportRuns;

// Wraps a synthetic file the way a browser sends it: a multipart part named "file" carrying the
// file name the owner picked, plus the account a statement is loaded onto.
internal static class ImportUpload
{
    public static MultipartFormDataContent Statement(byte[] file, Guid accountId, string fileName = "test-statement.csv")
    {
        MultipartFormDataContent form = Export(file, fileName);
        form.Add(new StringContent(accountId.ToString()), "account_id");

        return form;
    }

    public static MultipartFormDataContent Export(byte[] file, string fileName = "test-export.csv")
    {
        ByteArrayContent filePart = new(file);
        filePart.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        return new MultipartFormDataContent { { filePart, "file", fileName } };
    }
}
