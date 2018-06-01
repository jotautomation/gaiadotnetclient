using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace JOT.RESTClient
{
    public class JOTRestClient : RestSharp.RestClient
    {
        public IReadOnlyDictionary<string, DigitalOutput> Outputs { get; private set; }
        public IReadOnlyDictionary<string, DigitalInput> Inputs { get; private set; }
        public IReadOnlyDictionary<string, Application<string>> StateApps { get; private set; }

        public JOTRestClient(string baseUrl) : base(baseUrl) { }

        public JOTRestClient(Uri baseUrl) : base(baseUrl) { }

        public void FetchApplications()
        {
            var request = new RestRequest("api/applications", Method.GET);

            request.AddHeader("Accept", "application/vnd.siren+json");

            var response = (RestResponse<Siren>)Execute<Siren>(request);

            Outputs = response.Data.GetApplications<DigitalOutput>("DigitalOutput");
            Inputs = response.Data.GetApplications<DigitalInput>("DigitalInput");
            StateApps = response.Data.GetApplications<Application<string>>("StatefulApplication");
        }
    }
}
