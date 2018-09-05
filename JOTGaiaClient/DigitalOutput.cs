using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOT.GaiaClient
{
    public class DigitalOutput : DigitalInput
    {
        public DigitalOutput()
        {

        }
        public DigitalOutput(string name, Dictionary<string, ActionDelegate> actions, string href) : base(name, actions, href)
        {
        }

        public void SetOutput(bool value)
        {
            if (value)
                this.Actions["set-ON"]();
            else
                this.Actions["set-OFF"]();
        }
    }
}