﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Gammtek.Conduit.Extensions.Collections.Generic;
using ME3Explorer.Packages;
using ME3Explorer.Unreal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;
using StreamHelpers;

namespace ME3Explorer.Pathfinding_Editor
{
    class SharedPathfinding
    {
        //Defaults to empty to prevent issues
        public static Dictionary<string, Dictionary<string, string>> ImportClassDB = new Dictionary<string, Dictionary<string, string>>(); //SFXGame.Default__SFXEnemySpawnPoint -> class, packagefile (can infer link and name)
        public static List<PathfindingDB_ExportType> ExportClassDB = new List<PathfindingDB_ExportType>(); //SFXEnemy SpawnPoint -> class, name, ...etc
        private static bool ClassesDBLoaded;
        //internal static string ClassesDatabasePath = Path.Combine(App.ExecFolder, "pathfindingclassdb.json");

        /// <summary>
        /// Converts struct property to SharpDX Vector 3
        /// </summary>
        /// <param name="vectorStruct">Vector Struct to convert</param>
        /// <returns></returns>
        public static Vector3 GetVector3(StructProperty vectorStruct)
        {
            Vector3 v = new Vector3
            {
                X = vectorStruct.GetProp<FloatProperty>("X"),
                Y = vectorStruct.GetProp<FloatProperty>("Y"),
                Z = vectorStruct.GetProp<FloatProperty>("Z")
            };
            return v;
        }

        /// <summary>
        /// Converts struct property to SharpDX Vector 2
        /// </summary>
        /// <param name="vectorStruct">Vector Struct to convert</param>
        /// <returns></returns>
        public static Vector2 GetVector2(StructProperty vectorStruct)
        {
            Vector2 v = new Vector2
            {
                X = vectorStruct.GetProp<FloatProperty>("X"),
                Y = vectorStruct.GetProp<FloatProperty>("Y")
            };
            return v;
        }

        public static void GenerateNewRandomGUID(ExportEntry export)
        {
            StructProperty guidProp = export.GetProperty<StructProperty>("NavGuid");
            if (guidProp != null)
            {
                Random rnd = new Random();
                IntProperty A = guidProp.GetProp<IntProperty>("A");
                IntProperty B = guidProp.GetProp<IntProperty>("B");
                IntProperty C = guidProp.GetProp<IntProperty>("C");
                IntProperty D = guidProp.GetProp<IntProperty>("D");
                byte[] data = export.Data;

                data.OverwriteRange((int)A.ValueOffset, BitConverter.GetBytes(rnd.Next()));
                data.OverwriteRange((int)B.ValueOffset, BitConverter.GetBytes(rnd.Next()));
                data.OverwriteRange((int)C.ValueOffset, BitConverter.GetBytes(rnd.Next()));
                data.OverwriteRange((int)D.ValueOffset, BitConverter.GetBytes(rnd.Next()));
                export.Data = data;
            }
        }

