using JOT.ClientExample;
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

            var applications = client.StateApps;
            var robot = client.Robots["MainRobot"];

            //var percentage = 50;
            //client.LightSources["RobotLight3200k"].Actions["set-percentage"](new Dictionary<string, object>() { { "value", percentage } });



            Console.WriteLine("State: " + client.State);

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

                applications["BatteryConnector"].Actions["set-Work"]();

                while (!client.ReadyForTesting)
                {
                    Thread.Sleep(10);
                }

                // Step 3: Test box is fully closed and we are ready for actual testing.
                Console.WriteLine("Ready for testing!");


                robot.Actions["cnc_run"](plainText: GcodeExample.GCode);

                Thread.Sleep(1000);

                robot.WaitState("Ready", 30000);

                applications["BatteryConnector"].Actions["set-Home"]();
                
                // Step 4: Testing is ready and we release the DUT and give test result so that test box can indicate it to operator
                client.StateTriggers["Release"](new Dictionary<string, object> { { "testResult1", "pass" }, { "testResult2", "pass" }, { "testResult3", "fail" } });
            }

            // Change to FingerBase too. Note! Tool change can be defined also in G-code
            robot.Actions["changeTo-FingerBase"]();

            /*
             * // Here is example how to wait for application to go to certain state
            Task.Run(() =>
            {
                a["MyStatefulApplication"].WaitState("FirstState");
                Console.WriteLine("Hep!");
            }
            );
            */

        
            Console.WriteLine(applications["MyStatefulApplication"].State);
            Console.ReadLine();
        }
    }
}

