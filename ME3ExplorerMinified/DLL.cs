using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME1Explorer.Unreal;
using ME2Explorer.Unreal;
using ME3Explorer;
using ME3Explorer.Packages;
using ME3Explorer.Unreal;

namespace ME3ExplorerMinified
{
    public static class DLL
    {
        public static string BugReportURL = "https://github.com/ME3Tweaks/ME3ExplorerMinified";
        private static bool booted = false;
#if DEBUG
        public static bool IsDebug => true;
#else
        public static bool IsDebug => false;
#endif

        public static void Startup()
        {
            if (!booted)
            {
                ME1UnrealObjectInfo.loadfromJSON();
                ME2UnrealObjectInfo.loadfromJSON();
                ME3UnrealObjectInfo.loadfromJSON();
                UDKUnrealObjectInfo.loadfromJSON();
                ME1Directory.LoadGamePath();
                ME2Directory.LoadGamePath();
                ME3Directory.LoadGamePath();
                MEPackageHandler.Initialize();
                booted = true;
            }
        }
    }
}