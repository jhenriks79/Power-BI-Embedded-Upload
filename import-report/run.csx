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
    log.Info($"Import report function processed a request. RequestUri={req.RequestUri}");

    try
    {
        // Validate the request
        var reqBody = await ValidateAndCreateRequestAsync(req, RequestType.Import);
        log.Info($"Request validated with the following body: {JsonConvert.SerializeObject(reqBody)}");

        // Import the report and update the username/password
        var result = await ImportReportAndUpdateAsync(req, reqBody);
        log.Info($"Import report function successful. Result={result}");
        
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