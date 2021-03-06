﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ini;
using ME3Explorer.Packages;

namespace ME3Explorer
{
    public static class MELoadedFiles
    {
        private static readonly string[] ME1FilePatterns = { "*.u", "*.upk", "*.sfm" };
        private const string ME2and3FilePattern = "*.pcc";

        private static readonly string[] ME2and3FilePatternIncludeTFC = { "*.pcc", "*.tfc" };

        private static Dictionary<string, string> cachedME1LoadedFiles;
        private static Dictionary<string, string> cachedME2LoadedFiles;
        private static Dictionary<string, string> cachedME3LoadedFiles;
        /// <summary>
        /// Gets a Dictionary of all loaded files in the given game. Key is the filename, value is file path
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetFilesLoadedInGame(MEGame game, bool forceReload = false, bool includeTFC = false)
        {
            if (!forceReload)
            {
                if (game == MEGame.ME1 && cachedME1LoadedFiles != null) return cachedME1LoadedFiles;
                if (game == MEGame.ME2 && cachedME2LoadedFiles != null) return cachedME2LoadedFiles;
                if (game == MEGame.ME3 && cachedME3LoadedFiles != null) return cachedME3LoadedFiles;
            }

            //make dictionary from basegame files
            var loadedFiles = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (string directory in GetEnabledDLC(game).OrderBy(dir => GetMountPriority(dir, game)).Prepend(MEDirectories.BioGamePath(game)))
            {
                foreach (string filePath in GetCookedFiles(game, directory,includeTFC))
                {
                    string fileName = Path.GetFileName(filePath);
                    if (fileName != null) loadedFiles[fileName] = filePath;
                }
            }

            if (game == MEGame.ME1) cachedME1LoadedFiles = loadedFiles;
            if (game == MEGame.ME2) cachedME2LoadedFiles = loadedFiles;
            if (game == MEGame.ME3) cachedME3LoadedFiles = loadedFiles;

            return loadedFiles;
        }

        public static IEnumerable<string> GetAllFiles(MEGame game) => GetEnabledDLC(game).Prepend(MEDirectories.BioGamePath(game)).SelectMany(directory => GetCookedFiles(game, directory));

        private static IEnumerable<string> GetCookedFiles(MEGame game, string directory, bool includeTFCs = false)
        {
            if (game == MEGame.ME1)
                return ME1FilePatterns.SelectMany(pattern => Directory.EnumerateFiles(Path.Combine(directory, "CookedPC"), pattern, SearchOption.AllDirectories));
            if (includeTFCs)
            {
                return ME2and3FilePatternIncludeTFC.SelectMany(pattern => Directory.EnumerateFiles(Path.Combine(directory, game == MEGame.ME3 ? "CookedPCConsole" : "CookedPC"), pattern, SearchOption.AllDirectories));
            }
            else
            {
                return Directory.EnumerateFiles(Path.Combine(directory, game == MEGame.ME3 ? "CookedPCConsole" : "CookedPC"), ME2and3FilePattern);
            }
        }

        /// <summary>
        /// Gets the base DLC directory of each unpacked DLC/mod that will load in game (eg. C:\Program Files (x86)\Origin Games\Mass Effect 3\BIOGame\DLC\DLC_EXP_Pack001)
        /// Directory Override is used to use a custom path, for things like TFC Compactor, where the directory ME3Exp is pointing to may not be the one you want to use.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetEnabledDLC(MEGame game, string directoryOverride = null) =>
            Directory.Exists(MEDirectories.DLCPath(game))
                ? Directory.EnumerateDirectories(directoryOverride ?? MEDirectories.DLCPath(game)).Where(dir => IsEnabledDLC(dir, game))
                : Enumerable.Empty<string>();

        public static string GetMountDLCFromDLCDir(string dlcDirectory, MEGame game) => Path.Combine(dlcDirectory, game == MEGame.ME3 ? "CookedPCConsole" : "CookedPC", "Mount.dlc");

        public static bool IsEnabledDLC(string dir, MEGame game)
        {
            string dlcName = Path.GetFileName(dir);
            if (game == MEGame.ME1)
            {
                return ME1Directory.OfficialDLC.Contains(dlcName) || File.Exists(Path.Combine(dir, "AutoLoad.ini"));
            }
            return dlcName.StartsWith("DLC_") && File.Exists(GetMountDLCFromDLCDir(dir, game));
        }

        public static int GetMountPriority(string dlcDirectory, MEGame game)
        {
            if (game == MEGame.ME1)
            {
                int idx = 1 + ME1Directory.OfficialDLC.IndexOf(Path.GetFileName(dlcDirectory));
                if (idx > 0)
                {
                    return idx;
                }
                //is mod
                string autoLoadPath = Path.Combine(dlcDirectory, "AutoLoad.ini");
                IniFile dlcAutoload = new IniFile(autoLoadPath);
                return Convert.ToInt32(dlcAutoload.ReadValue("ME1DLCMOUNT", "ModMount"));
            }
            return MountFile.GetMountPriority(GetMountDLCFromDLCDir(dlcDirectory, game));
        }

        public static string GetDLCNameFromDir(string dlcDirectory) => Path.GetFileName(dlcDirectory).Substring(4);
    }
}
