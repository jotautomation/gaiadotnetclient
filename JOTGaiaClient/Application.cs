using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace JOT.GaiaClient
{
    /// <summary>
    /// Everything you can control on the machine is application. 
    /// Moving parts on mechanics, robots, electronics...
    /// </summary>
    public class Application : Waitable
    {
        private WebSocket myStateWs;
        private readonly object ActionPropertiesLock = new object();

        public Application(string name, Dictionary<string, ActionDelegate> actions, List<Action> actionProperties, List<Action> blockedActionProperties, string href, WebSocket ws) : base(name)

        {
            ActionWaitEvents = new Dictionary<object, string>();
            SetActionProperties(actionProperties, blockedActionProperties);
            Name = name;
            Actions = actions;
            Href = href;
            myStateWs = ws;
            myStateWs.MessageReceived += MyStateWs_MessageReceived;
            StartListen();
        }

        private void SetActionProperties(List<Action> actionProperties, List<Action> blockedActionProperties)
        {
            lock (ActionPropertiesLock)
            {
                if (blockedActionProperties != null)
                    actionProperties.AddRange(blockedActionProperties);

                ActionProperties = new Dictionary<string, Action>();

                foreach (var action in actionProperties)
                {
                    ActionProperties[action.name] = action;
                    foreach (var wait in ActionWaitEvents)
                    {
                        if (wait.Value == action.name && action.active)
                        {
                            ((ManualResetEvent)wait.Key).Set();
                        }
                    }
                }
            }
        }

        private void MyStateWs_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var status = JObject.Parse(e.Message);
            if (status?["name"].ToString() == this.Name)
            {
                SetActionProperties(
                    status["fullState"]["actions"].ToObject<List<Action>>(),
                    status["fullState"]["blocked_actions"]?.ToObject<List<Action>>());
            }
        }

        /// <summary>
        /// Executes action on application. By default waits that action is available/not blocked before executing.
        /// </summary>
        /// <param name="actionName">Name of the action to perform</param>
        /// <param name="fields">Named fields that will be converted to JSON fields</param>
        /// <param name="plainText">Plain text body of the HTTP request</param>
        /// <param name="waitActive">If true, waits that the action becomes active i.e. is not blocked. 
        /// Action can be blocked (not active) when it is not safe to execute the action or the previous action is still ongoing.</param>
        /// <param name="waitActiveTimeout"></param>
        /// <exception cref="WaitTimeoutException">Throws WaitTimeutException, if waitActiveTimeout was defined and action did not come available during that time.</exception>
        public void ExecuteAction(string actionName, Dictionary<string, object> fields = null, string plainText = null, bool waitActive = true, int waitActiveTimeout = 0)
        {

            ManualResetEvent manualResetEvent = new ManualResetEvent(false);

            lock (ActionPropertiesLock)
            {

                if (!this.ActionProperties[actionName].active && waitActive)
                    ActionWaitEvents[manualResetEvent] = actionName;
            }
 
            if (waitActive)
            {
                var WaitResult = manualResetEvent.WaitOne(waitActiveTimeout);
                ActionWaitEvents.Remove(manualResetEvent);
                if (!WaitResult)
                    throw new WaitTimeoutException();
            }

            this.Actions[actionName](fields, plainText);
        }

        /// <summary>
        /// Returns state of the application. For stateful application state, for IO value of the IO and so on...
        /// </summary>
        public override string State
        {
            get
            {
                var resp = (Dictionary<string, object>)Actions["state"]();
                return resp["value"].ToString();
            }
        }

        /// <summary>
        /// All actions the application can perform
        /// </summary>
        public Dictionary<string, ActionDelegate> Actions { get; private set; }

        /// <summary>
        /// Lists all properties of the application
        /// </summary>
        public Dictionary<string, dynamic> Properties
        {
            get
            {
                var client = new RestClient(new Uri(Href));
                var request = new RestRequest("", Method.GET);

                request.AddHeader("Accept", "application/vnd.siren+json");

                //TODO: Validate response
                var content = (RestResponse<Entity>)client.Execute<Entity>(request);

                return content.Data.properties;
            }
        }

        public Dictionary<string, Action> ActionProperties { get; private set; }

        /// <summary>
        /// Name of the application
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Link to the application
        /// </summary>
        public string Href { get; private set; }

        protected override WebSocket stateWS => myStateWs;

        public Dictionary<object, string> ActionWaitEvents { get; private set; }

        protected override void CheckWaitStatus(JObject status)
        {
            if (StateWait != null &&
                status?["name"].ToString() == this.Name && this.StateWait.States.Any(st => st == status?["value"].ToString()))
                this.StateWait.WaitEvent.Set();
        }
    }
}
