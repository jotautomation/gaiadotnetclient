using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOT.RESTClient
{
    public class DigitalOutput : DigitalInput
    {
        public DigitalOutput()
        {

        }
        public DigitalOutput(string name, Dictionary<string, ActionDelegate> actions) : base(name, actions)
        {
        }

        public void SetOutput(bool value)
        {
            if (value)
                this.Actions["set-state-on"]();
            else
                this.Actions["set-state-off"]();
        }
    }
}