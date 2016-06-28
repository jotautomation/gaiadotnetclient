using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JOT.RESTClient
{
    public class Application<T> : ApplicationBase
    {
        public Application()
        {

        }

        public T State
        {
            get
            {
                return (T)Convert.ChangeType(Actions["get-state"](), typeof(T));
            }
        }

        public Application(string name, Dictionary<string, Func<object>> actions)
            : base(name, actions)
        { }
    }

    public class ApplicationBase
    {
        public Dictionary<string, Func<object>> Actions { get; private set; }

        public ApplicationBase()
        {

        }

        public ApplicationBase(string name, Dictionary<string, Func<object>> actions)
        {
            Name = name;
            Actions = actions;
        }

        public string Name { get; private set; }

    }
}
