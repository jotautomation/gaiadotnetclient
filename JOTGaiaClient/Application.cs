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
        public Application(string name, Dictionary<string, ActionDelegate> actions, string href)
        {
            Name = name;
            Actions = actions;
            Href = href;
        }

        /// <summary>
        /// Returns state if the application. For stateful application state, for IO value of the IO and so on...
        /// </summary>
        public string State
        {
            get
            {
                var resp = (Dictionary<string,object>)Actions["state"]();
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
        /// <param name="timeOut_ms">Timeout</param>
        /// <param name="pollInterval_ms">Poll interval</param>
        /// <returns>Returns true if state was reached before timeout.</returns>
        public bool TryWaitState(string state, int timeOut_ms = 5000, int pollInterval_ms = 100)
        {
            var sw = new Stopwatch();
            sw.Start();

            while (!State.Equals(state))
            {
                if (sw.ElapsedMilliseconds > timeOut_ms)
                    return false;
                Thread.Sleep(pollInterval_ms);
            }
            return true;
        }

        /// <summary>
        /// Wait for state. Throws exception if timeout occurs.
        /// </summary>
        /// <param name="state">State to wait</param>
        /// <param name="timeOut_ms">Timeout</param>
        /// <param name="pollInterval_ms">Poll interval</param>
        public void WaitState(string state, int timeOut_ms = 5000, int pollInterval_ms = 100)
        {
            if (!TryWaitState(state, timeOut_ms, pollInterval_ms))
                throw new TimeoutException("Timeout while waiting " + state + " for " + this.Name + ". Current state: " + this.State);
        }

    }
}