        public static void CreateReachSpec(ExportEntry startNode, bool createTwoWay, ExportEntry destinationNode, string reachSpecClass, ReachSpecSize size, PropertyCollection externalGUIDProperties = null)
        {
            IMEPackage Pcc = startNode.FileRef;
            ExportEntry reachSpectoClone = Pcc.Exports.FirstOrDefault(x => x.ClassName == "ReachSpec");

            if (externalGUIDProperties != null) //EXTERNAL
            {
                //external node

                //Debug.WriteLine("Num Exports: " + pcc.Exports.Count);
                if (reachSpectoClone != null)
                {
                    ExportEntry outgoingSpec = reachSpectoClone.Clone();
                    Pcc.addExport(outgoingSpec);

                    IEntry reachSpecClassImp = GetEntryOrAddImport(Pcc, reachSpecClass); //new class type.

                    outgoingSpec.idxClass = reachSpecClassImp.UIndex;
                    outgoingSpec.idxObjectName = reachSpecClassImp.idxObjectName;

                    var properties = outgoingSpec.GetProperties();
                    ObjectProperty outgoingSpecStartProp = properties.GetProp<ObjectProperty>("Start"); //START
                    StructProperty outgoingEndStructProp = properties.GetProp<StructProperty>("End"); //Embeds END
                    ObjectProperty outgoingSpecEndProp = outgoingEndStructProp.Properties.GetProp<ObjectProperty>(SharedPathfinding.GetReachSpecEndName(outgoingSpec)); //END
                    outgoingSpecStartProp.Value = startNode.UIndex;
                    outgoingSpecEndProp.Value = 0;
                    var endGuid = outgoingEndStructProp.GetProp<StructProperty>("Guid");
                    endGuid.Properties = externalGUIDProperties; //set the other guid values to our guid values

                    //Add to source node prop
                    ArrayProperty<ObjectProperty> PathList = startNode.GetProperty<ArrayProperty<ObjectProperty>>("PathList");
                    PathList.Add(new ObjectProperty(outgoingSpec.UIndex));
                    startNode.WriteProperty(PathList);
                    outgoingSpec.WriteProperties(properties);


                    //Write Spec Size
                    SharedPathfinding.SetReachSpecSize(outgoingSpec, size.SpecRadius, size.SpecHeight);

                    //Reindex reachspecs.
                    SharedPathfinding.ReindexMatchingObjects(outgoingSpec);

                }
            }
            else
            {
                //Debug.WriteLine("Source Node: " + startNode.Index);

                //Debug.WriteLine("Num Exports: " + pcc.Exports.Count);
                //int outgoingSpec = pcc.ExportCount;
                //int incomingSpec = pcc.ExportCount + 1;


                if (reachSpectoClone != null)
                {
                    ExportEntry outgoingSpec = reachSpectoClone.Clone();
                    Pcc.addExport(outgoingSpec);
                    ExportEntry incomingSpec = null;
                    if (createTwoWay)
                    {
                        incomingSpec = reachSpectoClone.Clone();
                        Pcc.addExport(incomingSpec);
                    }

                    IEntry reachSpecClassImp = GetEntryOrAddImport(Pcc, reachSpecClass); //new class type.

                    outgoingSpec.idxClass = reachSpecClassImp.UIndex;
                    outgoingSpec.idxObjectName = reachSpecClassImp.idxObjectName;

                    var outgoingSpecProperties = outgoingSpec.GetProperties();
                    if (reachSpecClass == "Engine.SlotToSlotReachSpec")
                    {
                        outgoingSpecProperties.Add(new ByteProperty(1, "SpecDirection")); //We might need to find a way to support this edit
                    }

                    //Debug.WriteLine("Outgoing UIndex: " + outgoingSpecExp.UIndex);

                    ObjectProperty outgoingSpecStartProp = outgoingSpecProperties.GetProp<ObjectProperty>("Start"); //START
                    StructProperty outgoingEndStructProp = outgoingSpecProperties.GetProp<StructProperty>("End"); //Embeds END
                    ObjectProperty outgoingSpecEndProp = outgoingEndStructProp.Properties.GetProp<ObjectProperty>(SharedPathfinding.GetReachSpecEndName(outgoingSpec)); //END
                    outgoingSpecStartProp.Value = startNode.UIndex;
                    outgoingSpecEndProp.Value = destinationNode.UIndex;

                    //Add to source node prop
                    var PathList = startNode.GetProperty<ArrayProperty<ObjectProperty>>("PathList");
                    PathList.Add(new ObjectProperty(outgoingSpec.UIndex));
                    startNode.WriteProperty(PathList);

                    //Write Spec Size
                    SetReachSpecSize(outgoingSpecProperties, size.SpecRadius, size.SpecHeight);
                    outgoingSpec.WriteProperties(outgoingSpecProperties);

                    if (createTwoWay)
                    {
                        incomingSpec.idxClass = reachSpecClassImp.UIndex;
                        incomingSpec.idxObjectName = reachSpecClassImp.idxObjectName;
                        var incomingSpecProperties = incomingSpec.GetProperties();
                        if (reachSpecClass == "Engine.SlotToSlotReachSpec")
                        {
                            incomingSpecProperties.Add(new ByteProperty(2, "SpecDirection"));
                        }

                        ObjectProperty incomingSpecStartProp = incomingSpecProperties.GetProp<ObjectProperty>("Start"); //START
                        StructProperty incomingEndStructProp = incomingSpecProperties.GetProp<StructProperty>("End"); //Embeds END
                        ObjectProperty incomingSpecEndProp = incomingEndStructProp.Properties.GetProp<ObjectProperty>(SharedPathfinding.GetReachSpecEndName(incomingSpec)); //END

                        incomingSpecStartProp.Value = destinationNode.UIndex; //Uindex
                        incomingSpecEndProp.Value = startNode.UIndex;


                        //Add reachspec to destination node's path list (returning)
                        var DestPathList = destinationNode.GetProperty<ArrayProperty<ObjectProperty>>("PathList");
                        DestPathList.Add(new ObjectProperty(incomingSpec.UIndex));
                        destinationNode.WriteProperty(DestPathList);

                        //destNode.WriteProperty(DestPathList);
                        SetReachSpecSize(incomingSpecProperties, size.SpecRadius, size.SpecHeight);

                        incomingSpec.WriteProperties(incomingSpecProperties);
                    }

                    //Reindex reachspecs.
                    SharedPathfinding.ReindexMatchingObjects(outgoingSpec);
                }
            }
        }

