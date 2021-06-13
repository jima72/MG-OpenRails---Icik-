﻿using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using Ionic.Zip;

namespace ORTS
{
    public partial class Update : Form
    {
        public Update()
        {
            InitializeComponent();
        }

        private void Update_Load(object sender, EventArgs e)
        {
            try
            {
                Ping ping = new Ping();
                PingReply pr = ping.Send("lkpr.aspone.cz");
                if (pr.Status != IPStatus.Success)
                {
                    Close();
                    return;
                }
                string versionPath = Application.StartupPath;
                versionPath = versionPath + "\\version.ini";
                if (!File.Exists(versionPath))
                {
                    File.WriteAllText(versionPath, "1");
                }
                string version = File.ReadAllText(versionPath);
                WebClient webClient = new WebClient();
                string s = webClient.DownloadString("http://lkpr.aspone.cz/or/version.txt");
                if (version != s) // new version available
                {
                    File.Delete(Application.StartupPath + "\\Update.zip");
                    webClient.DownloadFile("http://lkpr.aspone.cz/or/update.zip", Application.StartupPath + "\\Update.zip");
                    ZipFile zip = new ZipFile(Application.StartupPath + "\\Update.zip");
                    zip.ExtractAll(Application.StartupPath, ExtractExistingFileAction.OverwriteSilently);
                }
                File.WriteAllText(versionPath, s);
            }
            catch (Exception ex) { MessageBox.Show("Chyba aktualizace." + Environment.NewLine + ex.Message, "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); }
            Close();
        }
    }
}
