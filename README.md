# ME3ExplorerLib
Library version of most of the backend for ME3Explorer for use as a dynamic link library

Current version is based on ME3Explorer 4.0 codebase. While not the latest, it has most features that that toolset does (most of the toolset work is done in te front end now).

Most calls are done through same way as ME3Explorer code.

Before using DLL, ensure you call 
```
ME3ExplorerMinified.DLL.Startup();
```
to force the package information databases to load.

## Packages

To open a package file, use MEPackageHandler. Do this at least once from a single-thread to ensure you don't have race condition initializing package function delegate. This will be fixed in a future version of the library.

```
var package = MEPackageHandler.OpenMEPackage("YourFile.pcc");
```

Get exports from package file: 
```
//Using 0 of negative values will throw an exception
package.getUExport(33);
```

Get imports from package file:
```
//Using 0 or positive numbers will throw an exception
package.getUImport(-43);
```

Get an entry (export or import):
```
//Using 0 will throw an exception.
package.getEntry(15);
```

### Export Properites
When fetching properties, setting the value on the returned property and then writing the collection back will work as it writes back the referenced items.

Get all properties for an export and write them back:
```
var export = package.getUExport(15);
var properties = export.GetProperties(); //gets a PropertyCollection object.
var prop1 = properties.GetProp<FloatProperty>("MaxSpeedX");
prop1.Value = 44.4f;
export.WriteProperties(properties);
```

Get one property and write it back:
```
var export = package.getUExport(15);
var prop1 = export.GetProperty<FloatProperty>("MaxSpeedX"); //gets a FloatProperty object. If MaxSpeedX doesn't exist, or it is not a float property, this returns null. A common area for mistakes!
prop1.Value = 44.4f;
export.WriteProperties(properties);
```

Get one property and write it back:
```
var export = package.getUExport(15);
var prop1 = export.GetProperty<FloatProperty>("MaxSpeedX"); //gets a FloatProperty object. If MaxSpeedX doesn't exist, or it is not a float property, this returns null. A common area for mistakes!
prop1.Value = 44.4f;
export.WriteProperties(properties);
```

Get one property and write it back (short version with null check):
```
var export = package.getUExport(15);
var prop1 = export.GetProperty<FloatProperty>("MaxSpeedX")?.Value = 44.4f; //This is an if not null check. Ensure this will work for you.
export.WriteProperty(prop1);
```

## TLK
TLK changes based on ME1 or ME2.

### ME1
Use ME1Explorer.Unreal.Classes.TalkFile to load a TalkFile. Use ME1Explorer.HuffmanCompression to reserialize back to an export.

Load TLK
```
using static ME1Explorer.Unreal.Classes.TalkFile;

<export variable is variable of TLK export>
var tf = new TalkFile(export);
//Access string refs through 

```

Modify strings
```
using System.linq;
<tf variable is existing TalkFile object>
var strref = tf.StringRefs.FirstOrDefault(x=>x.StringID == 301204);
if (strref != null) {
    strref.Data = "My new string";
}
```

Save back to export
```
<export variable is variable of TLK export>
<tf variable is existing TalkFile object>
ME1Explorer.HuffmanCompression hc = new ME1Explorer.HuffmanCompression();
hc.LoadInputData(tf.StringRefs);
hc.serializeTalkfileToExport(export, [bool savepackage]); // you can save package here if you choose.
```

### ME2/ME3
Use ME3Explorer.Unreal.Classes.TalkFile to load a TalkFile. Use ME3Explorer.HuffmanCompression to reserialize back to a file.

Load TLK
```
using ME3Explorer;

TalkFile tf = new TalkFile();
tf.LoadTlkData("PathToYourTLK.tlk");
//Access string refs through 

```

Modify strings
```
///ME2/ME3 TLK also uses ME1Explorer.Unreal.Classes.TalkFile.TLKStringRef objects.
using System.linq;
<tf variable is existing TalkFile object>
var strref = tf.StringRefs.FirstOrDefault(x=>x.StringID == 301204);
if (strref != null) {
    strref.Data = "My new string";
}
```

Save back to file
```
<tf variable is existing TalkFile object>
ME3Explorer.HuffmanCompression hc = new ME3Explorer.HuffmanCompression();
hc.LoadInputData(tf.StringRefs);
hc.SaveToTlkFile("PathToYourTLK.tlk"); // you can save package here if you choose.
```


ME3ExplorerLib is licensed under GPL.
