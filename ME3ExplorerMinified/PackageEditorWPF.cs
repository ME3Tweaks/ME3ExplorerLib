using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3Explorer.Packages;
using ME3Explorer.Unreal;
using ME3Explorer.Unreal.BinaryConverters;

namespace ME3ExplorerMinified
{
    class PackageEditorWPF
    {
        /// <summary>
        /// Adds an import from the importingPCC to the destinationPCC with the specified importFullName, or returns the existing one if it can be found. 
        /// This will add parent imports and packages as neccesary
        /// </summary>
        /// <param name="importFullName">GetFullPath() of an import from ImportingPCC</param>
        /// <param name="importingPCC">PCC to import imports from</param>
        /// <param name="destinationPCC">PCC to add imports to</param>
        /// <param name="forcedLink">force this as parent</param>
        /// <returns></returns>
        public static IEntry getOrAddCrossImportOrPackage(string importFullName, IMEPackage importingPCC, IMEPackage destinationPCC, int? forcedLink = null)
        {
            if (string.IsNullOrEmpty(importFullName))
            {
                return null;
            }

            //see if this import exists locally
            foreach (ImportEntry imp in destinationPCC.Imports)
            {
                if (imp.GetFullPath == importFullName)
                {
                    return imp;
                }
            }

            //see if this is an export Package and exists locally
            foreach (ExportEntry exp in destinationPCC.Exports)
            {
                if (exp.ClassName == "Package" && exp.GetFullPath == importFullName)
                {
                    return exp;
                }
            }

            if (forcedLink is int link)
            {
                ImportEntry importingImport = importingPCC.Imports.First(x => x.GetFullPath == importFullName); //this shouldn't be null
                var newImport = new ImportEntry(destinationPCC)
                {
                    idxLink = link,
                    idxClassName = destinationPCC.FindNameOrAdd(importingImport.ClassName),
                    idxObjectName = destinationPCC.FindNameOrAdd(importingImport.ObjectName),
                    idxPackageFile = destinationPCC.FindNameOrAdd(importingImport.PackageFile)
                };
                destinationPCC.addImport(newImport);
                return newImport;
            }

            string[] importParts = importFullName.Split('.');

            //recursively ensure parent package exists. when importParts.Length == 1, this will return null
            IEntry parent = getOrAddCrossImportOrPackage(string.Join(".", importParts.Take(importParts.Length - 1)), importingPCC, destinationPCC);


            foreach (ImportEntry imp in importingPCC.Imports)
            {
                if (imp.GetFullPath == importFullName)
                {
                    var import = new ImportEntry(destinationPCC)
                    {
                        idxLink = parent?.UIndex ?? 0,
                        idxClassName = destinationPCC.FindNameOrAdd(imp.ClassName),
                        idxObjectName = destinationPCC.FindNameOrAdd(imp.ObjectName),
                        idxPackageFile = destinationPCC.FindNameOrAdd(imp.PackageFile)
                    };
                    destinationPCC.addImport(import);
                    return import;
                }
            }

            foreach (ExportEntry exp in importingPCC.Exports)
            {
                if (exp.ClassName == "Package" && exp.GetFullPath == importFullName)
                {
                    importExport(destinationPCC, exp, parent?.UIndex ?? 0, out ExportEntry package);
                    return package;
                }
            }

            throw new Exception($"Unable to add {importFullName} to file! Could not find it!");
        }

