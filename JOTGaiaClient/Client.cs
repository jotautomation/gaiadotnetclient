using RestSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JOT.GaiaClient
{
    public delegate object ActionDelegate(Dictionary<string, object> fields = null, string plainText = null);
    public class JOTGaiaClient
    {
        public Dictionary<string, Application<string>> Applications { get; private set; }
        public IReadOnlyDictionary<string, ActionDelegate> StateTriggers { get; private set; }

        public bool ReadyForTesting
        {
            get
            {
                if (this.State.Contains("Ready"))
                {
                    return true;
                }

                return false;
            }
        }

        public bool TestBoxClosing
        {
            get
            {
                if (this.State.Contains("Closing"))
                {
                    return true;
                }

                return false;
            }
        }

        public string State
        {
            get
            {
                var request = new RestRequest("api", Method.GET);

                request.AddHeader("Accept", "application/vnd.siren+json");

                var response = (RestResponse<Siren>)myRestClient.Execute<Siren>(request);
                return response.Data.properties["state"];
            }
        }
        RestClient myRestClient;
        public JOTGaiaClient(string baseUrl)
        {
            myRestClient = new RestClient(baseUrl);
            // Fetch applications from the test box
            Populate();
        }

        public JOTGaiaClient(Uri baseUrl)
        {
            myRestClient = new RestClient(baseUrl);
            // Fetch applications from the test box
            Populate();
        }

        /// <summary>
        /// Download wave file (.wav) from G5
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public byte[] DownloadWave(string name)
        {
            var request = new RestRequest("api/waves/" + name, Method.GET);

            return myRestClient.DownloadData(request);
        }

        /// <summary>
        /// Upload wave file (.wav) to G5
        /// </summary>
        /// <param name="filePath"></param>
        public void UploadWave(string filePath)
        {
            RestRequest restRequest = new RestRequest("api/waves");
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.Method = Method.POST;
            restRequest.AddFile("file", filePath);
            restRequest.AlwaysMultipartFormData = true;
            var response = myRestClient.Execute(restRequest);
        }

        public void Populate()
        {
            var request = new RestRequest("api/applications", Method.GET);

            request.AddHeader("Accept", "application/vnd.siren+json");

            var response = (RestResponse<Siren>)myRestClient.Execute<Siren>(request);

            if (!response.IsSuccessful)
            {
                throw response.ErrorException ?? new Exception("Unknown error when trying to connect to machine.");
            }
            Applications = new Dictionary<string, Application<string>>();
            foreach (var entity in response.Data.entities)
            {

                var client = new RestClient(new Uri(entity.href));
                var entity_request = new RestRequest("", Method.GET);

                entity_request.AddHeader("Accept", "application/vnd.siren+json");

                //TODO: Validate response
                var content = (RestResponse<Siren>)client.Execute<Siren>(entity_request);
                var app = (Application<string>)Activator.CreateInstance(typeof(Application<string>), (string)entity.properties["name"], GetActions(content.Data), entity.href);
                Applications[entity.properties["name"]] = app;
            }


            request = new RestRequest("api", Method.GET);

            request.AddHeader("Accept", "application/vnd.siren+json");

            response = (RestResponse<Siren>)myRestClient.Execute<Siren>(request);
            StateTriggers = GetActions(response.Data);
        }

        private static Dictionary<string, ActionDelegate> GetActions(Siren content)
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

                            foreach (var userfield in UserDefinedFields.Keys)
                            {
                                bool found = false;

                                if (action.fields != null)
                                    foreach (var actionfield in action.fields)
                                    {
                                        if (actionfield.name == userfield)
                                            found = true;
                                    }

                                if (!found)
                                    throw new ArgumentException("Field '" + userfield + "' not available at action '" + action.name +
                                        "'. Check actions (with browser) from " + action.href.Replace("/" + action.name, ""));
                            }

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
                                        throw new ArgumentException("Value of field missing");

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

    }
}
