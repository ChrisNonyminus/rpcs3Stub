using RTCV.CorruptCore;
using RTCV.NetCore;
using RTCV.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vanguard;

namespace rpcs3Stub
{
    public partial class StubForm : Form
    {

        public StubForm()
        {
            InitializeComponent();

            SyncObjectSingleton.SyncObject = this;


            Text += rpcs3Watch.rpcs3StubVersion;

            this.cbTargetType.Items.AddRange(new object[] {
                //TargetType.ELF_SHADERCACHE,
                //TargetType.ELF_INSTALLDATA,
                TargetType.EBOOTELF,
                //TargetType.ELF_BDDATA,
                //TargetType.BDDATA,
                //TargetType.EVERYTHING,
            });

        }

        private void StubForm_Load(object sender, EventArgs e)
        {
            cbTargetType.SelectedIndex = 0;

            UICore.SetRTCColor(Color.Aquamarine, this);

            rpcs3Watch.Start();
        }

        Size originalLbTargetSize;
        Point originalLbTargetLocation;
        public void EnableInterface()
        {
            var diff = lbTarget.Location.X - btnBrowseTarget.Location.X;
            originalLbTargetLocation = lbTarget.Location;
            lbTarget.Location = btnBrowseTarget.Location;
            lbTarget.Visible = true;

            btnTargetSettings.Visible = false;

            btnBrowseTarget.Visible = false;
            originalLbTargetSize = lbTarget.Size;
            lbTarget.Size = new Size(lbTarget.Size.Width + diff, lbTarget.Size.Height);
            btnUnloadTarget.Visible = true;
            cbTargetType.Enabled = false;

            //lbTargetExecution.Enabled = true;
            //pnTargetExecution.Enabled = true;

            rpcs3Watch.EnableInterface();

            lbTarget.Text = rpcs3Watch.currentFileInfo.selectedTargetType.ToString() + " target loaded";
            lbTargetStatus.Text = rpcs3Watch.currentFileInfo.selectedTargetType.ToString() + " target loaded";
        }

        public void DisableInterface()
        {
            btnUnloadTarget.Visible = false;
            btnBrowseTarget.Visible = true;
            lbTarget.Size = originalLbTargetSize;
            lbTarget.Location = originalLbTargetLocation;
            lbTarget.Visible = false;
            cbTargetType.Enabled = true;

            btnTargetSettings.Visible = true;

            btnRestoreBackup.Enabled = false;
            btnResetBackup.Enabled = false;
            lbTarget.Text = "No target selected";
            lbTargetStatus.Text = "No target selected";
        }

        private void BtnBrowseTarget_Click(object sender, EventArgs e)
        {

            if (!rpcs3Watch.LoadTarget())
                return;

            if (!VanguardCore.vanguardConnected)
                VanguardCore.Start();

            EnableInterface();

        }

        private void BtnReleaseTarget_Click(object sender, EventArgs e)
        {
            if(!rpcs3Watch.CloseTarget())
                return;
            DisableInterface();
        }

        private void CbTargetType_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if(cbSelectedExecution.SelectedItem.ToString())
            rpcs3Watch.currentFileInfo.selectedTargetType = cbTargetType.SelectedItem.ToString();

        }

        private void BtnRestoreBackup_Click(object sender, EventArgs e)
        {
            rpcs3Watch.KillProcess();
            rpcs3Watch.currentFileInfo.targetInterface?.CloseStream();
            rpcs3Watch.currentFileInfo.targetInterface?.RestoreBackup();
        }

        private void BtnResetBackup_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
@"This resets the backup of the current target by using the current data from it.
If you override a clean backup using a corrupted file,
you won't be able to restore the original file using it.

Are you sure you want to reset the current target's backup?", "WARNING", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            rpcs3Watch.currentFileInfo.targetInterface?.ResetBackup(true);

        }

        private void BtnClearAllBackups_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear ALL THE BACKUPS\n from rpcs3Stub's cache?", "WARNING", MessageBoxButtons.YesNo) == DialogResult.No)
                return;


            rpcs3Watch.currentFileInfo.targetInterface?.RestoreBackup();

            foreach (string file in Directory.GetFiles(Path.Combine(rpcs3Watch.currentDir,"FILEBACKUPS")))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    MessageBox.Show($"Could not delete file {file}");
                }
            }

            FileInterface.CompositeFilenameDico = new Dictionary<string, string>();
            rpcs3Watch.currentFileInfo.targetInterface?.ResetBackup(false);
            FileInterface.SaveCompositeFilenameDico();
            MessageBox.Show("All the backups were cleared.");
        }

        private void BtnTargetSettings_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Control c = (Control)sender;
                Point locate = new Point(c.Location.X + e.Location.X, ((Control)sender).Location.Y + e.Location.Y);

                ContextMenuStrip columnsMenu = new ContextMenuStrip();


                ((ToolStripMenuItem)columnsMenu.Items.Add("Big endian", null, new EventHandler((ob, ev) => {

                    rpcs3Watch.currentFileInfo.bigEndian = !rpcs3Watch.currentFileInfo.bigEndian;

                    if (VanguardCore.vanguardConnected)
                        rpcs3Watch.UpdateDomains();

                }))).Checked = rpcs3Watch.currentFileInfo.bigEndian;

                ((ToolStripMenuItem)columnsMenu.Items.Add("Auto-Uncorrupt", null, new EventHandler((ob, ev) => {

                    rpcs3Watch.currentFileInfo.autoUncorrupt = !rpcs3Watch.currentFileInfo.autoUncorrupt;

                }))).Checked = rpcs3Watch.currentFileInfo.autoUncorrupt;

                ((ToolStripMenuItem)columnsMenu.Items.Add("Use Caching + Multithreading", null, new EventHandler((ob, ev) => {

                    rpcs3Watch.currentFileInfo.useCacheAndMultithread = !rpcs3Watch.currentFileInfo.useCacheAndMultithread;

                }))).Checked = rpcs3Watch.currentFileInfo.useCacheAndMultithread;

                columnsMenu.Show(this, locate);
            }
        }

        private void StubForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!rpcs3Watch.CloseTarget(false))
                e.Cancel = true;
        }
    }
}
