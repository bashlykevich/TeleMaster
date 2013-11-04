using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TeleMaster.Helpers
{
    class IOHelper
    {
        public static string GetSafeFilename(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
