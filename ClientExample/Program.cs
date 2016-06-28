using JOT.RESTClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JOT.Client
{
    class Program
    {
        static void Main(string[] args)
        {

            Thread.Sleep(5000);

            var client = new JOTRestClient("http://localhost:1234/");

            client.FetchApplications();

            var o = client.Outputs;
            var i = client.Inputs;
            var a = client.StateApps;

            a["MyStatefulApplication"].Actions["trigger-FirstState"]();

            o["Output1"].SetOutput(true);

            Console.WriteLine(o["Output1"].State);
            Console.WriteLine(a["MyStatefulApplication"].State);
            Console.ReadLine();
        }
    }
}