        /// <summary>
        /// Modifies the incoming properties collection to update the spec size
        /// </summary>
        /// <param name="specProperties"></param>
        /// <param name="radius"></param>
        /// <param name="height"></param>
        public static void SetReachSpecSize(PropertyCollection specProperties, int radius, int height)
        {
            IntProperty radiusProp = specProperties.GetProp<IntProperty>("CollisionRadius");
            IntProperty heightProp = specProperties.GetProp<IntProperty>("CollisionHeight");
            if (radiusProp != null)
            {
                radiusProp.Value = radius;
            }
            if (heightProp != null)
            {
                heightProp.Value = height;
            }
        }

        /// <summary>
        /// Sets the reach spec size and commits the results back to the export
        /// </summary>
        /// <param name="spec"></param>
        /// <param name="radius"></param>
        /// <param name="height"></param>
        public static void SetReachSpecSize(ExportEntry spec, int radius, int height)
        {
            PropertyCollection specProperties = spec.GetProperties();
            SetReachSpecSize(specProperties, radius, height);
            spec.WriteProperties(specProperties); //write it back.
        }

        public static IEntry GetEntryOrAddImport(IMEPackage Pcc, string importFullName)
        {
            //foreach (ImportEntry imp in Pcc.Imports)
            //{
            //    if (imp.GetFullPath == importFullName)
            //    {
            //        return imp;
            //    }
            //}

            var fullPathMappingList = new List<(string fullpath, IEntry entry)>();
            foreach (ImportEntry imp in Pcc.Imports)
            {
                fullPathMappingList.Add((imp.GetFullPath, imp));
            }
            foreach (ExportEntry exp in Pcc.Exports)
            {
                fullPathMappingList.Add((exp.GetFullPath, exp));
            }

            var directMapping = fullPathMappingList.Where(x => x.fullpath == importFullName).ToList();
            if (directMapping.Count == 1) return directMapping[0].entry; //direct single match

            //Find an upstream entry to attach our import to (we can't add exports)
            string[] importParts = importFullName.Split('.');
            int upstreamCount = 1;

            IEntry upstreamEntryToAttachTo = null;
            string upstreamfullpath;
            while (upstreamCount < importParts.Length)
            {
                upstreamfullpath = string.Join(".", importParts, 0, importParts.Length - upstreamCount);
                var upstreammatchinglist = fullPathMappingList.Where(x => x.fullpath == upstreamfullpath).ToList();
                if (upstreammatchinglist.Where(x => x.entry is ExportEntry).HasExactly(1) || upstreammatchinglist.Where(x => x.entry is ImportEntry).HasExactly(1))
                {
                    upstreamEntryToAttachTo = upstreammatchinglist[0].entry;
                    break;
                }
                /*if (upstreamEntryToAttachTo != null)
                {
                    break;
                }*/
                upstreamCount++;
            }

            //upstreamImport = Pcc.Imports.FirstOrDefault(x => x.GetFullPath == upstream);



            //Check if this is an export instead
            /* itemAsImport = Pcc.Exports.FirstOrDefault(x => x.GetFullPath == importFullName && x.indexValue == 0);
            if (itemAsImport != null)
            {
                return itemAsImport;
            }*/

            //Import doesn't exist, so we're gonna need to add it
            //But first we need to figure out what needs to be added.
            //string[] importParts = importFullName.Split('.');
            //List<int> upstreamLinks = new List<int>(); //0 = top level, 1 = next level... n = what we wanted to import

            /*ImportEntry upstreamImport = null;
            string upstream = null;
            while (upstreamCount < importParts.Count())
            {
                upstreamfullpath = string.Join(".", importParts, 0, importParts.Count() - upstreamCount);
                upstreamImport = Pcc.Imports.FirstOrDefault(x => x.GetFullPath == upstreamfullpath);

                if (upstreamImport != null)
                {
                    break;
                }
                upstreamCount++;
            }*/

            if (upstreamEntryToAttachTo == null)
            {
                //There is nothing we can attach to.
                Debug.WriteLine("cannot find a top level item to attach to for " + importFullName);
                return null;
            }

            //Have an upstream import, now we need to add downstream imports.
            ImportEntry mostdownstreamimport = null;

            while (upstreamCount > 0)
            {
                upstreamCount--;
                string fullobjectname = String.Join(".", importParts, 0, importParts.Length - upstreamCount);
                Dictionary<string, string> importdbinfo = ImportClassDB[fullobjectname];

                int downstreamName = Pcc.FindNameOrAdd(importParts[importParts.Length - upstreamCount - 1]);
                Debug.WriteLine(Pcc.Names[downstreamName]);
                int downstreamLinkIdx = upstreamEntryToAttachTo.UIndex;
                Debug.WriteLine(upstreamEntryToAttachTo.GetFullPath);

                int downstreamPackageName = Pcc.FindNameOrAdd(importdbinfo["packagefile"]);
                string downstreamClassName = importdbinfo["fullclasspath"];
                int lastPeriodIndex = downstreamClassName.LastIndexOf(".");
                if (lastPeriodIndex > 0)
                {
                    downstreamClassName = importdbinfo["fullclasspath"].Substring(lastPeriodIndex + 1);

                }

                int downstreamClassNameIdx = Pcc.FindNameOrAdd(downstreamClassName);
                Debug.WriteLine("Finding name " + downstreamClassName);
                //ImportEntry classImport = getOrAddImport();
                //int downstreamClass = 0;
                //if (classImport != null) {
                //    downstreamClass = classImport.UIndex; //no recursion pls
                //} else
                //{
                //    throw new Exception("No class was found for importing");
                //}

                mostdownstreamimport = new ImportEntry(Pcc)
                {
                    idxLink = downstreamLinkIdx,
                    idxClassName = downstreamClassNameIdx,
                    idxObjectName = downstreamName,
                    idxPackageFile = downstreamPackageName
                };
                Pcc.addImport(mostdownstreamimport);
                upstreamEntryToAttachTo = mostdownstreamimport;
            }
            return mostdownstreamimport;
        }