        /// <summary>
        /// Imports an export from another package file.
        /// </summary>
        /// <param name="mePackage"></param>
        /// <param name="ex">Export object from the other package to import</param>
        /// <param name="link">Local parent node UIndex</param>
        /// <param name="outputEntry">Newly generated export entry reference</param>
        /// <returns></returns>
        private static bool importExport(IMEPackage mePackage, ExportEntry ex, int link, out ExportEntry outputEntry)
        {
            byte[] prePropBinary;
            if (ex.HasStack)
            {
                if (mePackage.Game < MEGame.ME3)
                {
                    prePropBinary = new byte[]
                    {
                        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00
                    };
                }
                else
                {
                    prePropBinary = new byte[]
                    {
                        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00
                    };
                }
            }
            else
            {
                int start = ex.GetPropertyStart();
                prePropBinary = new byte[start];
            }

            PropertyCollection props = ex.GetProperties();
            //store copy of names list in case something goes wrong
            List<string> names = mePackage.Names.ToList();
            try
            {
                if (ex.Game != mePackage.Game)
                {
                    props = EntryPruner.RemoveIncompatibleProperties(ex.FileRef, props, ex.ClassName, mePackage.Game);
                }
            }
            catch (Exception exception)
            {
                //restore namelist in event of failure.
                mePackage.setNames(names);
                Console.WriteLine($"Error occured while trying to import {ex.ObjectName} : {exception.Message}");
                //MessageBox.Show($"Error occured while trying to import {ex.ObjectName} : {exception.Message}");
                outputEntry = null;
                return false;
            }

            //takes care of slight header differences between ME1/2 and ME3
            byte[] newHeader = ex.GenerateHeader(mePackage.Game);

            //for supported classes, this will add any names in binary to the Name table, as well as take care of binary differences for cross-game importing
            //for unsupported classes, this will just copy over the binary
            byte[] binaryData = ExportBinaryConverter.ConvertPostPropBinary(ex, mePackage.Game).ToArray(mePackage);

            int classValue = 0;
            int archetype = 0;
            int superclass = 0;
            //Set class. This will only work if the class is an import, as we can't reliably pull in exports without lots of other stuff.
            if (ex.idxClass < 0)
            {
                //The class of the export we are importing is an import. We should attempt to relink this.
                ImportEntry portingFromClassImport = ex.FileRef.getImport(ex.idxClass);
                IEntry newClassImport = getOrAddCrossImportOrPackage(portingFromClassImport.GetFullPath, ex.FileRef, mePackage);
                classValue = newClassImport.UIndex;
            }
            else if (ex.idxClass > 0)
            {
                //Todo: Add cross mapping support as multi-mode will allow this to work now
                ExportEntry portingInClass = ex.FileRef.getUExport(ex.idxClass);
                ExportEntry matchingExport = mePackage.Exports.FirstOrDefault(x => x.GetIndexedFullPath == portingInClass.GetIndexedFullPath);
                if (matchingExport != null)
                {
                    classValue = matchingExport.UIndex;
                }
            }

            //Set superclass
            if (ex.idxSuperClass < 0)
            {
                //The class of the export we are importing is an import. We should attempt to relink this.
                ImportEntry portingFromClassImport = ex.FileRef.getImport(ex.idxSuperClass);
                IEntry newClassImport = getOrAddCrossImportOrPackage(portingFromClassImport.GetFullPath, ex.FileRef, mePackage);
                superclass = newClassImport.UIndex;
            }
            else if (ex.idxSuperClass > 0)
            {
                //Todo: Add cross mapping support as multi-mode will allow this to work now
                ExportEntry portingInClass = ex.FileRef.getUExport(ex.idxSuperClass);
                ExportEntry matchingExport = mePackage.Exports.FirstOrDefault(x => x.GetIndexedFullPath == portingInClass.GetIndexedFullPath);
                if (matchingExport != null)
                {
                    superclass = matchingExport.UIndex;
                }
            }

            //Check archetype.
            if (ex.idxArchtype < 0)
            {
                ImportEntry portingFromClassImport = ex.FileRef.getImport(ex.idxArchtype);
                IEntry newClassImport = getOrAddCrossImportOrPackage(portingFromClassImport.GetFullPath, ex.FileRef, mePackage);
                archetype = newClassImport.UIndex;
            }
            else if (ex.idxArchtype > 0)
            {
                ExportEntry portingInClass = ex.FileRef.getUExport(ex.idxArchtype);
                ExportEntry matchingExport = mePackage.Exports.FirstOrDefault(x => x.GetIndexedFullPath == portingInClass.GetIndexedFullPath);
                if (matchingExport != null)
                {
                    archetype = matchingExport.UIndex;
                }
            }

            outputEntry = new ExportEntry(mePackage, prePropBinary, props, binaryData)
            {
                Header = newHeader,
                idxClass = classValue,
                idxObjectName = mePackage.FindNameOrAdd(ex.FileRef.getNameEntry(ex.idxObjectName)),
                idxLink = link,
                idxSuperClass = superclass,
                idxArchtype = archetype
            };
            mePackage.addExport(outputEntry);

            return true;
        }
    }
}
