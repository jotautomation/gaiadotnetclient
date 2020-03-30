using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace JOT.GaiaClient
{
    public abstract class Waitable
    {
        internal static readonly Object waitlock = new Object();
        protected StateWaitStruct StateWait = null;

        public abstract string State { get; }
        public abstract string Name { get; }

        public Waitable(WebSocket ws)
        {
            ws.MessageReceived += App_state_websocket_MessageReceived;
        }

        private void App_state_websocket_MessageReceived(object sender, WebSocket4Net.MessageReceivedEventArgs e)
        {
            lock (waitlock)
            {
                var status = JObject.Parse(e.Message);
                CheckWaitStatus(status);
            }
        }

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

        protected abstract void CheckWaitStatus(JObject status);

        protected class StateWaitStruct
        {
            internal string State { get; set; }
            internal ManualResetEvent WaitEvent { get; set; }
        }
    }
}
