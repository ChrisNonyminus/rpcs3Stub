using Newtonsoft.Json;
using RTCV.CorruptCore;
using RTCV.NetCore;
using RTCV.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vanguard;
using rpcs3Stub;

namespace rpcs3Stub
{
    public static class rpcs3Watch
    {
        public static string rpcs3StubVersion = "0.0.1";
        public static string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static rpcs3StubFileInfo currentFileInfo = new rpcs3StubFileInfo();
        public static rpcs3StubFileInfo dataFileInfo = new rpcs3StubFileInfo();
        public static string gameName = null;

        public static bool stubInterfaceEnabled = false;
        const int ELF_OFFSET = 0x78; //We'll implement header exclusion for this later


        public static void Start()
        {
            RTCV.Common.Logging.StartLogging(VanguardCore.logPath);
            if (VanguardCore.vanguardConnected)
                RemoveDomains();

            DisableInterface();

            RtcCore.EmuDirOverride = true; //allows the use of this value before vanguard is connected


            string backupPath = Path.Combine(rpcs3Watch.currentDir, "FILEBACKUPS");
            string paramsPath = Path.Combine(rpcs3Watch.currentDir, "PARAMS");
            string rpcs3Path = Path.Combine(rpcs3Watch.currentDir, "RPCS3");

            if (!Directory.Exists(backupPath))
                Directory.CreateDirectory(backupPath);

            if (!Directory.Exists(paramsPath))
                Directory.CreateDirectory(paramsPath);

            string disclaimerPath = Path.Combine(currentDir, "LICENSES", "DISCLAIMER.TXT");
            string disclaimerReadPath = Path.Combine(currentDir, "PARAMS", "DISCLAIMERREAD");

            if (File.Exists(disclaimerPath) && !File.Exists(disclaimerReadPath))
            {
                MessageBox.Show(File.ReadAllText(disclaimerPath).Replace("[ver]", rpcs3Watch.rpcs3StubVersion), "rpcs3 Stub", MessageBoxButtons.OK, MessageBoxIcon.Information);
                File.Create(disclaimerReadPath);
            }

            //If we can't load the dictionary, quit the wgh to prevent the loss of backups
            if (!FileInterface.LoadCompositeFilenameDico(rpcs3Watch.currentDir))
                Application.Exit();

        }

        private static void RemoveDomains()
        {
            if (currentFileInfo.targetInterface != null)
            {
                currentFileInfo.targetInterface.CloseStream();
                currentFileInfo.targetInterface = null;
            }

            UpdateDomains();
        }

        public static bool RestoreTarget()
        {
            bool success = false;
            if (rpcs3Watch.currentFileInfo.autoUncorrupt)
            {
                if (StockpileManager_EmuSide.UnCorruptBL != null)
                {
                    StockpileManager_EmuSide.UnCorruptBL.Apply(false);
                    success = true;
                }
                else
                {
                    //CHECK CRC WITH BACKUP HERE AND SKIP BACKUP IF WORKING FILE = BACKUP FILE
                   success = rpcs3Watch.currentFileInfo.targetInterface.ResetWorkingFile();
                }
            }
            else
            {
                success = rpcs3Watch.currentFileInfo.targetInterface.ResetWorkingFile();
            }

            return success;
        }

