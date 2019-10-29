﻿using System;
using System.Collections.Generic;
using System.Linq;
using ME3Explorer.Unreal;
using ME3Explorer.Unreal.BinaryConverters;

namespace ME3Explorer.Packages
{
    public static class MEPackageExtensions
    {

        public static string GetEntryString(this IMEPackage pcc, int index)
        {
            if (index == 0)
            {
                return "Null";
            }
            string retStr = "Entry not found";
            IEntry coreRefEntry = pcc.getEntry(index);
            if (coreRefEntry != null)
            {
                retStr = coreRefEntry is ImportEntry ? "[I] " : "[E] ";
                retStr += coreRefEntry.GetIndexedFullPath;
            }
            return retStr;
        }

        public static string FollowLink(this IMEPackage pcc, int uIndex)
        {
            if (pcc.isUExport(uIndex))
            {
                ExportEntry parent = pcc.getUExport(uIndex);
                return pcc.FollowLink(parent.idxLink) + parent.ObjectName + ".";
            }
            if (pcc.isImport(uIndex))
            {
                ImportEntry parent = pcc.getImport(uIndex);
                return pcc.FollowLink(parent.idxLink) + parent.ObjectName + ".";
            }
            return "";
        }

        //if neccessary, will fill in parents as Package Imports (if the import you need has non-Package parents, don't use this method)
        public static IEntry getEntryOrAddImport(this IMEPackage pcc, string fullPath, string className = "Class", string packageFile = "Core")
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return null;
            }

            //see if this import exists locally
            foreach (ImportEntry imp in pcc.Imports)
            {
                if (imp.GetFullPath == fullPath)
                {
                    return imp;
                }
            }

            //see if this is an export and exists locally
            foreach (ExportEntry exp in pcc.Exports)
            {
                if (exp.GetFullPath == fullPath)
                {
                    return exp;
                }
            }

            string[] pathParts = fullPath.Split('.');

            IEntry parent = pcc.getEntryOrAddImport(string.Join(".", pathParts.Take(pathParts.Length - 1)), "Package");

