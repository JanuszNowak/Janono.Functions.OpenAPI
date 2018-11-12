using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;

namespace Janono.Functions.OpenAPI
{
    public class Swagger
    {
        private const string SystemNamespace = "System";

        private const string SwaggerFunctionName = "Swagger";

        private const string SwaggerFunctionNameGui = "SwaggerGui";

        public static async Task<HttpResponseMessage> GetSwagger(HttpRequestMessage req, Assembly ass)
        {

            List<string> IgnoreList = new List<string>();
            Dictionary<string, string> parmaDictionary = new Dictionary<string, string>();

            ///get ignore params from query 
            foreach (var parameter in req.GetQueryNameValuePairs())
            {

                var key = parameter.Key.ToLower();
                var value = parameter.Value;
                if (key.StartsWith("ignore"))
                    IgnoreList.Add(value);

                if (key.StartsWith("param_"))
                {
                    parmaDictionary.Add(key.Replace("param_", ""), value);
                }
            }


            var assembly = ass;
            dynamic doc = new ExpandoObject();
            doc.swagger = "2.0";
            doc.info = new ExpandoObject();

            var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

            var companyName = versionInfo.CompanyName;
            //doc.info.title = assembly.GetName().Name;

            var title = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false).OfType<AssemblyTitleAttribute>().FirstOrDefault().Title;
            if (parmaDictionary.ContainsKey("env"))
                title += parmaDictionary["env"];

            doc.info.title = title;

            //doc.info.version = "1.0.0";
            doc.info.version = assembly.GetName().Version.ToString();
            doc.info.description = assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false).OfType<AssemblyDescriptionAttribute>().FirstOrDefault().Description; ;
            doc.host = req.RequestUri.Authority;
            doc.basePath = "/";
            doc.schemes = new[] { "https" };
            if (doc.host.Contains("127.0.0.1") || doc.host.Contains("localhost"))
            {
                doc.schemes = new[] { "http" };
            }


            doc.definitions = new ExpandoObject();

            doc.paths = GeneratePaths(assembly, doc, IgnoreList);


