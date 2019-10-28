using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME1Explorer
{
    public interface ITalkFile
    {
        bool Modified { get; set; }
        bool replaceString(int id, string newString);

        string findDataById(int strRefID, bool withFileName = false);
    }
}