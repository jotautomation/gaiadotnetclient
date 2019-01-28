﻿using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Dynamic;
using System.Net;

namespace JOT.GaiaClient
{
    public static class Extensions
    {
        public static IReadOnlyDictionary<string, ActionDelegate> GetStateTriggers(this Siren value)
        {
            return GetActions(value);
        }

        public static IReadOnlyDictionary<string, T> GetApplications<T>(this Siren value, string type)
        {
            var apps = new Dictionary<string, T>();

            foreach (var item in value.entities)
            {
                if (item.@class.Contains(type))
                {
                    if (item.properties["name"] != "NA")
                    {
                        apps.Add(item.properties["name"], (T)Activator.CreateInstance(typeof(T), (string)item.properties["name"],
                        GetActionsFromEntity(item), item.href));
                    }
                }
            }

            return apps;
        }

        public static Dictionary<string, ActionDelegate> GetActionsFromEntity(this Entity value)
        {
            var client = new RestClient(new Uri(value.href));
            var request = new RestRequest("", Method.GET);

            request.AddHeader("Accept", "application/vnd.siren+json");

            //TODO: Validate response
            var content = (RestResponse<Siren>)client.Execute<Siren>(request);
            if (content.StatusCode == HttpStatusCode.OK)
            {
                var actionDictionary = GetActions(content.Data);
                return actionDictionary;
            }
            else return null;
        }

        private static Dictionary<string, ActionDelegate> GetActions(Siren content)
        {
            //Include blocked actions to actions. TODO: add way to check on runtime if action is blocked or not
            var actions = content.actions;
            var blockedActions = content.blockedActions;
            if (blockedActions != null)
                actions.AddRange(blockedActions);

            var actionDictionary = new Dictionary<string, ActionDelegate>();

            foreach (var action in content.actions)
            {
                actionDictionary.Add(action.name,
                    (Dictionary<string, object> UserDefinedFields, string plainText) =>
                    {
                        var actionClient = new RestClient(new Uri(action.href));
                        var actionRequest = new RestRequest("", action.method.ToRestSharpMethod());
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
                                    else if(!field.optional)
                                        throw new Exception("Value of field missing");

                                }

                                actionRequest.AddBody(obj);
                            }
                            
                        }
                        resp = actionClient.Execute<object>(actionRequest);
                        HandleResponse(resp);

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

        private static void HandleResponse(IRestResponse<object> resp)
        {
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                if (resp.ErrorException != null)
                    throw resp.ErrorException;
                throw new Exception("Request failed. Status " + resp.StatusCode);
            }
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