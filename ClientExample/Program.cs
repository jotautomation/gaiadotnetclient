using JOT.ClientExample;
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

            var client = new JOTRestClient("http://192.168.133.130:1234");

            client.Populate();

            var o = client.Outputs;
            var i = client.Inputs;
            var a = client.StateApps;
            var r = client.Robots["MainRobot"];


            client.StateTriggers["Release"](new Dictionary<string, object> { { "testResult1", "pass" }, { "testResult2", "pass" }, { "testResult3", "fail" } });

            // Change to FingerBase too. Note! Tool change can be defined also in G-code
            r.Actions["changeTo-FingerBase"]();

            r.Actions["cnc_run"](plainText:GcodeExample.GCode);

            /*
             * // Here is example how to wait for application to go to certain state
            Task.Run(() =>
            {
                a["MyStatefulApplication"].WaitState("FirstState");
                Console.WriteLine("Hep!");
            }
            );
            */
            Console.WriteLine(a["RobotToolLock"].State);

            Thread.Sleep(100);

            a["OduMacControl"].Actions["set-Work"]();

            Console.WriteLine(a["RobotToolLock"].State);

            o["Output1"].SetOutput(true);

            Console.WriteLine(o["Output1"].State);
            Console.WriteLine(a["MyStatefulApplication"].State);
            Console.ReadLine();
        }
    }
}

