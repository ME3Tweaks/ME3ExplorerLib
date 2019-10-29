using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using StreamHelpers;

namespace ME3Explorer.Packages
{
    public static class MEPackageHandler
    {
        static Func<string, UDKPackage> UDKConstructorDelegate;
        static Func<string, MEGame, MEPackage> MEConstructorDelegate;

        public static void Initialize()
        {
            UDKConstructorDelegate = UDKPackage.Initialize();
            MEConstructorDelegate = MEPackage.Initialize();
        }

        public static IMEPackage OpenMEPackage(string pathToFile)
        {
            IMEPackage package;
            pathToFile = Path.GetFullPath(pathToFile); //STANDARDIZE INPUT
            ushort version;
            ushort licenseVersion;

            using (FileStream fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(4, SeekOrigin.Begin);
                version = fs.ReadUInt16();
                licenseVersion = fs.ReadUInt16();
            }


            if (version == 684 && licenseVersion == 194 ||
                version == 512 && licenseVersion == 130 ||
                version == 491 && licenseVersion == 1008)
            {
                return MEConstructorDelegate(pathToFile, MEGame.Unknown);
            }

            if (version == 868 && licenseVersion == 0)
            {
                //UDK
                return UDKConstructorDelegate(pathToFile);
            }
            throw new FormatException("Not an ME1, ME2, ME3, or UDK package file.");
        }

        public static void CreateAndSaveMePackage(string path, MEGame game)
        {
            MEConstructorDelegate(path, game).save();
        }

        public static IMEPackage OpenME3Package(string pathToFile)
        {
            IMEPackage pck = OpenMEPackage(pathToFile);
            if (pck.Game == MEGame.ME3)
            {
                return pck;
            }

            throw new FormatException("Not an ME3 package file.");
        }

        public static IMEPackage OpenME2Package(string pathToFile)
        {
            IMEPackage pck = OpenMEPackage(pathToFile);
            if (pck.Game == MEGame.ME2)
            {
                return pck;
            }

            throw new FormatException("Not an ME2 package file.");
        }

        public static IMEPackage OpenME1Package(string pathToFile)
        {
            IMEPackage pck = OpenMEPackage(pathToFile);
            if (pck.Game == MEGame.ME1)
            {
                return pck;
            }

            throw new FormatException("Not an ME1 package file.");
        }
    }
}