using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Description;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Janono.Functions.OpenAPI.Sample.FunctionAppV1
{
    public static class Calculator
    {
        private const string FunctionName = "RichRequestResponseFunction";

        [AnnotationParameter("x-functions-key", AnnotationParameter.ParameterInEnum.header, "Functions key", true, typeof(string))]


        [HttpProduceResponse(HttpStatusCode.Forbidden, "Forbidden, this call requires valid Function Key")]
        [HttpProduceResponse(HttpStatusCode.BadRequest, "Message is in invalid ")]
        [HttpProduceResponse(HttpStatusCode.InternalServerError, "InternalServerError")]
        [HttpProduceResponse(HttpStatusCode.Unauthorized, "Please authenticate with valid account.")]
        [ResponseType(typeof(RichResponse))]
        [HttpProduceResponse(HttpStatusCode.OK, "Succeeded.", typeof(RichResponse))]
        [Display(Name = FunctionName, Description = FunctionName)]
        [FunctionName(FunctionName)]

        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous,
            WebRequestMethods.Http.Post,Route =null)]
            [HttpTriggerBodyType(typeof(RichRequest))]
            HttpRequestMessage req,
            TraceWriter log)
        //ILogger log)
        {

            RichRequest data = await req.Content.ReadAsAsync<RichRequest>();
            var res = new RichResponse();
            res.Result = data.FirstNumber + data.SecondNumnber;
            return req.CreateResponse(HttpStatusCode.OK, res, "application/json");
        }
    }

    [Description("Rich Request")]
    public class RichRequest
    {  
        [Range(0, 100)]
        [Display(Description = "The number of maximum returned items")]
        [Annotation("minimum", 1)]
        [Annotation("maximum", 100)]
        [Annotation("example", 10)]
        public int FirstNumber{ get; set; }

        [Range(0, 100)]
        [Display(Description = "The number of maximum returned items")]
        [Annotation("minimum", 1)]
        [Annotation("maximum", 100)]
        [Annotation("example", 10)]
        public int SecondNumnber { get; set; }
    }

    //required?
    [Description("RichResponse")]
    public class RichResponse
    {        
        [Display(Description = "The number of maximum returned items")]
        [Annotation("example", 10)]
        public int Result { get; set; }
    }
}
