﻿using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace JOT.RESTClient
{
    public delegate object ActionDelegate(Dictionary<string, object> fields = null, string plainText = null);
    public class JOTRestClient : RestSharp.RestClient
    {

        public IReadOnlyDictionary<string, DigitalOutput> Outputs { get; private set; }
        public IReadOnlyDictionary<string, DigitalInput> Inputs { get; private set; }
        public IReadOnlyDictionary<string, Application<string>> StateApps { get; private set; }
        public IReadOnlyDictionary<string, Application<string>> Robots { get; private set; }
        public IReadOnlyDictionary<string, ActionDelegate> StateTriggers { get; private set; }
        public bool ReadyForTesting
        {
            get
            {
                if (this.State.Contains("Executing"))
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

                var response = (RestResponse<Siren>)Execute<Siren>(request);
                return response.Data.properties["state"];
            }
        }

        public JOTRestClient(string baseUrl) : base(baseUrl) { }

        public JOTRestClient(Uri baseUrl) : base(baseUrl) { }

        public void Populate()
        {
            var request = new RestRequest("api/applications", Method.GET);

            request.AddHeader("Accept", "application/vnd.siren+json");

            var response = (RestResponse<Siren>)Execute<Siren>(request);

            Outputs = response.Data.GetApplications<DigitalOutput>("DigitalOutput");
            Inputs = response.Data.GetApplications<DigitalInput>("DigitalInput");
            StateApps = response.Data.GetApplications<Application<string>>("StatefulApplication");
            Robots = response.Data.GetApplications<Application<string>>("CncRobot");

            request = new RestRequest("api", Method.GET);

            request.AddHeader("Accept", "application/vnd.siren+json");

            response = (RestResponse<Siren>)Execute<Siren>(request);
            StateTriggers = response.Data.GetStateTriggers();
        }
    }
}
