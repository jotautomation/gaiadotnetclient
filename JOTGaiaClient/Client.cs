using RestSharp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using WebSocket4Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace JOT.GaiaClient
{
    /// <summary>
    /// Delegate that defines action to be executed on application
    /// </summary>
    /// <param name="fields">Named fields that will be converted to JSON fields</param>
    /// <param name="plainText">Plain text body of the HTTP request</param>
    /// <returns></returns>
    public delegate object ActionDelegate(Dictionary<string, object> fields = null, string plainText = null);

    /// <summary>
    /// Client implementation for JOT Automation gaia platform machines
    /// </summary>
    public class JOTGaiaClient : Waitable
    {

        internal WebSocket AppStateWebsocket { get; private set; }
        internal WebSocket MachineStateWebsocket { get; private set; }


        /// <summary>
        /// List of applications on the machine. List wil be populated when
        /// client connects to the machine.
        /// </summary>
        public Dictionary<string, Application> Applications { get; private set; }

        /// <summary>
        /// List of state triggers. These are used to tell the machine that test
        /// is ready etc.
        /// </summary>
        public IReadOnlyDictionary<string, ActionDelegate> StateTriggers { get; private set; }


        /// <summary>
        /// Return true if machine is ready for testing i.e. in ready state.
        /// Any test activity may be executed on this state.
        /// </summary>
        public bool ReadyForTesting
        {
            get
            {
                if (this.State == "Ready")
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns true if test box is closing. Some application actions
        /// are available. Robot cannot be controlled yet and test box is not
        /// RF or Audio shielded.
        /// </summary>
        public bool TestBoxClosing
        {
            get
            {
                if (this.State == "Closing")
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// All devices that can interfere audio or rf measurements
        /// are turned off
        /// </summary>
        /// <param name="on"></param>
        public void SilentMode(bool on)
        {
            if (on)
                StateTriggers["SilenceOn"]();
            else
                StateTriggers["SilenceOff"]();
        }

        /// <summary>
        /// Returns internal state of the machine. 
        /// This should be rarely needed,
        /// Use more convenient State, ReadyForTesting() and TestBoxClosing() instead
        /// </summary>
        public string InternalState
        {
            get
            {
                var request = new RestRequest("api", Method.GET);

                request.AddHeader("Accept", "application/vnd.siren+json");

                var response = (RestResponse<Siren>)myRestClient.Execute<Siren>(request);
                return response.Data.properties["internal_state"];
            }
        }

        /// <summary>
        /// Returns main state of the machine.
        /// See explanation at https://github.com/jotautomation/gaiadotnetclient#g5-states
        /// </summary>
        public override string State
        {
            get
            {
                var request = new RestRequest("api", Method.GET);

                request.AddHeader("Accept", "application/vnd.siren+json");

                var response = (RestResponse<Siren>)myRestClient.Execute<Siren>(request);
                return response.Data.properties["state"];
            }
        }

        protected override WebSocket stateWS => MachineStateWebsocket;

        RestClient myRestClient;

        /// <summary>
        /// Client implementation for JOT Automation gaia platform machines
        /// </summary>
        /// <param name="baseUrl">URL for the controlled machine</param>
        public JOTGaiaClient(string baseUrl, string user = "", string password = "")
            : base("Machine State")
        {
            connect(new Uri(baseUrl), user, password);
        }

        /// <summary>
        /// Client implementation for JOT Automation gaia platform machines
        /// </summary>
        /// <param name="baseUrl">URL for the controlled machin</param>
        public JOTGaiaClient(Uri baseUrl, string user = "", string password = "")
             : base("Machine State")
        {
            connect(baseUrl, user, password);
        }

        public enum States
        {
            Closing,
            Ready,
            ReadyForEngage,
            NotReady,
            Error
        }

        /// <summary>
        /// Wait for state.
        /// </summary>
        /// <param name="state">State to wait</param>
        /// <param name="timeOut_ms">The number of milliseconds to wait, or System.Threading.Timeout.Infinite (-1)
        ///     to wait indefinitely.</param>
        /// <returns>Returns true if state was reached before timeout.</returns>
        public bool TryWaitState(States state, int timeOut_ms = -1, bool raiseOnError = true)
        {
            return TryWaitState(state.ToString(), timeOut_ms, raiseOnError);
        }

        /// <summary>
        /// Wait for state. Throws exception if timeout occurs.
        /// </summary>
        /// <param name="state">State to wait</param>
        /// <param name="timeOut_ms">The number of milliseconds to wait, or System.Threading.Timeout.Infinite (-1)
        ///     to wait indefinitely.</param>
        public void WaitState(States state, int timeOut_ms = -1, bool raiseOnError = true)
        {
            WaitState(state.ToString(), timeOut_ms, raiseOnError);
        }

        private void connect(Uri url, string user, string password)
        {
            // create client
            myRestClient = new RestClient(url);

            var version_request = new RestRequest("api", Method.GET);

            version_request.AddHeader("Accept", "application/vnd.siren+json");

            var version_response = (RestResponse<Siren>)myRestClient.Execute<Siren>(version_request);

            if (!version_response.IsSuccessful)
            {
                throw version_response.ErrorException ?? new Exception("Unknown error when trying to connect to machine.");
            }

            var gaia_version = new Version(version_response.Data.properties["sw_version"].Split('-')[0]);

            if (gaia_version.CompareTo(new Version("1.2.0")) < 0)
                throw new GaiaException($"Incompatible versions. Minimun version of Gaia machine is 1.2.0. Currently {gaia_version}");

            var WsUriApp = new UriBuilder(url)
            {
                Scheme = "ws",
                Path = "/websocket/applications"
            };

            AppStateWebsocket = new WebSocket(WsUriApp.ToString());
            AppStateWebsocket.Open();

            var WsUriMachineState = new UriBuilder(url)
            {
                Scheme = "ws",
                Path = "/websocket/state"
            };

            MachineStateWebsocket = new WebSocket(WsUriMachineState.ToString());
            MachineStateWebsocket.Open();

            // Start listen to state changes
            StartListen();

            myRestClient.CookieContainer = new CookieContainer();

            if (!string.IsNullOrWhiteSpace(user))
            {
                // Login to server
                var request = new RestRequest("login", Method.POST);
                request.RequestFormat = DataFormat.Json;
                request.AddJsonBody(new { user = user, password = password });
                IRestResponse<object> response = myRestClient.Execute<object>(request);
                HandleResponse(response);
                Console.WriteLine(response.Content);
            }
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

        void Populate()
        {
            var request = new RestRequest("api/applications", Method.GET);

            request.AddHeader("Accept", "application/vnd.siren+json");

            var response = (RestResponse<Siren>)myRestClient.Execute<Siren>(request);

            if (!response.IsSuccessful)
            {
                throw response.ErrorException ?? new Exception("Unknown error when trying to connect to machine.");
            }
            Applications = new Dictionary<string, Application>();
            foreach (var entity in response.Data.entities)
            {
                var entity_request = new RestRequest(new Uri(entity.href).AbsolutePath, Method.GET);
                entity_request.AddHeader("Accept", "application/vnd.siren+json");

                //TODO: Validate response
                var content = (RestResponse<Siren>)myRestClient.Execute<Siren>(entity_request);
                var app = (Application)Activator.CreateInstance(typeof(Application), 
                    (string)entity.properties["name"], 
                    GetActions(content.Data, myRestClient), 
                    content.Data.actions,
                    content.Data.blockedActions,
                    entity.href, this.AppStateWebsocket);
                Applications[entity.properties["name"]] = app;
            }


            request = new RestRequest("api", Method.GET);

            request.AddHeader("Accept", "application/vnd.siren+json");

            response = (RestResponse<Siren>)myRestClient.Execute<Siren>(request);
            StateTriggers = GetActions(response.Data, myRestClient);
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

                            if (UserDefinedFields != null)
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
                        resp = client.Execute<object>(actionRequest);
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

        internal static void HandleResponse(IRestResponse<object> resp)
        {
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                if (resp.ErrorException != null)
                    throw resp.ErrorException;
                if (!string.IsNullOrEmpty(resp.Content))
                {
                    throw new GaiaClientException(GaiaError.FromDictionary((Dictionary<string, object>)resp.Data));
                }
                else
                    throw new Exception("Request failed. Status " + resp.StatusCode);
            }
        }

        protected override void CheckWaitStatus(JObject status)
        {
            if (this.StateWait != null && this.StateWait.States.Any(st => st == status?["state"].ToString()))
                this.StateWait.WaitEvent.Set();
        }
    }

    [Serializable]
    public class GaiaException : Exception
    {
        public GaiaException() { }
        public GaiaException(string message) : base(message) { }
        public GaiaException(string message, Exception inner) : base(message, inner) { }
        protected GaiaException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Application or the tester itself in error state
    /// </summary>
    [Serializable]
    public class GaiaErrorStateException : Exception
    {
        public GaiaErrorStateException() { }
        public GaiaErrorStateException(string message) : base(message) { }
        public GaiaErrorStateException(string message, Exception inner) : base(message, inner) { }
        protected GaiaErrorStateException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Generic excpetion that comes from the tester
    /// </summary>
    [Serializable]
    public class GaiaClientException : GaiaException
    {
        public GaiaError GaiaError;
        public GaiaClientException(GaiaError error) : base(error.message)
        {
            this.GaiaError = error;
        }
        protected GaiaClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class GaiaError
    {
        public string message { get; private set; }
        public string time { get; private set; }
        public string instructions { get; private set; }
        public string http_code { get; private set; }
        public string gaia_error_code { get; private set; }

        public static GaiaError FromDictionary(Dictionary<string, object> dict)
        {
            return new GaiaError()
            {
                message = (string)dict["message"],
                gaia_error_code = (string)dict["gaia_error_code"],
                http_code = dict["http_code"].ToString(),
                instructions = (string)dict["instructions"],
                time = (string)dict["time"]
            };
        }
    }

}
