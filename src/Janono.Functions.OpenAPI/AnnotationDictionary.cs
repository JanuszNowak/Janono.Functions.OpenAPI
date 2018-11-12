using System;
using System.Collections.Generic;

namespace Janono.Functions.OpenAPI
{

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class AnnotationDictionary : Attribute
    {
        public AnnotationDictionary(string name, params object[] values)
        {
            NameValue = name;
            this.values = new Dictionary<object, object>();
            for (var index = 0; index < values.Length; index = index + 2)
            {
                this.values.Add(values[index], values[index + 1]);
            }
        }

        public Dictionary<object, object> values { get; set; }

        private string NameValue { get; set; }

        public string Name()
        {
            return NameValue;
        }
    }
}
