using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace self_signed_exe_updater.deps.debugger
{
    public class ArgumentedUpdate<T> 
    {
        public T? Argument { protected set; get; }
        public DateTime Timestamp { get; protected set; }
        public ArgumentedUpdate(T argument, DateTime stamp)
        {
            Argument = argument;
            Timestamp = stamp;
        }
        public override string ToString()
        {
            return (Argument.ToString()) + " " + Timestamp.ToString();
        }
    }
}
