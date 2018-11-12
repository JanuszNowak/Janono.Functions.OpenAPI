using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Janono.Functions.OpenAPI
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class AnnotationSecurityDefinitionOAuth : Attribute
    {
        public AnnotationSecurityDefinitionOAuth(
            string name = "oauth",
            string type = "oauth2",
            string flow = "accessCode",
            string authorizationUrl = "https://login.microsoftonline.com/guid/oauth2/authorize?client_id=guid",
            string tokenUrl = "https://login.microsoftonline.com/guid/oauth2/token",
            params object[] scopes)
        {

            NameValue = name;
            this.type = type;
            this.flow = flow;
            this.authorizationUrl = authorizationUrl;
            this.tokenUrl = tokenUrl;

            this.scopes = new Dictionary<object, object>();
            for (var index = 0; index < scopes.Length; index = index + 2)
            {
                this.scopes.Add(scopes[index], scopes[index + 1]);
            }
        }

        public Dictionary<object, object> scopes { get; set; }

        public string tokenUrl { get; set; }

        public string authorizationUrl { get; set; }

        public string flow { get; set; }

        public string type { get; set; }

        private string NameValue { get; set; }

        public string Name()
        {
            return NameValue;
        }

        [JsonIgnore]
        public override object TypeId => base.TypeId;
    }

}
