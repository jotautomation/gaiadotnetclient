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

            var client = new JOTRestClient("http://172.23.225.118:1234");

            client.Populate();

            var o = client.Outputs;
            var i = client.Inputs;
            var a = client.StateApps;
            var r = client.Robots["MainRobot"];

            Console.WriteLine("State: " + client.State);

            while (!client.TestBoxClosing)
            {
                
                Thread.Sleep(10);
            }

            Console.WriteLine("Test box closing!");
            // Now DUT(s) is locked and it is safe to attach battery connector, USB etc.

            while (!client.ReadyForTesting)
            {                
                Thread.Sleep(10);
            }

            //Now test box is fully closed and we are ready for actual testing.
            Console.WriteLine("Ready for testing!");


            client.StateTriggers["Release"](new Dictionary<string, object> { { "testResult1", "pass" }, { "testResult2", "pass" }, { "testResult3", "pass" } });

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

