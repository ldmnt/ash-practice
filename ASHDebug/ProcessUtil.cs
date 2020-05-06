using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASHDebug
{
    public class ProcessUtil
    {
        public static Process FindByName(string name)
        {
            if (Process.GetProcessesByName(name).Length > 0)
            {
                var process = Process.GetProcessesByName(name)[0];
                return process;
            }
            return null;
        }
    }
}
