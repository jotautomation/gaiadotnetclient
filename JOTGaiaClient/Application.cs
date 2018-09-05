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
    public class Application<T> : ApplicationBase
    {
        public Application()
        {

        }

        public T State
        {
            get
            {
                var resp = (Dictionary<string,object>)Actions["state"]();
                return (T)Convert.ChangeType(resp["value"], typeof(T));
            }
        }

        /// <summary>
        /// Wait for state.
        /// </summary>
        /// <param name="state">State to wait</param>
        /// <param name="timeOut_ms">Timeout</param>
        /// <param name="pollInterval_ms">Poll interval</param>
        /// <returns>Returns true if state was reached before timeout.</returns>
        public bool TryWaitState(T state, int timeOut_ms = 5000, int pollInterval_ms = 10)
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
        public void WaitState(T state, int timeOut_ms = 5000, int pollInterval_ms = 10)
        {
            if (!TryWaitState(state, timeOut_ms, pollInterval_ms))
                throw new TimeoutException("Timeout while waiting " + state + " for " + this.Name + ". Current state: " + this.State);
        }

        public Application(string name, Dictionary<string, ActionDelegate> actions, string href)
            : base(name, actions, href)
        { }
    }

    public class ApplicationBase
    {
        public Dictionary<string, ActionDelegate> Actions { get; private set; }
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

        public ApplicationBase()
        {

        }

        public ApplicationBase(string name, Dictionary<string, ActionDelegate> actions, string href)
        {
            Name = name;
            Actions = actions;
            Href = href;
        }

        public string Name { get; private set; }
        public string Href { get; private set; }

    }
}
