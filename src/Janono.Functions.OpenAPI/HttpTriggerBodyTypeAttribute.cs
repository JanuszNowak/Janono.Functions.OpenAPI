using System;

namespace Janono.Functions.OpenAPI
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class HttpTriggerBodyTypeAttribute : Attribute
    {
        public HttpTriggerBodyTypeAttribute(Type type)
        {
            RequestType = type;
        }

        public Type RequestType { get; set; }
    }
}
