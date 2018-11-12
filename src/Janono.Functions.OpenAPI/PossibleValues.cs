using System;
using System.Collections.Generic;

namespace Janono.Functions.OpenAPI
{
    public class PossibleValues : Attribute
    {
        public PossibleValues(params object[] values)
        {
            Values = values;
        }

        public IEnumerable<object> Values { get; }

    }
}
