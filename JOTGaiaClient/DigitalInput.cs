﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOT.GaiaClient
{
    public class DigitalInput : Application<bool>
    {
        public DigitalInput()
        {

        }

        public DigitalInput(string name, Dictionary<string, ActionDelegate> actions, string href) : base(name, actions, href)
        {
        }
    }
}
