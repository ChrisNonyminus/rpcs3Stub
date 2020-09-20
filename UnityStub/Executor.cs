using RTCV.CorruptCore;
using RTCV.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace rpcs3Stub
{
    static class Executor
    {
        public static string gameElf = null;
        public static string rpcs3 = null;

        public static void Execute()
        {

           if (gameElf != null && rpcs3 !=null)
           {

               string fullPath = gameElf;
               string emuPath = rpcs3;
               ProcessStartInfo psi = new ProcessStartInfo();
               psi.FileName = Path.GetFileName(emuPath);
               psi.WorkingDirectory = Path.GetDirectoryName(emuPath);
               psi.Arguments = "\"" + gameElf + "\"";

                try
                {
                    Process.Start(psi);
                }
                catch (Exception) { }
           }
           else
               MessageBox.Show("You need to specify the game elf file and rpcs3's exe.");
           return;
        }
    }
}
