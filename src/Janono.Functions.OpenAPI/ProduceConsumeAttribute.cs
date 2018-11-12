using System;
using System.Collections.Generic;

namespace Janono.Functions.OpenAPI
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ProduceConsumeAttribute : Attribute
    {
        public string Verb { get; }

        public IEnumerable<string> Produces { get; }

        public IEnumerable<string> Consumes { get; set; }

        public ProduceConsumeAttribute(string verb, IEnumerable<string> produceTypes, IEnumerable<string> consumeTypes)
        {
            if (string.IsNullOrWhiteSpace(verb))
                throw new ArgumentNullException(nameof(verb));

            if (produceTypes == null)
                throw new ArgumentNullException(nameof(produceTypes));

            if (consumeTypes == null)
                throw new ArgumentNullException(nameof(consumeTypes));

            Verb = verb;
            Produces = produceTypes;
            Consumes = consumeTypes;
        }
    }
}
