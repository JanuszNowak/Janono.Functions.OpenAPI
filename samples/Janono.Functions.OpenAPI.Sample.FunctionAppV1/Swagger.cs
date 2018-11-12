using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http.Description;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Janono.Functions.OpenAPI.Sample.FunctionAppV1
{
    public static class Swagger
    {
        const string SwaggerFunctionName = "Swagger";

        [FunctionName(SwaggerFunctionName)]
        [ResponseType(typeof(void))]
        public static async Task<HttpResponseMessage> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous,
                System.Net.WebRequestMethods.Http.Get,System.Net.WebRequestMethods.Http.Post)] HttpRequestMessage req)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return await Janono.Functions.OpenAPI.Swagger.GetSwagger(req, assembly);
        }

        const string SwaggerFunctionNameGui = "SwaggerGui";
        [FunctionName(SwaggerFunctionNameGui)]
        [ResponseType(typeof(void))]
        public static async Task<HttpResponseMessage> RunAAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous,
                System.Net.WebRequestMethods.Http.Get,System.Net.WebRequestMethods.Http.Post)] HttpRequestMessage req)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            var sg = new Janono.Functions.OpenAPI.SwaggerGui();
            response.Content = new StringContent(sg.GetSwaggerGui(req));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }


        //ignoreatriburesoricludeenv
        const string SwaggerEnvFunctionName = "SwaggerEnv";

        [FunctionName(SwaggerEnvFunctionName)]
        [ResponseType(typeof(void))]
        public static async Task<HttpResponseMessage> RunAsyncA(
            [HttpTrigger(AuthorizationLevel.Anonymous,
                System.Net.WebRequestMethods.Http.Get,System.Net.WebRequestMethods.Http.Post)] HttpRequestMessage req)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return await Janono.Functions.OpenAPI.Swagger.GetSwagger(req, assembly);
        }
    }
}
