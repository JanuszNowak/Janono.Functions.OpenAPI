using System;

namespace Janono.Functions.OpenAPI
{

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class AnnotationParameter : Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public bool Required { get; }

        public ParameterInEnum @In { get; }

        public Type ParameterType { get; set; }

        public AnnotationParameter(string name, ParameterInEnum @in, string description, bool required, Type type)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentNullException(nameof(description));

            Name = name;
            Description = description;
            Required = required;
            @In = @in;

            ParameterType = type;
        }

        public enum ParameterInEnum
        {
            header,
            body

        }
    }

}