        /// <summary>
        /// Writes the buffer to the memory array starting at position pos
        /// </summary>
        /// <param name="memory">Memory array to overwrite onto</param>
        /// <param name="pos">Position to start writing at</param>
        /// <param name="buff">byte array to write, in order</param>
        /// <returns>Modified memory</returns>
        public static byte[] WriteMem(byte[] memory, int pos, byte[] buff)
        {
            for (int i = 0; i < buff.Length; i++)
                memory[pos + i] = buff[i];

            return memory;
        }

        /// <summary>
        /// Gets the end name of a ReachSpec for property parsing. ME1 uses Nav, while ME2 and above use Actor.
        /// </summary>
        /// <param name="export">export used to determine which game is being parsed</param>
        /// <returns>Actor for ME2/ME3, Nav for ME1</returns>
        public static string GetReachSpecEndName(ExportEntry export) => export.FileRef.Game == MEGame.ME1 ? "Nav" : "Actor";

        /// <summary>
        /// Rounds a double to an int. Because apparently Microsoft doesn't know how to round numbers.
        /// </summary>
        /// <param name="d">Double to round</param>
        /// <returns>Rounded int</returns>
        public static int RoundDoubleToInt(double d)
        {
            if (d < 0)
            {
                return (int)(d - 0.5);
            }
            return (int)(d + 0.5);
        }

