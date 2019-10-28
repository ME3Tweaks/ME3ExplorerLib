using System;
using System.Collections.Generic;
using System.IO;
using ME3Explorer.Packages;

namespace ME1Explorer.Unreal.Classes
{
    public class BioTlkFileSet : ITalkFile
    {
        public List<TalkFile> talkFiles;
        public IMEPackage pcc;
        public int index;
        public int selectedTLK;
        public string Name => index != -1 ? (pcc.Exports[index].ObjectName  + "."): null;

        public BioTlkFileSet(IMEPackage _pcc)
        {
            pcc = _pcc;
            index = -1;
            IReadOnlyList<ExportEntry> Exports = pcc.Exports;
            for (int i = 0; i < Exports.Count; i++)
            {
                if (Exports[i].ClassName == "BioTlkFileSet")
                {
                    index = i;
                }
            }
            if (index != -1)
            {
                loadData(index);
            }
            else
            {
                talkFiles = new List<TalkFile>();
            }
        }

        public BioTlkFileSet(IMEPackage _pcc, int _index)
        {
            pcc = _pcc;
            index = _index;
            loadData();
        }

        public void loadData(int _index = -1)
        {
            if (_index != -1)
            {
                index = _index;
            }
            BinaryReader r = new BinaryReader(new MemoryStream(pcc.Exports[index].Data));

            //skip properties
            r.BaseStream.Seek(12, SeekOrigin.Begin);

            if (r.BaseStream.Length > 12)
            {
                int count = r.ReadInt32();
                talkFiles = new List<TalkFile>(count);
                int langRef;
                for (int i = 0; i < count; i++)
                {
                    langRef = r.ReadInt32();
                    r.ReadInt64();
                    talkFiles.Add(new TalkFile(pcc, r.ReadInt32(), true, langRef, index));
                    talkFiles.Add(new TalkFile(pcc, r.ReadInt32(), false, langRef, index));
                }
                for (int i = 0; i < talkFiles.Count; i++)
                {
                    if (talkFiles[i].language == "Int" && !talkFiles[i].male)
                    {
                        selectedTLK = i;
                        break;
                    }
                } 
            }
            else
            {
                talkFiles = new List<TalkFile>();
            }
        }

        public string findDataById(int strRefID, bool withFileName = false)
        {
            if (talkFiles != null)
            {
                if (talkFiles.Count > selectedTLK)
                {
                    return talkFiles[selectedTLK].findDataById(strRefID, withFileName);
                }
                return "No Data";
            }
            else
            {
                return "No Data";
            }
        }

        public bool replaceString(int id, string newString)
        {
            if (talkFiles != null && talkFiles.Count > selectedTLK)
            {
                ME1Explorer.Unreal.Classes.TalkFile tf = talkFiles[selectedTLK];
                for (int i = 0; i < tf.StringRefs.Length; i++)
                {
                    if (tf.StringRefs[i].StringID == id)
                    {
                        tf.StringRefs[i].Data = newString;
                        tf.StringRefs[i].Flags = newString == null ? 0 : 1; //should port to me3explorer.
                        Modified = true;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool Modified { get; set; }
    }
}