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
    public class Application:Waitable
    {
        private string myName;

        public Application(string name, Dictionary<string, ActionDelegate> actions, string href, WebSocket ws)
            :base(ws)

        {
            myName = name;
            Actions = actions;
            Href = href;
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

        /// <summary>
        /// Name of the application
        /// </summary>
        public override string Name { get => myName; }

        /// <summary>
        /// Link to the application
        /// </summary>
        public string Href { get; private set; }

        protected override void CheckWaitStatus(JObject status)
        {
            if (status?["name"].ToString() == this.Name && status?["value"].ToString() == this.StateWait?.State)
                this.StateWait.WaitEvent.Set();
        }
    }
}