        internal static bool LoadTarget()
        {

            FileInterface.identity = FileInterfaceIdentity.HASHED_PREFIX;

            string filename = null;
            string rpcs3exename = null;
            string gameInstallFolder = null;
            string gameCacheFolder = null;

            OpenFileDialog OpenFileDialog1;
            OpenFileDialog FindRPCS3Dialog;
            FolderBrowserDialog GameInstallLocationDialog;
            FolderBrowserDialog GameCacheLocationDialog;

            OpenFileDialog1 = new OpenFileDialog();

            OpenFileDialog1.Title = "Select a decrypted elf file";
            OpenFileDialog1.Filter = "Executable Files|*.elf";
            OpenFileDialog1.RestoreDirectory = true;
            if (OpenFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (OpenFileDialog1.FileName.ToString().Contains('^'))
                {
                    MessageBox.Show("You can't use a file that contains the character ^ ");
                    return false;
                }

                filename = OpenFileDialog1.FileName;
            }
            else
                return false;

            FindRPCS3Dialog = new OpenFileDialog();

            FindRPCS3Dialog.Title = "Select RPCS3 (Preferably the version that comes with rpcs3Stub)";
            FindRPCS3Dialog.Filter = "Executable Files|rpcs3.exe";
            FindRPCS3Dialog.RestoreDirectory = true;
            if (FindRPCS3Dialog.ShowDialog() == DialogResult.OK)
            {
                if (FindRPCS3Dialog.FileName.ToString().Contains('^'))
                {
                    MessageBox.Show("You can't use a file that contains the character ^ ");
                    return false;
                }

                rpcs3exename = FindRPCS3Dialog.FileName;
            }
            else
                return false;


            FileInfo gameElf = new FileInfo(filename);
            FileInfo rpcs3exe = new FileInfo(rpcs3exename);
            //if (rpcs3Watch.currentFileInfo.selectedTargetType == TargetType.ELF_INSTALLDATA || rpcs3Watch.currentFileInfo.selectedTargetType == TargetType.EVERYTHING)
            //{
            //    GameInstallLocationDialog = new FolderBrowserDialog();
            //    GameInstallLocationDialog.Description = "Select the game's install folder (/[RPCS3 Location]/dev_hdd0/game/[game serial])";
            //    if (GameInstallLocationDialog.ShowDialog() == DialogResult.OK)
            //    {
            //        gameInstallFolder = GameInstallLocationDialog.SelectedPath;
            //    }
            //    else
            //        return false;
            //}
            //else
            //    gameInstallFolder = rpcs3exe.DirectoryName; // this line that basically does nothing is here so the program doesn't flip its shit when the target type in question isn't chosen


            //if (rpcs3Watch.currentFileInfo.selectedTargetType == TargetType.ELF_SHADERCACHE || rpcs3Watch.currentFileInfo.selectedTargetType == TargetType.EVERYTHING)
            //{
            //    GameCacheLocationDialog = new FolderBrowserDialog();

            //    GameCacheLocationDialog.Description = "Select the game's cache folder (/[RPCS3 Location]/cache/[game serial])";
            //    if (GameCacheLocationDialog.ShowDialog() == DialogResult.OK)
            //    {
            //        gameCacheFolder = GameCacheLocationDialog.SelectedPath;
            //    }
            //    else
            //        return false;
            //}
            //else
            //    gameCacheFolder = rpcs3exe.DirectoryName; //ditto

            if (!CloseTarget(false))
                return false;

            //DirectoryInfo gameInstall = new DirectoryInfo(gameInstallFolder).GetDirectories().FirstOrDefault(); //hopefully searches for USRDIR if all goes well
            //DirectoryInfo gameCache = new DirectoryInfo(gameCacheFolder);
            //DirectoryInfo shaderCache = gameCache;
            //if (gameCache.Name != rpcs3exe.DirectoryName) gameCache.GetDirectories().Where(it => it.Name.Contains(gameElf.Name)).FirstOrDefault().GetDirectories().Where(it => it.Name.ToUpper().Contains("SHADERS")).FirstOrDefault().GetDirectories().FirstOrDefault().GetDirectories().FirstOrDefault().GetDirectories().FirstOrDefault();

            rpcs3Watch.currentFileInfo.targetShortName = gameElf.Name;
            rpcs3Watch.currentFileInfo.targetFullName = gameElf.FullName;

            DirectoryInfo elfLocation = gameElf.Directory;

            //var allFiles = DirSearch(elfLocation.FullName).ToArray();
            //DirectoryInfo firstSubFolder = elfLocation.GetDirectories()[0];
            DirectoryInfo PS3_GAME = elfLocation.Parent;

            /*if (allFiles.FirstOrDefault(it => it.ToUpper().Contains("UNITY")) == null)
            {
                MessageBox.Show("Could not find ps3 files");
                return false;
            }*/

            //var allDllFiles = allFiles.Where(it => it.ToUpper().EndsWith(".DLL")).ToArray();
            //var allrpcs3DllFiles = allDllFiles.Where(it => it.ToUpper().Contains("UNITY")).ToArray();
            //var rpcs3EngineDll = allDllFiles.Where(it => it.ToUpper().Contains("BDDATA.DLL")).ToArray();
            //var firstSubfolderDataFiles = DirSearch(firstSubFolder.FullName).ToArray();
            //var gameInstallDataFiles = DirSearch(gameInstall.GetDirectories().FirstOrDefault().FullName).ToArray();
            //var gameShaderCache = DirSearch(shaderCache.FullName).ToArray();
            gameName = PS3_GAME.Parent.Name;

            List<string> targetFiles = new List<string>();

            switch (rpcs3Watch.currentFileInfo.selectedTargetType)
            {
                case TargetType.EBOOTELF:
                    targetFiles.Add(gameElf.FullName);
                    break;
                //case TargetType.ELF_INSTALLDATA:
                //    targetFiles.Add(gameElf.FullName);
                //    targetFiles.AddRange(gameInstallDataFiles);
                //    break;
                //case TargetType.ELF_SHADERCACHE:
                //    targetFiles.Add(gameElf.FullName);
                //    targetFiles.AddRange(gameShaderCache);
                //    break;
                //case TargetType.ELF_BDDATA:
                //    targetFiles.Add(gameElf.FullName);
                //    targetFiles.AddRange(firstSubfolderDataFiles);

                //    break;
                //case TargetType.EVERYTHING:
                //    targetFiles.Add(gameElf.FullName);
                //    targetFiles.AddRange(gameInstallDataFiles);
                //    targetFiles.AddRange(gameShaderCache);
                //    targetFiles.AddRange(firstSubfolderDataFiles);
                //    break;
                //case TargetType.BDDATA:
                //    targetFiles.AddRange(firstSubfolderDataFiles);
                //    break;
            }
            string multipleFiles = "";

            for (int i = 0; i < targetFiles.Count; i++)
            {
                multipleFiles += targetFiles[i];

                if (i < targetFiles.Count - 1)
                    multipleFiles += "|";
            }

            var mfi = new MultipleFileInterface(multipleFiles, rpcs3Watch.currentFileInfo.bigEndian, rpcs3Watch.currentFileInfo.useAutomaticBackups);

            if (rpcs3Watch.currentFileInfo.useCacheAndMultithread)
                mfi.getMemoryDump();

            rpcs3Watch.currentFileInfo.targetInterface = mfi;

            Executor.gameElf = gameElf.FullName;
            Executor.rpcs3 = rpcs3exe.FullName;

            StockpileManager_EmuSide.UnCorruptBL = null;

            if (VanguardCore.vanguardConnected)
                rpcs3Watch.UpdateDomains();

            return true;
        }

