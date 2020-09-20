using RTCV.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace rpcs3Stub
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Make sure we resolve our dlls
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            Initialize();
        }

        static void Initialize()
        {
            MessageBox.Show("REMEMBER: You need to first set up RPCS3. If you haven't done that, close this program and set up rpcs3 using the quickstart guide on their official website. \n\nNext, choose a decrypted ELF file (You can decrypt PS3 Executables in RPCS3 by clicking 'Utilities->Decrypt PS3 Binaries') to corrupt and execute.\nNot all elfs will execute properly, and not always is EBOOT.BIN the game's true executable.\nSometimes you need to do some fiddling.\n\nAlso, when loading the ELF file, you need to make a VMD starting at address 0x78. This avoids the header.\n\nI recomend using the vector engine with limiter/value lists made for Dolphin, as the PS3 uses a similar architecture to Gamecube and Wii.");
            var frm = new StubForm();
            S.SET<StubForm>(frm);
            Application.Run(frm);

        }


        //Lifted from Bizhawk
        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string requested = args.Name;
                lock (AppDomain.CurrentDomain)
                {
                    var asms = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (var asm in asms)
                        if (asm.FullName == requested)
                        {
                            return asm;
                        }

                    //load missing assemblies by trying to find them in the dll directory
                    string dllname = new AssemblyName(requested).Name + ".dll";
                    string directory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "RTCV");
                    string simpleName = new AssemblyName(requested).Name;
                    string fname = Path.Combine(directory, dllname);
                    if (!File.Exists(fname))
                    {
                        return null;
                    }

                    //it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unamanged) assemblies can't load
                    return Assembly.UnsafeLoadFrom(fname);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Something went really wrong in AssemblyResolve. Send this to the devs\n" + e);
                return null;
            }
        }
    }
}
