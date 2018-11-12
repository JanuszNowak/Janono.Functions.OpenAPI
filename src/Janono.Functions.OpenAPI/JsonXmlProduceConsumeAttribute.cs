using System;

namespace Janono.Functions.OpenAPI
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class JsonXmlProduceConsumeAttribute : ProduceConsumeAttribute
    {
        private const string JsonMime = "application/json";
        private const string XmlMime = "application/xml";

        public JsonXmlProduceConsumeAttribute(string verb) : base(verb, new[] { JsonMime, XmlMime }, new[] { JsonMime, XmlMime })
        {
        }
    }
}
