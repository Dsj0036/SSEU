using self_signed_exe_updater.deps.debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace self_signed_exe_updater.deps
{
    public class Debugger
    {
        public bool Listening { get; set; }
        public string Address { get; private set; }
        public string Name { get; private set; }
        public const string TRACEFILE = "/dev_hdd0/tmp/tracefile.log";
        public Debugger(string address, string name)
        {
            Address = address;
        }
        // soon
    }
}