        private static List<String> DirSearch(string sDir)
        {
            List<String> files = new List<String>();
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    files.Add(f);
                }
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    files.AddRange(DirSearch(d));
                }
            }
            catch (System.Exception excpt)
            {
                MessageBox.Show(excpt.Message);
            }

            return files;
        }

        internal static void KillProcess()
        {

            if (Executor.gameElf != null)
            {

                string otherProgramShortFilename = Path.GetFileName(Executor.rpcs3);

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "taskkill";
                startInfo.Arguments = $"/IM \"{otherProgramShortFilename}\"";
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                Process processTemp = new Process();
                processTemp.StartInfo = startInfo;
                processTemp.EnableRaisingEvents = true;
                try
                {
                    processTemp.Start();
                    processTemp.WaitForExit();
                    Thread.Sleep(500); //Add an artificial delay as sometimes the handles aren't released immediately even though the process has terminated
                }
                catch (Exception ex)
                {
                    throw ex;
                }


            }
        }
        internal static bool CloseTarget(bool updateDomains = true)
        {
            if (rpcs3Watch.currentFileInfo.targetInterface != null)
            {
                rpcs3Watch.KillProcess();
                if (!rpcs3Watch.RestoreTarget())
                {
                    MessageBox.Show("Unable to restore the backup. Aborting!");
                    return false;
                }

                rpcs3Watch.currentFileInfo.targetInterface.CloseStream();
                rpcs3Watch.currentFileInfo.targetInterface = null;
            }

            if (updateDomains)
                UpdateDomains();
            return true;
        }

        public static void UpdateDomains()
        {
            try
            {
                PartialSpec gameDone = new PartialSpec("VanguardSpec");
                gameDone[VSPEC.SYSTEM] = "PS3";
                gameDone[VSPEC.GAMENAME] = gameName;
                gameDone[VSPEC.SYSTEMPREFIX] = "rpcs3Stub";
                gameDone[VSPEC.SYSTEMCORE] = "rpcs3";
                //gameDone[VSPEC.SYNCSETTINGS] = BIZHAWK_GETSET_SYNCSETTINGS;
                gameDone[VSPEC.OPENROMFILENAME] = currentFileInfo.targetFullName;
                gameDone[VSPEC.MEMORYDOMAINS_BLACKLISTEDDOMAINS] = new string[0];
                gameDone[VSPEC.MEMORYDOMAINS_INTERFACES] = GetInterfaces();
                gameDone[VSPEC.CORE_DISKBASED] = false;
                AllSpec.VanguardSpec.Update(gameDone);

                //This is local. If the domains changed it propgates over netcore
                LocalNetCoreRouter.Route(NetcoreCommands.CORRUPTCORE, NetcoreCommands.REMOTE_EVENT_DOMAINSUPDATED, true, true);

                //Asks RTC to restrict any features unsupported by the stub
                LocalNetCoreRouter.Route(NetcoreCommands.CORRUPTCORE, NetcoreCommands.REMOTE_EVENT_RESTRICTFEATURES, true, true);

            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex) == DialogResult.Abort)
                    throw new RTCV.NetCore.AbortEverythingException();
            }
        }

        public static MemoryDomainProxy[] GetInterfaces()
        {
            try
            {
                Console.WriteLine($" getInterfaces()");
                if (currentFileInfo.targetInterface == null)
                {
                    Console.WriteLine($"rpxInterface was null!");
                    return new MemoryDomainProxy[] { };
                }

                List<MemoryDomainProxy> interfaces = new List<MemoryDomainProxy>();


                foreach (var fi in (currentFileInfo.targetInterface as MultipleFileInterface).FileInterfaces)
                    interfaces.Add(new MemoryDomainProxy(fi));

                foreach (MemoryDomainProxy mdp in interfaces)
                    mdp.BigEndian = currentFileInfo.bigEndian;

                return interfaces.ToArray();
            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex, true) == DialogResult.Abort)
                    throw new RTCV.NetCore.AbortEverythingException();

                return new MemoryDomainProxy[] { };
            }

        }

        public static void EnableInterface()
        {
            S.GET<StubForm>().btnResetBackup.Enabled = true;
            S.GET<StubForm>().btnRestoreBackup.Enabled = true;

            stubInterfaceEnabled = true;
        }
        public static void DisableInterface()
        {
            S.GET<StubForm>().btnResetBackup.Enabled = false;
            S.GET<StubForm>().btnRestoreBackup.Enabled = false;
            stubInterfaceEnabled = false;
        }

    }


}
