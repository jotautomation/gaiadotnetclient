using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JOT.GaiaClient
{
    /// <summary>
    /// Everything you can control on the machine is application. 
    /// Moving parts on mechanics, robots, electronics...
    /// </summary>
    public class Application
    {

        StateWaitStruct StateWait = null;
        private static readonly Object waitlock = new Object();

        public Application(string name, Dictionary<string, ActionDelegate> actions, string href, JOTGaiaClient gaiaClient)
        {
            var GaiaClient = gaiaClient;
            GaiaClient.app_state_websocket.MessageReceived += App_state_websocket_MessageReceived;
            Name = name;
            Actions = actions;
            Href = href;
        }

        private void App_state_websocket_MessageReceived(object sender, WebSocket4Net.MessageReceivedEventArgs e)
        {
            lock (waitlock)
            {
                var details = JObject.Parse(e.Message);
                if (details?["name"].ToString() == this.Name && details?["value"].ToString() == this.StateWait?.State)
                    this.StateWait.WaitEvent.Set();
            }
        }

        /// <summary>
        /// Returns state if the application. For stateful application state, for IO value of the IO and so on...
        /// </summary>
        public string State
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
        public string Name { get; private set; }

        /// <summary>
        /// Link to the application
        /// </summary>
        public string Href { get; private set; }

        /// <summary>
        /// Wait for state.
        /// </summary>
        /// <param name="state">State to wait</param>
        /// <param name="timeOut_ms">The number of milliseconds to wait, or System.Threading.Timeout.Infinite (-1)
        ///     to wait indefinitely.</param>
        /// <returns>Returns true if state was reached before timeout.</returns>
        public bool TryWaitState(string state, int timeOut_ms = -1)
        {
            var waitEvent = new ManualResetEvent(false);

            lock (waitlock)
            {
                this.StateWait = new StateWaitStruct() { State = state, WaitEvent = waitEvent };

                // Check initial state
                if (this.State == state)
                    waitEvent.Set();
            }
  
            return waitEvent.WaitOne(timeOut_ms);
        }

        /// <summary>
        /// Wait for state. Throws exception if timeout occurs.
        /// </summary>
        /// <param name="state">State to wait</param>
        /// <param name="timeOut_ms">The number of milliseconds to wait, or System.Threading.Timeout.Infinite (-1)
        ///     to wait indefinitely.</param>
        public void WaitState(string state, int timeOut_ms = -1)
    {
        if (!TryWaitState(state, timeOut_ms))
            throw new TimeoutException("Timeout while waiting " + state + " for " + this.Name + ". Current state: " + this.State);
    }

}
class StateWaitStruct
{
    internal string State { get; set; }
    internal ManualResetEvent WaitEvent { get; set; }
}
}