        /// <summary>
        /// Fetches the NavGuid object as a UnrealGUID
        /// </summary>
        /// <param name="export"></param>
        /// <returns></returns>
        public static UnrealGUID GetNavGUID(ExportEntry export)
        {
            StructProperty navGuid = export.GetProperty<StructProperty>("NavGuid");
            if (navGuid != null)
            {
                return new UnrealGUID(navGuid)
                {
                    export = export
                };
            }

            return null;
        }


        /// <summary>
        /// Reindexes all objects in this pcc that have the same full path.
        /// USE WITH CAUTION!
        /// </summary>
        /// <param name="exportToReindex">Export that contains the path you want to reindex</param>
        public static void ReindexMatchingObjects(ExportEntry exportToReindex)
        {
            string fullpath = exportToReindex.GetFullPath;
            int index = 1; //we'll start at 1.
            foreach (ExportEntry export in exportToReindex.FileRef.Exports)
            {
                if (fullpath == export.GetFullPath && export.ClassName != "Class")
                {
                    export.indexValue = index;
                    index++;
                }
            }
        }

        //internal static void LoadClassesDB()
        //{
        //    if (!ClassesDBLoaded)
        //    {
        //        if (File.Exists(ClassesDatabasePath))
        //        {
        //            string raw = File.ReadAllText(ClassesDatabasePath);
        //            JObject o = JObject.Parse(raw);
        //            JToken exportjson = o.SelectToken("exporttypes");
        //            JToken importjson = o.SelectToken("importtypes");

        //            ExportClassDB = JsonConvert.DeserializeObject<List<PathfindingDB_ExportType>>(exportjson.ToString());
        //            ImportClassDB = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(importjson.ToString());
        //            ClassesDBLoaded = true;
        //        }
        //    }
        //}

        /// <summary>
        /// Gets a list of the reachspec exports listed in the PathList array property
        /// </summary>
        /// <param name="export">export to read PathList from</param>
        /// <returns></returns>
        internal static List<ExportEntry> GetReachspecExports(ExportEntry export)
        {
            var pathlist = export.GetProperty<ArrayProperty<ObjectProperty>>("PathList");
            if (pathlist == null) return new List<ExportEntry>(); //nothing
            var returnlist = new List<ExportEntry>(pathlist.Count);
            foreach (ObjectProperty prop in pathlist)
            {
                if (prop.Value > 0)
                {
                    returnlist.Add(export.FileRef.getUExport(prop.Value));
                }
            }

            return returnlist;
        }

        internal static ExportEntry GetReachSpecEndExport(ExportEntry reachSpec, PropertyCollection props = null)
        {
            if (props == null)
            {
                props = reachSpec.GetProperties();
            }

            if (props.GetProp<StructProperty>("End") is StructProperty endProperty &&
                endProperty.GetProp<ObjectProperty>(SharedPathfinding.GetReachSpecEndName(reachSpec)) is ObjectProperty otherNodeValue
                && reachSpec.FileRef.isUExport(otherNodeValue.Value))
            {
                return reachSpec.FileRef.getUExport(otherNodeValue.Value);
            }

            return null; //can't get end, or is external
        }

        public static Point3D GetLocation(ExportEntry export)
        {
            float x = 0, y = 0, z = int.MinValue;
            var prop = export.GetProperty<StructProperty>("location");
            if (prop != null)
            {
                foreach (var locprop in prop.Properties)
                {
                    switch (locprop)
                    {
                        case FloatProperty fltProp when fltProp.Name == "X":
                            x = fltProp;
                            break;
                        case FloatProperty fltProp when fltProp.Name == "Y":
                            y = fltProp;
                            break;
                        case FloatProperty fltProp when fltProp.Name == "Z":
                            z = fltProp;
                            break;
                    }
                }
                return new Point3D(x, y, z);
            }
            return null;
        }

        public static void SetLocation(ExportEntry export, float x, float y, float z)
        {
            StructProperty prop = export.GetProperty<StructProperty>("location");
            SetLocation(prop, x, y, z);
            export.WriteProperty(prop);
        }

