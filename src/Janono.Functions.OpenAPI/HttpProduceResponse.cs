using System;
using System.Net;

namespace Janono.Functions.OpenAPI
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HttpProduceResponse : Attribute
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Description { get; }

        public Type ResponseType { get; set; }

        public HttpProduceResponse(HttpStatusCode statuscode, string description, Type type = null)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentNullException(nameof(description));

            StatusCode = statuscode;

            Description = description;

            ResponseType = type;

        }
    }
}
