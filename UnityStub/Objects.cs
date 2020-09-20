using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.IO;
using Ceras;
using RTCV.CorruptCore;

namespace rpcs3Stub
{

    public class rpcs3StubFileInfo
    {
        internal string targetShortName = "No target";
        internal bool writeCopyMode = false;
        internal string targetFullName = "No target";
        internal FileMemoryInterface targetInterface;
        internal string selectedTargetType = TargetType.EBOOTELF;
        internal bool autoUncorrupt = true;
        internal bool TerminateBeforeExecution = true;
        internal bool useAutomaticBackups = true;
        internal bool bigEndian = true;
        internal bool useCacheAndMultithread = true;

        public override string ToString()
        {
            return targetShortName;
        }
    }

    public static class TargetType
    {
        //comment out everything except for the elf target because the code for searching for the shader cache is borked
        //public const string ELF_INSTALLDATA = "Decrypted ELF file for game, along with the install folder";
        //public const string ELF_SHADERCACHE = "Decrypted ELF file and any detected shader caches (probably won't work unless you don't corrupt the ELF)";
        public const string ELF_BDDATA = "Decrypted ELF file and first available data subfolder";
        public const string EBOOTELF = "Decrypted ELF file";
        public const string BDDATA = "Contents of first available data subflder";
        //public const string EVERYTHING = "All of the above";
    }

}
