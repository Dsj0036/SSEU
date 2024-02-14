using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace self_signed_exe_updater.deps.debugger
{
    public class Tracefile
    {
        public bool LocalCopy { get; set; } = false;
        public readonly string FileName;
        public readonly long Length;
        public venum<string> Data { get; protected set; }
        public venum<ArgumentedUpdate<int>> Changed { get; protected set; }
        public Tracefile(string filename, bool localcopy)
        {
            LocalCopy = localcopy;
            FileName = filename;
            Length = FTP.GetLength(filename);
            if (Length == 0)
            {
                throw new InvalidOperationException("File doesnt exists");
            }
            else if (localcopy)
            {
                
            }
        }
        /// <summary>
        /// Performs check+update operation
        /// </summary>
        public void PerformUC()
        {
            
        }
    }
}
