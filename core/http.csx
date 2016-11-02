#r "Newtonsoft.Json"
#load "./models.csx"

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static System.String;

public static async Task<GatewayDataSource> GetGatewayDataSource(string appKey, string collectionName, string workspaceId, string datasetId)
{
    using (var client = GetHttpClient(appKey))
    {
        var response = await client.GetAsync($"https://api.powerbi.com/v1.0/collections/{collectionName}/workspaces/{workspaceId}/datasets/{datasetId}/GetBoundGatewayDatasources");
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"{response.StatusCode} - {response.ReasonPhrase}");

        var result = await response.Content.ReadAsStringAsync();
        var jsonResult = JsonConvert.DeserializeObject<GatewayDataSource>(result);

        return jsonResult;
    }
}

public static HttpClient GetHttpClient(string appKey)
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"AppKey {appKey}");

    return client;
}

public static async Task<string> GetRequestContentAsync(MultipartMemoryStreamProvider multipartFormData, string key)
{
    var content = multipartFormData.Contents.FirstOrDefault(x => x.Headers.ContentDisposition.Name.Contains(key));

    if (content == null) throw new HttpRequestException($"{key} missing from request body.");

    return await content.ReadAsStringAsync();
}

public static async Task<PbixImport> GetUploadStatusAsync(string appKey, string collectionName, string workspaceId, string uploadRequestId)
{
    using (var client = GetHttpClient(appKey))
    {
        var response = await client.GetAsync($"https://api.powerbi.com/v1.0/collections/{collectionName}/workspaces/{workspaceId}/imports/{uploadRequestId}");
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"{response.StatusCode} - {response.ReasonPhrase}");

        var result = await response.Content.ReadAsStringAsync();
        var jsonResult = JsonConvert.DeserializeObject<PbixImport>(result);

        return jsonResult;
    }
}

public static async Task<HttpResponseMessage> GetWorkspaceReportsAsync(string appKey, string collectionName, string workspaceId)
{
    using (var client = GetHttpClient(appKey))
    {
        return await client.GetAsync($"https://api.powerbi.com/v1.0/collections/{collectionName}/workspaces/{workspaceId}/reports");
    }
}

public static async Task<HttpResponseMessage> ImportReportAndUpdateAsync(HttpRequestMessage req, ReqBody reqBody)
{
    // Upload the file and get the request Id
    var uploadRequestId = await PostPbixAsync(req, reqBody.AppKey, reqBody.CollectionName, reqBody.WorkspaceId, reqBody.DatasetName, reqBody.FileBytes);

    // Check the status of the upload
    PbixImport pbixImport = null;
    for (var i = 0; i < 5; i++)
    {
        pbixImport = await GetUploadStatusAsync(reqBody.AppKey, reqBody.CollectionName, reqBody.WorkspaceId, uploadRequestId);
        if ((pbixImport != null) && pbixImport.ImportState.Equals("Succeeded", StringComparison.OrdinalIgnoreCase))
            break;

        Thread.Sleep(2500);
    }

    if (pbixImport == null) throw new HttpRequestException("Failed to import PBIX file");
    if (!pbixImport.ImportState.Equals("Succeeded", StringComparison.OrdinalIgnoreCase)) throw new HttpRequestException($"Invalid PBIX Import state - {pbixImport.ImportState}");
    ;
    var dataset = pbixImport.Datasets.FirstOrDefault(x => x.Name.Equals(reqBody.DatasetName, StringComparison.OrdinalIgnoreCase));
    if (dataset == null) throw new HttpRequestException("Failed to create dataset");

    // Get the data source gateway
    var gatewayDataSource = await GetGatewayDataSource(reqBody.AppKey, reqBody.CollectionName, reqBody.WorkspaceId, dataset.Id);

    // Change the username and password
    var changeUsernamePasswordResponse = await PatchUsernamePasswordAsync(reqBody.AppKey, reqBody.CollectionName, reqBody.WorkspaceId, gatewayDataSource.Value[0].GatewayId, gatewayDataSource.Value[0].Id, reqBody.Username, reqBody.Password);
    if (!changeUsernamePasswordResponse.IsSuccessStatusCode) return changeUsernamePasswordResponse;

    // Get the report and embed url
    var response = await GetWorkspaceReportsAsync(reqBody.AppKey, reqBody.CollectionName, reqBody.WorkspaceId);

    return response;
}

public static async Task<HttpResponseMessage> PatchUsernamePasswordAsync(string appKey, string collectionName, string workspaceId, string gatewayId, string datasourceId, string username, string password)
{
    using (var client = GetHttpClient(appKey))
    {
        var json = new
        {
            credentialType = "Basic",
            basicCredentials = new {username, password}
        };

        var content = new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"https://api.powerbi.com/v1.0/collections/{collectionName}/workspaces/{workspaceId}/gateways/{gatewayId}/datasources/{datasourceId}") {Content = content};

        return await client.SendAsync(request);
    }
}

