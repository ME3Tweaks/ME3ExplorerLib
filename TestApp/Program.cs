using ME3Explorer.Packages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.WriteLine("Starting up");

            ME3ExplorerMinified.DLL.Startup();
            Debug.WriteLine("Started up");
            MEPackageHandler.OpenMEPackage("");
        }
    }
}
