#load "../core/http.csx"
#load "../core/models.csx"

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"Upload PBIX function processed a request. RequestUri={req.RequestUri}");

    try
    {
        // Validate the request
        var reqBody = await ValidateAndCreateRequestAsync(req, RequestType.Import);

        // Import the report and update the username/password
        var result = await ImportReportAndUpdateAsync(req, reqBody);

        return result;
    }
    catch (HttpRequestException exc)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, exc.Message);
    }
    catch (Exception)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest);
    }
}