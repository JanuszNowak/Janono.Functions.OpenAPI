using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http.Description;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace Janono.Functions.OpenAPI.Sample.FunctionAppV1
{
    [Description("Is Alive Contract")]
    public class IsAliveContract
    {
        [Display(Description = "Version of Application")]
        public string version { get; set; }

        [Display(Description = "TimeStamp")]
        public DateTimeOffset timestamp { get; set; }

        [Display(Description = "Is Alive Value")]
        public bool isAlive { get; set; }

        [Display(Description = "Region name")]
        public string regionName { get; set; }
    }

    public static class IsAlive
    {
        public static readonly string ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        const string IsAliveFunctionName = "IsAlive";
        [FunctionName(IsAliveFunctionName)]
        [ResponseType(typeof(IsAliveContract))]
        public static async Task<HttpResponseMessage> Run
        (
            [HttpTrigger(AuthorizationLevel.Function, WebRequestMethods.Http.Get)]HttpRequestMessage req,
            TraceWriter log
        )
        {

            var result = new IsAliveContract
            {
                isAlive = true,
                timestamp = DateTimeOffset.UtcNow,
                version = ApplicationVersion,
                regionName = Environment.GetEnvironmentVariable("REGION_NAME")
            };

            return req.CreateResponse(HttpStatusCode.OK, result, "application/json");
        }
    }
}