public static async Task<string> PostPbixAsync(HttpRequestMessage req, string appKey, string collectionName, string workspaceId, string datasetName, byte[] fileBytes)
{
    using (var client = GetHttpClient(appKey))
    {
        // Upload file
        var content = new MultipartFormDataContent {new ByteArrayContent(fileBytes)};

        var response = await client.PostAsync($"https://api.powerbi.com/v1.0/collections/{collectionName}/workspaces/{workspaceId}/imports?datasetDisplayName={datasetName}", content);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"{response.StatusCode} - {response.ReasonPhrase}");

        var result = await response.Content.ReadAsStringAsync();
        var jsonResult = JsonConvert.DeserializeObject<dynamic>(result);

        if (jsonResult.id != null) return jsonResult.id;
    }

    throw new HttpRequestException("Unknown");
}

public static async Task<string> PostWorkspaceAsync(string appKey, string collectionName)
{
    using (var client = GetHttpClient(appKey))
    {
        var content = new StringContent(Empty);

        var response = await client.PostAsync($"https://api.powerbi.com/v1.0/collections/{collectionName}/workspaces", content);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"{response.StatusCode} - {response.ReasonPhrase}");

        var result = await response.Content.ReadAsStringAsync();
        var jsonResult = JsonConvert.DeserializeObject<dynamic>(result);

        if (jsonResult.workspaceId != null) return jsonResult.workspaceId;
    }

    throw new HttpRequestException("Unknown");
}

public static async Task<ReqBody> ValidateAndCreateRequestAsync(HttpRequestMessage req, RequestType reqType)
{
    if (!req.Content.Headers.ContentType.MediaType.ToLower().Contains("multipart/form-data")) throw new HttpRequestException("Invalid content-type. Content-Type should be 'multipart/form-data'");

    // Parse query parameters
    var appKey = req.GetQueryNameValuePairs().FirstOrDefault(q => Compare(q.Key, "appKey", StringComparison.OrdinalIgnoreCase) == 0).Value;
    var collectionName = req.GetQueryNameValuePairs().FirstOrDefault(q => Compare(q.Key, "collectionName", StringComparison.OrdinalIgnoreCase) == 0).Value;
    var datasetName = req.GetQueryNameValuePairs().FirstOrDefault(q => Compare(q.Key, "datasetName", StringComparison.OrdinalIgnoreCase) == 0).Value;
    var username = req.GetQueryNameValuePairs().FirstOrDefault(q => Compare(q.Key, "username", StringComparison.OrdinalIgnoreCase) == 0).Value;
    var password = req.GetQueryNameValuePairs().FirstOrDefault(q => Compare(q.Key, "password", StringComparison.OrdinalIgnoreCase) == 0).Value;

    // Get the form data
    var multipartFormData = await req.Content.ReadAsMultipartAsync();

    // Set name to query string or body data
    appKey = appKey ?? await GetRequestContentAsync(multipartFormData, "appKey");
    collectionName = collectionName ?? await GetRequestContentAsync(multipartFormData, "collectionName");
    datasetName = datasetName ?? await GetRequestContentAsync(multipartFormData, "datasetName");
    username = username ?? await GetRequestContentAsync(multipartFormData, "username");
    password = password ?? await GetRequestContentAsync(multipartFormData, "password");

    var fileContent = multipartFormData.Contents.FirstOrDefault(x => !IsNullOrWhiteSpace(x.Headers.ContentDisposition.FileName));
    if (fileContent == null) throw new HttpRequestException("File content missing from body.");

    var fileBytes = await fileContent.ReadAsByteArrayAsync();

    // Check for required parameters
    if (appKey == null) throw new HttpRequestException($"{nameof(appKey)} is required. Pass {nameof(appKey)} on the query string or in the request body");
    if (collectionName == null) throw new HttpRequestException($"{nameof(collectionName)} is required. Pass {nameof(collectionName)} on the query string or in the request body");
    if (datasetName == null) throw new HttpRequestException($"{nameof(datasetName)} is required. Pass {nameof(datasetName)} on the query string or in the request body");
    if (username == null) throw new HttpRequestException($"{nameof(username)} is required. Pass {nameof(username)} on the query string or in the request body");
    if (password == null) throw new HttpRequestException($"{nameof(password)} is required. Pass {nameof(password)} on the query string or in the request body");

    string workspaceId = null;
    if (reqType == RequestType.Import)
    {
        workspaceId = req.GetQueryNameValuePairs().FirstOrDefault(q => Compare(q.Key, "workspaceId", StringComparison.OrdinalIgnoreCase) == 0).Value;
        workspaceId = workspaceId ?? await GetRequestContentAsync(multipartFormData, "workspaceId");
        if (workspaceId == null) throw new HttpRequestException($"{nameof(workspaceId)} is required. Pass {nameof(workspaceId)} on the query string or in the request body");
    }

    return new ReqBody
    {
        AppKey = appKey,
        CollectionName = collectionName,
        DatasetName = datasetName,
        FileBytes = fileBytes,
        Password = password,
        Username = username,
        WorkspaceId = workspaceId
    };
}