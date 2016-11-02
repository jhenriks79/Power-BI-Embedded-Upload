#r "Newtonsoft.Json"
#load "../core/http.csx"
#load "../core/models.csx"

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"Create workspace function processed a request. RequestUri={req.RequestUri}");

    try
    {
        // Validate the request
        var reqBody = await ValidateAndCreateRequestAsync(req, RequestType.Create);
        log.Info($"Request validated with the following body: {JsonConvert.SerializeObject(reqBody, Formatting.Indented)}");

        // Create the workspace
        reqBody.WorkspaceId = await PostWorkspaceAsync(reqBody.AppKey, reqBody.CollectionName);
        log.Info($"Workspace created with WorkspaceId={reqBody.WorkspaceId}");

        // Import the report and update the username/password
        var result = await ImportReportAndUpdateAsync(req, reqBody);
        log.Info($"Create workspace function successful. Result={result}");

        return result;
    }
    catch (HttpRequestException exc)
    {
        log.Info($"HttpRequestException: {exc}");
        return req.CreateResponse(HttpStatusCode.BadRequest, exc.Message);
    }
    catch (Exception exc)
    {
        log.Info($"Exception: {exc}");
        return req.CreateResponse(HttpStatusCode.BadRequest);
    }
}