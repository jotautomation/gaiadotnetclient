using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Dynamic;
using System.Net;

namespace JOT.RESTClient
{
    public static class Extensions
    {
        public static IReadOnlyDictionary<string, T> GetApplications<T>(this Siren value, string type)
        {
            var apps = new Dictionary<string, T>();

            foreach (var item in value.entities)
            {
                if (item.@class.Contains(type))
                {
                    apps.Add(item.properties["name"], (T)Activator.CreateInstance(typeof(T), (string)item.properties["name"],
                    GetActions(item)));
                }
            }

            return apps;
        }

        public static Dictionary<string, Func<string>> GetActions(this Entity value)
        {
            var client = new RestClient(new Uri(value.href));
            var request = new RestRequest("", Method.GET);

            request.AddHeader("Accept", "application/vnd.siren+json");

            var content = (RestResponse<Siren>)client.Execute<Siren>(request);

            var actionDictionary = new Dictionary<string, Func<string>>();

            foreach (var action in content.Data.actions)
            {
                actionDictionary.Add(action.name,
                    () =>
                     {
                         var actionClient = new RestClient(new Uri(action.href));
                         var actionRequest = new RestRequest("", action.method.ToRestSharpMethod());

                         actionRequest.AddHeader("Content", "application/vnd.siren+json");
                         actionRequest.RequestFormat = DataFormat.Json;

                         if (action.fields != null)
                             foreach (var field in action.fields)
                             {
                                 var obj = new ExpandoObject();
                                 obj.AddProperty(field.name, field.value);
                                 actionRequest.AddBody(obj);
                             }

                         var resp = actionClient.Execute(actionRequest);

                         HandleResponse(resp.StatusCode);

                         if (resp.ContentType.Contains("json") | resp.ContentType.Contains("text/plain"))
                             return resp.Content;
                         else throw new NotSupportedException("Other than json or text/plain responses are not supported. Response type is " +
                             resp.ContentType);

                     });
            }
            return actionDictionary;
        }

        private static void HandleResponse(HttpStatusCode statusCode)
        {
            if (statusCode != HttpStatusCode.OK)
                throw new Exception("Request failed. Status " + statusCode);
        }

        public static Method ToRestSharpMethod(this string value)
        {
            return (Method)Enum.Parse(typeof(Method), value);
        }

        public static void AddProperty(this ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }
    }
}
