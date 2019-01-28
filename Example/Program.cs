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

            var client = new JOTGaiaClient("http://172.23.225.84:1234");

            client.Populate();

            var StatefulApplications = client.StateApps;
            var Robot = client.Robots["MainRobot"];
            var WavePlayerDefault = client.WavePlayer["DefaultAudioOut"];
            var WaveRecorderDefault = client.WaveRecorder["DefaultAudioIn"];

         //   WaveRecorderDefault.Actions["record-wave"](new Dictionary<string, object> { { "time_s", 2 }, { "filename", "testrecord.wav" } });
         //   WavePlayerDefault.Actions["play-wave"](new Dictionary<string, object> { { "filename", "sine_1000Hz_-3dBFS_3s.wav" } });

            Console.WriteLine("State: " + client.State);

            // This is how you get properties of application. For example here we get current position of X-axle of main robot.
            Console.WriteLine(Robot.Properties["position"]["x"]);


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
              //  StatefulApplications["LeftToolMagazine"].Actions["set-Work"]();

                Robot.Actions["cnc_run"](plainText: GcodeExample.GCode);
                Robot.WaitState("Ready", timeOut_ms:200000);
                // Step 3: Test box is fully closed and we are ready for actual testing.
                Console.WriteLine("Ready for testing!");

                // Step 4: Testing is ready and we release the DUT and give test result so that test box can indicate it to operator
               // client.StateTriggers["Release"](new Dictionary<string, object> { { "testResult1", "pass" }, { "testResult2", "pass" }, { "testResult3", "fail" } });
            }

            // Change to FingerBase too. Note! Tool change can be defined also in G-code

            Robot.Actions["cnc_run"](plainText: GcodeExample.GCode);

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
            Console.WriteLine(StatefulApplications["RobotToolLock"].State);

            Thread.Sleep(100);

            StatefulApplications["SideButtonPresser"].Actions["set-Work"]();

            Console.WriteLine(StatefulApplications["RobotToolLock"].State);


            Console.WriteLine(StatefulApplications["MyStatefulApplication"].State);
            Console.ReadLine();
        }
    }
}


