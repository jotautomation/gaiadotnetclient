﻿using JOT.ClientExample;
using JOT.GaiaClient;
using System;
using System.Collections.Generic;
using System.Threading;

namespace JOT.Client
{
    class Program
    {
        static void Main(string[] args)
        {

            var client = new JOTGaiaClient("http://172.23.225.118:1234");

            client.Populate();

            var o = client.Outputs;
            var i = client.Inputs;
            var a = client.StateApps;
            var r = client.Robots["MainRobot"];

            Console.WriteLine("State: " + client.State);

            //Note! There is a bug on API here. Positions are on array thus we need "extra" [0] in the middle.
            //This will be fixed someday
            Console.WriteLine(r.Properties["position"][0]["x"]);

            while (true)
            {
                //This is normal test sequence. 

                // Step 1: We are waiting that test box gets ready and operator puts DUT in

                while (!(client.TestBoxClosing || client.ReadyForTesting))
                {

                    Thread.Sleep(10);
                }

                // Step 2. Operator did put DUT in. DUT(s) is locked and it is safe to attach battery connector, USB etc.
                // Test box is still closing so it is not audio or RF shielded and robot actions are not allowed

                Console.WriteLine("Test box closing!");

                while (!client.ReadyForTesting)
                {
                    Thread.Sleep(10);
                }

                // Step 3: Test box is fully closed and we are ready for actual testing.
                Console.WriteLine("Ready for testing!");

                // Step 4: Testing is ready and we release the DUT and give test result so that test box can indicate it to operator
                client.StateTriggers["Release"](new Dictionary<string, object> { { "testResult1", "pass" }, { "testResult2", "pass" }, { "testResult3", "fail" } });
            }

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

            // TODO: Add real application here
            Console.WriteLine(a["RobotToolLock"].State);

            Thread.Sleep(100);

            a["SideButtonPresser"].Actions["set-Work"]();

            Console.WriteLine(a["RobotToolLock"].State);

            o["Output1"].SetOutput(true);

            Console.WriteLine(o["Output1"].State);
            Console.WriteLine(a["MyStatefulApplication"].State);
            Console.ReadLine();
        }
    }
}

