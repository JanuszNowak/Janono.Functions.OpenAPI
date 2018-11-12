using System;
using System.Collections.Generic;

namespace Janono.Functions.OpenAPI
{

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class Annotation : Attribute
    {
        public Annotation(params object[] values)
        {
            this.Values = values;
        }

        public IEnumerable<object> Values { get; }

    }
}
