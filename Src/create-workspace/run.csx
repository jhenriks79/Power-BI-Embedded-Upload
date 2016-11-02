#load "../core/csx"
#load "../core/models.csx"

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Pbie.Core;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"Upload PBIX function processed a request. RequestUri={req.RequestUri}");

    try
    {
        // Validate the request
        var reqBody = await ValidateAndCreateRequestAsync(req, RequestType.Create);

        // Create the workspace
        reqBody.WorkspaceId = await PostWorkspaceAsync(reqBody.AppKey, reqBody.CollectionName);

        // Import the report and update the username/password
        var result = await ImportReportAndUpdateAsync(req, reqBody);

        return result;
    }
    catch (HttpParseException exc)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, exc.Message);
    }
    catch (Exception)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest);
    }
}