            var import = new ImportEntry(pcc)
            {
                idxLink = parent?.UIndex ?? 0,
                idxClassName = pcc.FindNameOrAdd(className),
                idxObjectName = pcc.FindNameOrAdd(pathParts.Last()),
                idxPackageFile = pcc.FindNameOrAdd(packageFile)
            };
            pcc.addImport(import);
            return import;
        }

    }
    public static class ExportEntryExtensions
    {

        public static T GetProperty<T>(this ExportEntry export, string name) where T : UProperty
        {
            return export.GetProperties().GetProp<T>(name);
        }

        public static void WriteProperty(this ExportEntry export, UProperty prop)
        {
            var props = export.GetProperties();
            props.AddOrReplaceProp(prop);
            export.WriteProperties(props);
        }

        public static bool RemoveProperty(this ExportEntry export, string propname)
        {
            var props = export.GetProperties();
            UProperty propToRemove = null;
            foreach (UProperty prop in props)
            {
                if (prop.Name.Name == propname)
                {
                    propToRemove = prop;
                    break;
                }
            }

            //outside for concurrent collection modification
            if (propToRemove != null)
            {
                props.Remove(propToRemove);
                export.WriteProperties(props);
                return true;
            }

            return false;
        }
    }

    public static class IEntryExtensions
    {
        public static bool IsTrash(this IEntry entry)
        {
            return entry.ObjectName == UnrealPackageFile.TrashPackageName || entry.Parent?.ObjectName == UnrealPackageFile.TrashPackageName;
        }

        public static bool IsTexture(this IEntry entry)
        {
            return entry.ClassName == "Texture2D" ||
                   entry.ClassName == "LightMapTexture2D" ||
                   entry.ClassName == "ShadowMapTexture2D" ||
                   entry.ClassName == "TerrainWeightMapTexture" ||
                   entry.ClassName == "TextureFlipBook";
        }

        public static bool IsDescendantOf(this IEntry entry, IEntry ancestor)
        {
            while (entry.HasParent)
            {
                entry = entry.Parent;
                if (entry == ancestor)
                {
                    return true;
                }
            }
            return false;
        }

        public static List<IEntry> GetChildren(this IEntry entry)
        {
            var kids = new List<IEntry>();
            kids.AddRange(entry.FileRef.Exports.Where(export => export.idxLink == entry.UIndex));
            kids.AddRange(entry.FileRef.Imports.Where(import => import.idxLink == entry.UIndex));
            return kids;
        }

        public static List<IEntry> GetAllDescendants(this IEntry entry)
        {
            var kids = new List<IEntry>();
            kids.AddRange(entry.FileRef.Exports.Where(export => export.IsDescendantOf(entry)));
            kids.AddRange(entry.FileRef.Imports.Where(import => import.IsDescendantOf(entry)));
            return kids;
        }

        public static Dictionary<IEntry, List<string>> GetEntriesThatReferenceThisOne(this IEntry baseEntry)
        {
            var result = new Dictionary<IEntry, List<string>>();
            int baseUIndex = baseEntry.UIndex;
            foreach (ExportEntry exp in baseEntry.FileRef.Exports)
            {
                if (exp == baseEntry)
                {
                    continue;
                }
                //find header references
                if (exp.idxArchtype == baseUIndex)
                {
                    result.AddToListAt(exp, "Header: Archetype");
                }
                if (exp.idxClass == baseUIndex)
                {
                    result.AddToListAt(exp, "Header: Class");
                }
                if (exp.idxSuperClass == baseUIndex)
                {
                    result.AddToListAt(exp, "Header: SuperClass");
                }
                if (exp.HasComponentMap && exp.ComponentMap.Any(kvp => kvp.Value == baseUIndex))
                {
                    result.AddToListAt(exp, "Header: ComponentMap");
                }

                //find stack references
                if (exp.HasStack && exp.Data is byte[] data
                 && (baseUIndex == BitConverter.ToInt32(data, 0) || baseUIndex == BitConverter.ToInt32(data, 4)))
                {
                    result.AddToListAt(exp, "Stack");
                }


                //find property references
                findPropertyReferences(exp.GetProperties(), exp, "Property:");

                //find binary references
                if (ObjectBinary.From(exp) is ObjectBinary objBin)
                {
                    List<(UIndex, string)> indices = objBin.GetUIndexes(exp.FileRef.Game);
                    foreach ((UIndex uIndex, string propName) in indices)
                    {
                        if (uIndex == baseUIndex)
                        {
                            result.AddToListAt(exp, $"(Binary prop: {propName})");
                        }
                    }
                }
            }

            return result;

            void findPropertyReferences(PropertyCollection props, ExportEntry exp, string prefix = "")
            {
                foreach (UProperty prop in props)
                {
                    switch (prop)
                    {
                        case ObjectProperty objectProperty:
                            if (objectProperty.Value == baseUIndex)
                            {
                                result.AddToListAt(exp, $"{prefix} {objectProperty.Name}");
                            }
                            break;
                        case DelegateProperty delegateProperty:
                            if (delegateProperty.Value.Object == baseUIndex)
                            {
                                result.AddToListAt(exp, $"{prefix} {delegateProperty.Name}");
                            }
                            break;
                        case StructProperty structProperty:
                            findPropertyReferences(structProperty.Properties, exp, $"{prefix} {structProperty.Name}:");
                            break;
                        case ArrayProperty<ObjectProperty> arrayProperty:
                            for (int i = 0; i < arrayProperty.Count; i++)
                            {
                                ObjectProperty objProp = arrayProperty[i];
                                if (objProp.Value == baseUIndex)
                                {
                                    result.AddToListAt(exp, $"{prefix} {arrayProperty.Name}[{i}]");
                                }
                            }
                            break;
                        case ArrayProperty<StructProperty> arrayProperty:
                            for (int i = 0; i < arrayProperty.Count; i++)
                            {
                                StructProperty structProp = arrayProperty[i];
                                findPropertyReferences(structProp.Properties, exp, $"{prefix} {arrayProperty.Name}[{i}]:");
                            }
                            break;
                    }
                }
            }
        }
    }
}