        public static void SetLocation(StructProperty prop, float x, float y, float z)
        {
            prop.GetProp<FloatProperty>("X").Value = x;
            prop.GetProp<FloatProperty>("Y").Value = y;
            prop.GetProp<FloatProperty>("Z").Value = z;
        }
    }

    public class UnrealGUID
    {
        public readonly int A, B, C, D;
        public ExportEntry export;

        public UnrealGUID(StructProperty guid)
        {
            if (guid.StructType != "Guid")
            {
                throw new Exception("Can't parse non-guid struct with UnrealGUID");
            }

            A = guid.GetProp<IntProperty>("A");
            B = guid.GetProp<IntProperty>("B");
            C = guid.GetProp<IntProperty>("C");
            D = guid.GetProp<IntProperty>("D");
        }

        public static bool operator ==(UnrealGUID b1, UnrealGUID b2)
        {
            if (b1 is null)
                return b2 is null;

            return b1.Equals(b2);
        }

        public static bool operator !=(UnrealGUID b1, UnrealGUID b2) => !(b1 == b2);

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            UnrealGUID other = (UnrealGUID)obj;
            return other.A == A && other.B == B && other.C == C && other.D == D;
        }

        public override int GetHashCode() => (A, B, C, D).GetHashCode();

        public override string ToString()
        {
            MemoryStream ms = new MemoryStream();
            ms.WriteInt32(A);
            ms.WriteInt32(B);
            ms.WriteInt32(C);
            ms.WriteInt32(D);
            return new Guid(ms.ToArray()).ToString();
        }
    }

    [DebuggerDisplay("ReachSpecSize | {Header} {SpecHeight}x{SpecRadius}")]
    public class ReachSpecSize : NotifyPropertyChangedBase, IEquatable<ReachSpecSize>
    {
        public const int MOOK_RADIUS = 34;
        public const int MOOK_HEIGHT = 90;
        public const int MINIBOSS_RADIUS = 105;
        public const int MINIBOSS_HEIGHT = 145;
        public const int BOSS_RADIUS = 140;
        public const int BOSS_HEIGHT = 195;
        public const int BANSHEE_RADIUS = 50;
        public const int BANSHEE_HEIGHT = 125;
        public const int HARVESTER_RADIUS = 500;
        public const int HARVESTER_HEIGHT = 500;

        public bool CustomSized;

        public ReachSpecSize()
        {

        }

        public ReachSpecSize(string header, int height, int radius, bool customsized = false)
        {
            Header = header;
            SpecHeight = height;
            SpecRadius = radius;
            CustomSized = customsized;
        }

        private string _header;
        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        private int _specRadius;
        public int SpecRadius
        {
            get => _specRadius;
            set => SetProperty(ref _specRadius, value);
        }

        private int _specHeight;
        public int SpecHeight
        {
            get => _specHeight;
            set => SetProperty(ref _specHeight, value);
        }

        public bool Equals(ReachSpecSize other)
        {
            if (other is null)
            {
                return false;
            }
            return SpecRadius == other.SpecRadius && SpecHeight == other.SpecHeight;
        }
    }

    public class PathfindingDB_ExportType_EnsuredProperty
    {
        public string name { get; set; }
        public string type { get; set; }
        public string defaultvalue { get; set; }
    }

    public class PathfindingDB_ExportType
    {
        public string nodetypename { get; set; }
        public string fullclasspath { get; set; }
        public string name { get; set; }
        public string cylindercomponentarchetype { get; set; }
        public bool pathnode { get; set; }
        public string description { get; set; }
        public bool usesbtop { get; set; }
        public bool upgradetomaxpathsize { get; set; }
        public List<PathfindingDB_ExportType_EnsuredProperty> ensuredproperties { get; set; } = new List<PathfindingDB_ExportType_EnsuredProperty>();
        public string inboundspectype { get; set; }
    }


    public class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3D()
        {

        }

        public Point3D(double X, double Y, double Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public double getDistanceToOtherPoint(Point3D other)
        {
            double deltaX = X - other.X;
            double deltaY = Y - other.Y;
            double deltaZ = Z - other.Z;

            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }

        public override string ToString()
        {
            return $"{X},{Y},{Z}";
        }
    }
}
