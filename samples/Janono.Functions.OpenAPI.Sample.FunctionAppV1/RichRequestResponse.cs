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
    public static class RichRequestResponse
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

            var res = new RichResponse();
            return req.CreateResponse(HttpStatusCode.OK, res, "application/json");
        }
    }

    [Description("Rich Request")]
    public class RichRequest
    {
        [Required]
        [Display(Description = "String Variable")]
        public DateTimeOffset dateTimeOffsetVariable { get; set; }

        [Display(Description = "String Variable")]
        public int intVar { get; set; }

        [Display(Description = "String Variable")]
        public string stringVar { get; set; }

        [Required]
        [Annotation("format", "byte")]
        [Display(Description = "Byte data")]
        byte[] byteArray { get; set; }


        [Required]
        [Display(Description = "Uint Variable")]
        uint uIntVar { get; set; }

        [Required]
        [Display(Description = "Json data")]
        dynamic dynamicVar { get; set; }

        [AnnotationDictionary("example", "@Number", "1234567890")]
        [Display(Description = "Object, containing names and values of parameters.")]
        public Dictionary<string, object> Parameters { get; set; }

        [Range(0, 100)]
        [Display(Description = "The number of maximum returned items")]
        [Annotation("minimum", 1)]
        [Annotation("maximum", 100)]
        [Annotation("example", 10)]
        public int? MaxItemCount { get; set; }

    }

    //required?
    [Description("RichResponse")]
    public class RichResponse
    {
        [Range(0, 100)]
        [Display(Description = "The number of maximum returned items")]
        [Annotation("minimum", 1)]
        [Annotation("maximum", 100)]
        [Annotation("example", 10)]
        public int? MaxItemCount { get; set; }
    }
}
