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
            #region initialize connection
            // Connect to the test box
            var client = new JOTGaiaClient("http://ci.jot.local:1234");

            // Fetch applications from the test box
            client.Populate();

            //Get state of the tester
            Console.WriteLine("State: " + client.State);

            // This is how you get properties of application. For example here we get current position of X-axle of main robot.
            Console.WriteLine(client.Applications["mainrobot"].Properties["position"]["x"]);
            #endregion

            #region test sequence
            while (true)
            {
                //From here starts the actual test sequence 

                // Step 1: We are waiting that the test box gets ready and operator puts DUT(s) in

                // (Todo: implement waiting witht out polling)
                while (!(client.TestBoxClosing || client.ReadyForTesting))
                {

                    Thread.Sleep(10);
                }

                // Step 2: Operator did put the DUT(s) in. DUT(s) is locked and it is safe to attach battery connector, USB etc.
                // The test box is still closing so it is not audio or RF shielded and robot actions are not allowed

                Console.WriteLine("Test box closing!");

                // Wait that the test box is closed and ready for testing
                // (Todo: implement waiting witht out polling)
                while (!client.ReadyForTesting)
                {
                    Thread.Sleep(10);
                }

                // Step 3: Test box is fully closed and we are ready for actual testing.
                Console.WriteLine("Ready for testing!");
                #region Control commands examples 
                // Execute the tests. Here's some examples.

                // Change robot tool. Note! Tool change can be defined also in G-code but some time you can save time by changing the tool
                // while doing something else.
                client.Applications["MainRobot"].Actions["changeTo-AudioTool"]();

                // Run robot movement. See GcodeExample.GCode for g-code example and also how to define tool on g-code.
                // Note that for safety reason when g-code is modified it will run once with low speed and power.
                // So if you made mistake and robot collides it won't brake anything
                client.Applications["MainRobot"].Actions["cnc_run"](plainText: GcodeExample.GCode);

                // Push button on DUT with pusher
                client.Applications["SideButtonPusher"].Actions["Push"]();

                // Optionally wait that pusher is on end position (detected by sensor)
                client.Applications["SideButtonPusher"].WaitState("Push");

                // Release pusher
                client.Applications["SideButtonPusher"].Actions["Release"]();

                // Record audio
                client.Applications["WaveRecorder"].Actions["record-wave"](new Dictionary<string, object> { { "time_s", 2 }, { "filename", "testrecord.wav" } });

                // Play audio
                client.Applications["WavePlayer"].Actions["play-wave"](new Dictionary<string, object> { { "filename", "sine_1000Hz_-3dBFS_3s.wav" } });


                // Step 4: Testing is ready and we release the DUT and give test result so that test box can indicate it to operator
                client.StateTriggers["Release"](new Dictionary<string, object> { { "testResult1", "pass" }, { "testResult2", "pass" }, { "testResult3", "fail" } });
                #endregion
            }
            #endregion
        }
    }
}



