using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Dynamic;
using System.Net;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace JOT.GaiaClient
{
    public static class Extensions
    {
        public static Dictionary<string, ActionDelegate> GetActionsFromEntity(this Entity value, RestClient client)
        {
            var request = new RestRequest(new Uri(value.href).AbsolutePath, Method.GET);

            request.AddHeader("Accept", "application/vnd.siren+json");

            //TODO: Validate response
            var content = (RestResponse<Siren>)client.Execute<Siren>(request);
            var actionDictionary = GetActions(content.Data, client);
            return actionDictionary;
        }

        private static Dictionary<string, ActionDelegate> GetActions(Siren content, RestClient client)
        {
            //Include blocked actions to actions. TODO: add way to check on runtime if action is blocked or not
            var actions = content.actions;
            var blockedActions = content.blockedActions;
            if (blockedActions != null)
                actions.AddRange(blockedActions);

            var actionDictionary = new Dictionary<string, ActionDelegate>();

            if (actions == null)
                return actionDictionary;

            foreach (var action in actions)
            {
                actionDictionary.Add(action.name,
                    (Dictionary<string, object> UserDefinedFields, string plainText) =>
                    {
                        var actionRequest = new RestRequest(new Uri(action.href).AbsolutePath, action.method.ToRestSharpMethod());
                        IRestResponse<object> resp;

                        if (action.type == "text/plain")
                        {
                            actionRequest.AddHeader("Content-Type", "text/plain");
                            actionRequest.AddParameter("text/plain", plainText, ParameterType.RequestBody);
                        }
                        else
                        {
                            actionRequest.AddHeader("Content", "application/vnd.siren+json");
                            actionRequest.RequestFormat = DataFormat.Json;

                            if (action.fields != null)
                            {
                                var obj = new ExpandoObject();

                                foreach (var field in action.fields)
                                {
                                    if (UserDefinedFields != null && UserDefinedFields.ContainsKey(field.name))
                                        obj.AddProperty(field.name, UserDefinedFields[field.name]);
                                    else if (field.value != null)
                                        obj.AddProperty(field.name, field.value);
                                    else if (!field.optional)
                                        throw new Exception("Value of field missing");

                                }

                                actionRequest.AddBody(obj);
                            }

                        }
                        resp = client.Execute<object>(actionRequest);
                        JOTGaiaClient.HandleResponse(resp);

                        if (resp.ContentType.Contains("json"))
                            return resp.Data;
                        if (resp.ContentType.Contains("text/plain"))
                            return resp.Content;
                        else throw new NotSupportedException("Other than json or text/plain responses are not supported. Response type is " +
                            resp.ContentType);

                    });
            }

            return actionDictionary;
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
