using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JOT.GaiaClient
{
    public delegate object ActionDelegate(Dictionary<string, object> fields = null, string plainText = null);
    public class JOTGaiaClient : RestSharp.RestClient
    {

        public IReadOnlyDictionary<string, DigitalOutput> Outputs { get; private set; }
        public IReadOnlyDictionary<string, DigitalInput> Inputs { get; private set; }
        public IReadOnlyDictionary<string, Application<string>> StateApps { get; private set; }
        public IReadOnlyDictionary<string, Application<string>> Robots { get; private set; }
        public IReadOnlyDictionary<string, ActionDelegate> StateTriggers { get; private set; }
        public IReadOnlyDictionary<string, Application<string>> LightSources { get; private set; }
        public IReadOnlyDictionary<string, Application<string>> AudioTool { get; private set; }

        public IReadOnlyDictionary<string, Application<string>> AudioRouter { get; private set; }
        public IReadOnlyDictionary<string, Application<string>> WavePlayer { get; private set; }
        public IReadOnlyDictionary<string, Application<string>> WaveRecorder { get; private set; }


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

        public bool TestBoxClosing
        {
            get
            {
                if (this.State.Contains("Active_ClosingTestBox"))
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

        public JOTGaiaClient(string baseUrl) : base(baseUrl) { }

        public JOTGaiaClient(Uri baseUrl) : base(baseUrl) { }

        public void Populate()
        {
            var request = new RestRequest("api/applications", Method.GET);

            request.AddHeader("Accept", "application/vnd.siren+json");

            var response = (RestResponse<Siren>)Execute<Siren>(request);
           
            var appLoadTasks = new Task[] {
                Task.Run(() => {
                    Outputs = response.Data.GetApplications<DigitalOutput>("DigitalOutput");
                }),
                Task.Run(() => {
                    Inputs = response.Data.GetApplications<DigitalInput>("DigitalInput");
                }),
                Task.Run(() => {
                    StateApps = response.Data.GetApplications<Application<string>>("StatefulApplication");
                }),
                Task.Run(() => {
                    Robots = response.Data.GetApplications<Application<string>>("CncRobot");
                }),
                Task.Run(() => {
                    LightSources = response.Data.GetApplications<Application<string>>("LightSourceTool");
                }),
                Task.Run(() => {
                    AudioTool = response.Data.GetApplications<Application<string>>("AudioTool");
                }),
                Task.Run(() => {
                    AudioRouter = response.Data.GetApplications<Application<string>>("AudioRouter");
                }),
                Task.Run(() => {
                    WavePlayer = response.Data.GetApplications<Application<string>>("WavePlayer");
                }),
                Task.Run(() => {
                    WaveRecorder = response.Data.GetApplications<Application<string>>("WaveRecorder");
                })};


            Task.WaitAll(appLoadTasks);

            request = new RestRequest("api", Method.GET);

            request.AddHeader("Accept", "application/vnd.siren+json");

            response = (RestResponse<Siren>)Execute<Siren>(request);
            StateTriggers = response.Data.GetStateTriggers();
        }
    }
}