            //do not use securityDefinitions we have this is parmater
            doc.securityDefinitions = GenerateSecurityDefinitions(assembly, doc);

            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ObjectContent<object>(doc, new JsonMediaTypeFormatter()),
            });
        }

        public static async Task<HttpResponseMessage> GetSwagger(HttpRequestMessage req)
        {
            var assembly = Assembly.GetExecutingAssembly();

            return await GetSwagger(req, assembly);
        }


        private static dynamic GeneratePaths(Assembly assembly, dynamic doc, List<string> IgnoreList)
        {
            dynamic paths = new ExpandoObject();
            var methods = assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(FunctionNameAttribute), false).Length > 0)
                .ToArray();

            foreach (MethodInfo methodInfo in methods)
            {
                var route = "/api/";

                var functionAttr = methodInfo.GetCustomAttribute<FunctionNameAttribute>();

                if (functionAttr.Name == SwaggerFunctionName)
                    continue;

                if (functionAttr.Name == SwaggerFunctionNameGui)
                    continue;

                var triggerAttribute = methodInfo
                    .GetParameters()
                    .FirstOrDefault(o => o.GetCustomAttribute<HttpTriggerAttribute>() != null)?
                    .GetCustomAttribute<HttpTriggerAttribute>();

                if (triggerAttribute == null)
                    continue; // Trigger attribute is required in an Azure function

                if (string.IsNullOrWhiteSpace(triggerAttribute.Route))
                    route += functionAttr.Name;
                else
                    route += triggerAttribute.Route;

                // does the method has specified produces and consumes?
                var produceConsumeAttributes = methodInfo.GetCustomAttributes<ProduceConsumeAttribute>().ToArray();
                if (!produceConsumeAttributes.Any())
                    // default to http trigger attribute methods; and json
                    produceConsumeAttributes = triggerAttribute
                        .Methods
                        .Select(v => new ProduceConsumeAttribute(v, new[] { "application/json" }, new[] { "application/json" })).ToArray();

                dynamic path = new ExpandoObject();

                // explicitly defined produce and consumes by verbs;
                // do not put guessed "default" values like before
                foreach (var produceConsumeAttribute in produceConsumeAttributes)
                {
                    var verb = produceConsumeAttribute.Verb.ToLower();

                    dynamic operation = new ExpandoObject();
                    // Verbose description
                    operation.description = GetFunctionDescription(methodInfo, functionAttr.Name);
                    operation.operationId = functionAttr.Name;//ToTitleCase(functionAttr.Name) //+ ToTitleCase(verb);
                    // Summary is title
                    operation.summary = GetFunctionName(methodInfo, functionAttr.Name);

                    //operation.produces = produceConsumeAttribute.Produces;
                    operation.consumes = produceConsumeAttribute.Consumes;
                    operation.produces = produceConsumeAttribute.Produces;
                    //operation.parameters = GenerateFunctionParametersSignature(methodInfo, route, doc);
                    //// Verbose description
                    //operation.description = GetFunctionDescription(methodInfo, functionAttr.Name);
                    operation.parameters = GenerateFunctionParametersSignature(methodInfo, route, doc, IgnoreList);
                    operation.responses = GenerateResponseParameterSignature(methodInfo, doc);


                    ///Chekc security atributes on method operation.security 
                    GenerateOperationSecurity(operation, methodInfo, doc);

                    //dynamic keyQuery = new ExpandoObject();
                    //keyQuery.apikeyQuery = new string[0];
                    //operation.security = new ExpandoObject[] { keyQuery };

                    // Microsoft Flow import doesn't like two apiKey options, so we leave one out.
                    //dynamic apikeyHeader = new ExpandoObject();
                    //apikeyHeader.apikeyHeader = new string[0];
                    //operation.security = new ExpandoObject[] { keyQuery, apikeyHeader };

                    AddToExpando(path, verb, operation);
                }

                AddToExpando(paths, route, path);
            }
            return paths;
        }

        private static dynamic GenerateSecurityDefinitions(Assembly assembly, dynamic doc)
        {
            dynamic securityDefinitions = new ExpandoObject();

            var methods = assembly.GetTypes()
           .SelectMany(t => t.GetMethods())
           .Where(m => m.GetCustomAttributes(typeof(FunctionNameAttribute), false).Length > 0)
           .ToArray();
            //look for each method and look for attribute AnnotationSecurityDefinitionOAuth
            foreach (MethodInfo methodInfo in methods)
            {
                var annotationSecurityDefinitionOAuths = methodInfo.GetCustomAttributes<AnnotationSecurityDefinitionOAuth>().ToArray();
                if (annotationSecurityDefinitionOAuths.Length > 0)
                {
                    foreach (var annotationSecurityDefinitionOAuth in annotationSecurityDefinitionOAuths)
                    {
                        var item = (IDictionary<string, object>)securityDefinitions;

                        item[annotationSecurityDefinitionOAuth.Name()] = annotationSecurityDefinitionOAuth;


                        //var dict = (IDictionary<string, object>)item[annotationSecurityDefinitionOAuth.Name()] ;
                        //dict.Remove("TypeId");
                        //item[annotationSecurityDefinitionOAuth.Name] = new ExpandoObject();
                        //ExpandoObject itemOAuth = (ExpandoObject)(item[annotationSecurityDefinitionOAuth.Name]);
                        //itemOAuth.aasd = "Asd";
                        //securityDefinitions.type = annotationSecurityDefinitionOAuth;
                        //securityDefinitions.xxxxxxx = new ExpandoObject();
                        //annotationSecurityDefinitionOAuth.Name;
                    }

                }
            }
            //get all custom atribures for security from method or assembly ?



            //securityDefinitions.apikeyQuery = new ExpandoObject();
            //securityDefinitions.apikeyQuery.type = "apiKey";
            //securityDefinitions.apikeyQuery.name = "code";
            //securityDefinitions.apikeyQuery.@in = "query";

            // Microsoft Flow import doesn't like two apiKey options, so we leave one out.

            //securityDefinitions.apikeyHeader = new ExpandoObject();
            //securityDefinitions.apikeyHeader.type = "apiKey";
            //securityDefinitions.apikeyHeader.name = "x-functions-key";
            //securityDefinitions.apikeyHeader.@in = "header";
            return securityDefinitions;
        }


        private static void GenerateOperationSecurity(dynamic operation, MethodInfo methodInfo, object doc)
        {
            var annotationSecurityDefinitionOAuth = methodInfo.GetCustomAttributes<AnnotationSecurityDefinitionOAuth>().ToArray();
            if (annotationSecurityDefinitionOAuth.Length > 0)
            {
                //get ouath name from atribute
                var nameOfSecurityVariable = annotationSecurityDefinitionOAuth[0].Name();
                dynamic item = new ExpandoObject();
                var dict = (IDictionary<string, object>)item;
                dict[nameOfSecurityVariable] = new ExpandoObject[] { };
                operation.security = new ExpandoObject[] { item };
            }
        }

        private static string GetFunctionDescription(MethodInfo methodInfo, string funcName)
        {
            var displayAttr = (DisplayAttribute)methodInfo.GetCustomAttributes(typeof(DisplayAttribute), false)
                .SingleOrDefault();
            return !string.IsNullOrWhiteSpace(displayAttr?.Description) ? displayAttr.Description : $"This function will run {funcName}";
        }

        /// <summary>
        /// Max 80 characters in summary/title
        /// </summary>
        private static string GetFunctionName(MethodInfo methodInfo, string funcName)
        {
            var displayAttr = (DisplayAttribute)methodInfo.GetCustomAttributes(typeof(DisplayAttribute), false)
                .SingleOrDefault();
            if (!string.IsNullOrWhiteSpace(displayAttr?.Name))
            {
                return displayAttr.Name.Length > 80 ? displayAttr.Name.Substring(0, 80) : displayAttr.Name;
            }
            return $"Run {funcName}";
        }



        private static IList<object> GetPropertyPossibleValues(PropertyInfo propertyInfo)
        {
            var displayAttr = (PossibleValues)propertyInfo.GetCustomAttributes(typeof(PossibleValues), false)
                .SingleOrDefault();
            //return !string.IsNullOrWhiteSpace(displayAttr?.Values) ? displayAttr.Values : $"This returns {propertyInfo.PropertyType.Name}";
            if (displayAttr == null)
            {
                return null;
            }
            else
                return displayAttr.Values.ToList();
        }


        private static string GetPropertyDescription(PropertyInfo propertyInfo)
        {
            var displayAttr = (DisplayAttribute)propertyInfo.GetCustomAttributes(typeof(DisplayAttribute), false)
                .SingleOrDefault();
            return !string.IsNullOrWhiteSpace(displayAttr?.Description) ? displayAttr.Description : $"This returns {propertyInfo.PropertyType.Name}";
        }


        private static bool HavePropertyDescription(PropertyInfo propertyInfo)
        {
            var displayAttr = (DisplayAttribute)propertyInfo.GetCustomAttributes(typeof(DisplayAttribute), false)
                .SingleOrDefault();
            return displayAttr != null;
        }

        private static dynamic GenerateResponseParameterSignature(MethodInfo methodInfo, dynamic doc)
        {
            dynamic responses = new ExpandoObject();
            dynamic responseDef = new ExpandoObject();
            responseDef.description = "OK";

            var returnType = methodInfo.ReturnType;
            if (returnType.IsGenericType)
            {
                var genericReturnType = returnType.GetGenericArguments().FirstOrDefault();
                if (genericReturnType != null)
                {
                    returnType = genericReturnType;
                }
            }
            if (returnType == typeof(HttpResponseMessage))
            {
                var responseTypeAttr = (ResponseTypeAttribute)methodInfo
                    .GetCustomAttributes(typeof(ResponseTypeAttribute), false).FirstOrDefault();
                if (responseTypeAttr != null)
                {
                    returnType = responseTypeAttr.ResponseType;
                }
                else
                {
                    returnType = typeof(void);
                }
            }
            if (returnType != typeof(void))
            {
                responseDef.schema = new ExpandoObject();

                if (returnType.Namespace == SystemNamespace)
                {
                    // Warning:
                    // Allthough valid, it's always better to wrap single values in an object
                    // Returning { Value = "foo" } is better than just "foo"
                    SetParameterType(returnType, responseDef.schema, null);
                }
                else
                {
                    string name = returnType.Name;
                    if (returnType.IsGenericType)
                    {
                        var realType = returnType.GetGenericArguments()[0];
                        if (realType.Namespace == SystemNamespace)
                        {
                            dynamic inlineSchema = GetObjectSchemaDefinition(null, returnType);
                            responseDef.schema = inlineSchema;
                        }
                        else
                        {
                            AddToExpando(responseDef.schema, "$ref", "#/definitions/" + name);
                            AddParameterDefinition((IDictionary<string, object>)doc.definitions, returnType);
                        }
                    }
                    else
                    {
                        AddToExpando(responseDef.schema, "$ref", "#/definitions/" + name);
                        AddParameterDefinition((IDictionary<string, object>)doc.definitions, returnType);
                    }
                }
            }


            var responseTypesAttributes = methodInfo.GetCustomAttributes<HttpProduceResponse>().ToList();

            if (responseTypesAttributes.Count == 0) { AddToExpando(responses, "200", responseDef); }
            else
                foreach (var responseType in responseTypesAttributes)
                {
                    dynamic responseDefList = new ExpandoObject();
                    responseDefList.description = responseType.Description;
                    if (responseType.ResponseType != null)
                    {

                        if (responseType.ResponseType.Namespace == SystemNamespace)
                        {
                            // Warning:
                            // Allthough valid, it's always better to wrap single values in an object
                            // Returning { Value = "foo" } is better than just "foo"
                            SetParameterType(responseType.ResponseType, responseDefList.schema, null);
                        }
                        else
                        {
                            string name = responseType.ResponseType.Name;
                            if (responseType.ResponseType.IsGenericType)
                            {
                                var realType = responseType.ResponseType.GetGenericArguments()[0];
                                if (realType.Namespace == SystemNamespace)
                                {
                                    dynamic inlineSchema = GetObjectSchemaDefinition(null, responseType.ResponseType);
                                    responseDefList.schema = inlineSchema;
                                }
                                else
                                {
                                    AddToExpando(responseDefList.schema, "$ref", "#/definitions/" + name);
                                    AddParameterDefinition((IDictionary<string, object>)doc.definitions, responseType.ResponseType);
                                }
                            }
                            else
                            {
                                AddToExpando(responseDefList.schema, "$ref", "#/definitions/" + name);
                                AddParameterDefinition((IDictionary<string, object>)doc.definitions, responseType.ResponseType);
                            }
                        }
                        //dynamic inlineSchema = GetObjectSchemaDefinition(null, responseType.ResponseType);
                        //responseDefList.schema = inlineSchema;
                    }

                    AddToExpando(responses, ((int)responseType.StatusCode).ToString(), responseDefList);

                }
            return responses;
        }


        private static List<object> GenerateFunctionParametersSignature(MethodInfo methodInfo, string route, dynamic doc, List<string> IgnoreList)
        {
            var parameterSignatures = new List<object>();
            foreach (ParameterInfo p in methodInfo.GetParameters())
            {

                var parameter = p;
                var parameterParameterType = p.ParameterType;

                if (parameter.ParameterType == typeof(TraceWriter)) continue;

                //jnno
                if (parameter.ParameterType == typeof(CloudTable)) continue;

                var attributes = parameter.GetCustomAttributes().ToArray();
                var isIgnored = attributes.Any(o => o is IgnoreParameterAttribute);

                if (isIgnored)
                    continue;

                if (parameter.ParameterType == typeof(HttpRequestMessage))
                {
                    var httpTriggerBodyTypeAttribute = attributes.FirstOrDefault(attr => attr is HttpTriggerBodyTypeAttribute) as HttpTriggerBodyTypeAttribute;

                    if (httpTriggerBodyTypeAttribute != null)
                        parameterParameterType = httpTriggerBodyTypeAttribute.RequestType;
                    else
                        continue;
                }

                var hasUriAttribute = attributes.Any(attr => attr is FromUriAttribute);


                if (route.Contains('{' + parameter.Name))
                {
                    dynamic opParam = new ExpandoObject();
                    opParam.name = parameter.Name;
                    opParam.@in = "path";
                    opParam.required = true;
                    SetParameterType(parameterParameterType, opParam, null);
                    parameterSignatures.Add(opParam);
                }
                else if (hasUriAttribute && parameterParameterType.Namespace == SystemNamespace)
                {
                    dynamic opParam = new ExpandoObject();
                    opParam.name = parameter.Name;
                    opParam.@in = "query";
                    opParam.required = attributes.Any(attr => attr is RequiredAttribute);
                    SetParameterType(parameterParameterType, opParam, doc.definitions);
                    parameterSignatures.Add(opParam);
                }
                else if (hasUriAttribute && parameterParameterType.Namespace != SystemNamespace)
                {
                    AddObjectProperties(parameterParameterType, "", parameterSignatures, doc);
                }
                else
                {
                    dynamic opParam = new ExpandoObject();
                    opParam.name = parameter.Name;
                    opParam.@in = "body";
                    opParam.required = true;
                    opParam.schema = new ExpandoObject();
                    if (parameterParameterType.Namespace == SystemNamespace)
                    {
                        SetParameterType(parameterParameterType, opParam.schema, null);
                    }
                    else
                    {
                        AddToExpando(opParam.schema, "$ref", "#/definitions/" + parameterParameterType.Name);
                        AddParameterDefinition((IDictionary<string, object>)doc.definitions, parameterParameterType);
                    }
                    parameterSignatures.Add(opParam);
                }
            }


            //Get all parameter anotation for additional parameter in like in header

            var methodAnnotationParameters = methodInfo.GetCustomAttributes<AnnotationParameter>().ToArray();

            //var methodAnnotationParameters = IList<AnnotationParameter>methodInfo.GetCustomAttributes(typeof(AnnotationParameter), false).FirstOrDefault();
            foreach (AnnotationParameter parameter in methodAnnotationParameters)
            {
                //todo not not adding ignore params by name in req
                //if ( == "x-functions-key")
                if (IgnoreList.Contains(parameter.Name))
                {
                }
                else
                {

                    dynamic opParam = new ExpandoObject();
                    opParam.name = parameter.Name;
                    opParam.@in = parameter.@In.ToString();
                    opParam.required = parameter.Required;


                    var parameterParameterType = parameter.ParameterType;

                    if (parameterParameterType.Namespace == SystemNamespace)
                    {
                        //opParam.schema = new ExpandoObject();
                        //SetParameterType(parameterParameterType, opParam.schema, null);
                        SetParameterType(parameterParameterType, opParam, null);
                    }
                    else
                    {
                        opParam.schema = new ExpandoObject();
                        AddToExpando(opParam.schema, "$ref", "#/definitions/" + parameterParameterType.Name);
                        AddParameterDefinition((IDictionary<string, object>)doc.definitions, parameterParameterType);
                    }

                    parameterSignatures.Add(opParam);
                }
            }


            return parameterSignatures;
        }

        private static void AddObjectProperties(Type t, string parentName, List<object> parameterSignatures, dynamic doc)
        {
            var publicProperties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in publicProperties)
            {
                if (!string.IsNullOrWhiteSpace(parentName))
                {
                    parentName += ".";
                }
                if (property.PropertyType.Namespace != SystemNamespace)
                {
                    AddObjectProperties(property.PropertyType, parentName + property.Name, parameterSignatures, doc);
                }
                else
                {
                    dynamic opParam = new ExpandoObject();

                    opParam.name = parentName + property.Name;
                    opParam.@in = "query";
                    opParam.required = property.GetCustomAttributes().Any(attr => attr is RequiredAttribute);
                    opParam.description = GetPropertyDescription(property);
                    SetParameterType(property.PropertyType, opParam, doc.definitions);

                    //
                    if (HavePropertyDescription(property))
                        parameterSignatures.Add(opParam);
                }
            }
        }

        private static void AddParameterDefinition(IDictionary<string, object> definitions, Type parameterType)
        {
            dynamic objDef;
            if (!definitions.TryGetValue(parameterType.Name, out objDef))
            {
                objDef = GetObjectSchemaDefinition(definitions, parameterType);
                definitions.Add(parameterType.Name, objDef);
            }
        }


        private static Annotation[] GetPropertyAnnotation(PropertyInfo propertyInf)
        {
            var displayAttr = propertyInf.GetCustomAttributes<Annotation>().ToArray();

            //var methodAnnotationParameters = IList<AnnotationParameter>methodInfo.GetCustomAttributes(typeof(AnnotationParameter), false).FirstOrDefault();
            //foreach (AnnotationParameter parameter in methodAnnotationParameters)
            //{

            //var displayAttr = (Annotation)propertyInf.GetCustomAttributes(typeof(Annotation), false)
            //.SingleOrDefault();
            if (displayAttr.Length > 0)
            {
                return displayAttr;

                //return displayAttr.ToList();
            }
            else
            {
                return null;
            }
        }

        private static bool HavePropertyDescription(Type parameterTyp)
        {
            var displayAttr = (DescriptionAttribute)parameterTyp.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .SingleOrDefault();
            return displayAttr != null;
        }
        private static string GetPropertyDescription(Type parameterTyp)
        {
            var displayAttr = (DescriptionAttribute)parameterTyp.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .SingleOrDefault();
            return !string.IsNullOrWhiteSpace(displayAttr?.Description) ? displayAttr.Description : $"This returns {parameterTyp.Name}";
        }

        private static dynamic GetObjectSchemaDefinition(IDictionary<string, object> definitions, Type parameterType)
        {
            dynamic objDef = new ExpandoObject();
            objDef.type = "object";

            if (HavePropertyDescription(parameterType)) { objDef.description = GetPropertyDescription(parameterType); }

            objDef.properties = new ExpandoObject();
            var publicProperties = parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            //parameterType.GetProperties(BindingFlags.Serializable | BindingFlags.Instance);

            //Attributes Public | Serializable | BeforeFieldInit System.Reflection.TypeAttributes

            List<string> requiredProperties = new List<string>();

            foreach (PropertyInfo property in publicProperties)
            {
                if (property.GetCustomAttributes().Any(attr => attr is RequiredAttribute))
                {
                    requiredProperties.Add(property.Name);
                }

                dynamic propDef = new ExpandoObject();
                propDef.description = GetPropertyDescription(property);

                SetParameterType(property.PropertyType, propDef, definitions);

                ///Get Proparty enums               
                var posibleValuesForProperty = GetPropertyPossibleValues(property);

                if (posibleValuesForProperty != null && posibleValuesForProperty.Count > 0)
                {
                    propDef.@enum = posibleValuesForProperty;
                }

                //"minimum": 1,
                //"maximum": 100,
                //"example": 10
                var propertyAnotations = GetPropertyAnnotation(property);

                if (propertyAnotations != null)
                {
                    foreach (var propertyAnotation in propertyAnotations)
                    {
                        var dict = (IDictionary<string, object>)propDef;

                        var a = (string)propertyAnotation.Values.ToList()[0];
                        var b = propertyAnotation.Values.ToList()[1];
                        dict[a] = b;

                    }
                }


                //"example": {
                //    "@serialNumber": "1234567890"
                //}
                var propertyAnotationsDictionary = property.GetCustomAttributes<AnnotationDictionary>().ToArray();

                //if (propertyAnotationsDictionary.Length > 0)
                {
                    foreach (var propertyAnotation in propertyAnotationsDictionary)
                    {

                        var dict = (IDictionary<string, object>)propDef;
                        //propertyAnotation.Name()
                        //var a = (string)propertyAnotation.Values.ToList()[0];
                        //var b = propertyAnotation.Values.ToList()[1];
                        dict[propertyAnotation.Name()] = propertyAnotation.values;

                    }
                }




                //SetParameterType(property.PropertyType, propDef, definitions);
                //
                if (HavePropertyDescription(property))
                    AddToExpando(objDef.properties, property.Name, propDef);
            }
            if (requiredProperties.Count > 0)
            {
                objDef.required = requiredProperties;
            }
            return objDef;
        }

        private static void SetParameterType(dynamic opParam, Type inputType)
        {
            if (inputType.Namespace != SystemNamespace || inputType.IsGenericType && inputType.GetGenericArguments()[0].Namespace != SystemNamespace)
                return;

            switch (Type.GetTypeCode(inputType))
            {
                case TypeCode.UInt16:
                case TypeCode.Int32:
                    opParam.format = "int32";
                    opParam.type = "integer";
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt32:
                    opParam.format = "int64";
                    opParam.type = "integer";
                    break;
                case TypeCode.Single:
                    opParam.format = "float";
                    opParam.type = "number";
                    break;
                case TypeCode.Double:
                    opParam.format = "double";
                    opParam.type = "number";
                    break;
                case TypeCode.String:
                    opParam.type = "string";
                    break;
                case TypeCode.Byte:
                    opParam.format = "byte";
                    opParam.type = "string";
                    break;
                case TypeCode.Boolean:
                    opParam.type = "boolean";
                    break;
                case TypeCode.DateTime:
                    opParam.format = "date";
                    opParam.type = "string";
                    break;
                default:
                    opParam.type = "string";
                    break;
            }
        }

        private static void SetParameterType(Type parameterType, dynamic opParam, dynamic definitions)
        {
            var inputType = parameterType;
            var setObject = opParam;

            if (inputType.Name == "Byte[]")
            {
                opParam.type = "string";
                opParam.format = "byte";
            }
            else
            if (inputType.IsArray)
            {
                opParam.type = "array";
                opParam.items = new ExpandoObject();
                setObject = opParam.items;
                parameterType = parameterType.GetElementType();
            }
            else if (inputType.IsGenericType)
            {
                var underlyingType = Nullable.GetUnderlyingType(inputType);
                if (underlyingType != null)
                {
                    // nullable type
                    inputType = underlyingType;
                }
                else
                {
                    // non-nullable type
                    opParam.type = "array";
                    opParam.items = new ExpandoObject();
                    setObject = opParam.items;
                    parameterType = parameterType.GetGenericArguments()[0];
                }
            }

            if (inputType.Namespace == SystemNamespace || inputType.IsGenericType && inputType.GetGenericArguments()[0].Namespace == SystemNamespace)
                SetParameterType(setObject, inputType);
            else if (inputType.IsEnum)
            {
                opParam.type = "string";
                opParam.@enum = Enum.GetNames(inputType);
            }
            else if (definitions != null)
            {
                AddToExpando(setObject, "$ref", "#/definitions/" + parameterType.Name);
                AddParameterDefinition((IDictionary<string, object>)definitions, parameterType);
            }
        }

        public static string ToTitleCase(string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
        }

        public static void AddToExpando(ExpandoObject obj, string name, object value)
        {
            ((IDictionary<string, object>)obj).Add(name, value);
        }
    }
}
