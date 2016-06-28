﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JOT.RESTClient
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
                return (T)Convert.ChangeType(Actions["get-state"](), typeof(T));
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

        public Application(string name, Dictionary<string, Func<object>> actions)
            : base(name, actions)
        { }
    }

    public class ApplicationBase
    {
        public Dictionary<string, Func<object>> Actions { get; private set; }

        public ApplicationBase()
        {

        }

        public ApplicationBase(string name, Dictionary<string, Func<object>> actions)
        {
            Name = name;
            Actions = actions;
        }

        public string Name { get; private set; }

    }
}
