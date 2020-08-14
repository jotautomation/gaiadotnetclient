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

        public Application(string name, Dictionary<string, ActionDelegate> actions, List<Action> actionProperties, List<Action> blockedActionProperties, string href, WebSocket ws) : base(name)

        {
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
            if (blockedActionProperties != null)
                actionProperties.AddRange(blockedActionProperties);

            ActionProperties = new Dictionary<string, Action>();

            foreach (var action in actionProperties)
            {
                ActionProperties[action.name] = action;
            }           
        }

        private void MyStateWs_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var status = JObject.Parse(e.Message);
            if(status?["name"].ToString() == this.Name)
            {
                SetActionProperties(
                    status["fullState"]["actions"].ToObject<List<Action>>(),
                    status["fullState"]["blocked_actions"]?.ToObject<List<Action>>());
            }
        }

        /// <summary>
        /// Returns state if the application. For stateful application state, for IO value of the IO and so on...
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
        /// All actions that application can perform
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

        protected override void CheckWaitStatus(JObject status)
        {
            if (StateWait != null &&
                status?["name"].ToString() == this.Name && this.StateWait.States.Any(st => st == status?["value"].ToString()))
                this.StateWait.WaitEvent.Set();
        }
    }
}
