using System.Net.Http;

namespace Janono.Functions.OpenAPI
{
    public class SwaggerGui
    {
        private string org =
            "var configObject = JSON.parse(\'{\"urls\":[{\"url\":\"/api/swagger\",\"name\":\"OneCustomer V1\"}],\"validatorUrl\":null}\');";

        public string GetSwaggerGui(HttpRequestMessage par)
        {
            string newval = org.Replace("/api/swagger", "/api/swagger" + par.RequestUri.Query);
            newval = newval.Replace("OneCustomer V1", "app");

            return SwaggerGuiConst.a1.Replace(org, newval);
        }
    }
}
