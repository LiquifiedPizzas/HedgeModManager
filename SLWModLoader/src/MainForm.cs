﻿using System;
using System.IO;
using System.Windows.Forms;
using SLWModLoader.Properties;
using System.Diagnostics;
using System.Net;
using System.Collections.Generic;

namespace SLWModLoader
{
    public partial class MainForm : Form
    {
        public static readonly string GensExecutablePath = Path.Combine(Program.StartDirectory, "SonicGenerations.exe");
        public static readonly string LWExecutablePath = Path.Combine(Program.StartDirectory, "slw.exe");
        public static readonly string ModsFolderPath = Path.Combine(Program.StartDirectory, "mods");
        public static readonly string ModsDbPath = Path.Combine(ModsFolderPath, "ModsDB.ini");
        public ModsDatabase ModsDb;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            LogFile.AddEmptyLine();
            LogFile.AddMessage("The form has been closed.");

            LogFile.Close();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Text += $" (v{Program.VersionString})";

            if(File.Exists(LWExecutablePath))
                Text += " - Sonic Lost World";

            if (File.Exists(GensExecutablePath))
                Text += " - Sonic Generations";

            if (File.Exists(LWExecutablePath) || File.Exists(GensExecutablePath))
            {
                LoadMods();
                OrderModList();
                if(!isCPKREDIRInstalled())
                {
                    if (MessageBox.Show("Your "+(File.Exists(LWExecutablePath) ? "Sonic Lost World" : "Sonic Generations") +" executable has not yet been Installed for use with CPKREDIR, which is required to load mods.\nWould you like to patch it now?", Program.ProgramName, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        InstallCPKREDIR();
                    }
                }
                PatchLabel.Text = (File.Exists(LWExecutablePath) ? Path.GetFileName(LWExecutablePath) : Path.GetFileName(GensExecutablePath)) +
                ": " + (isCPKREDIRInstalled() ? "Installed" : "Not Installed");
            }
            else
            {
                MessageBox.Show(Resources.CannotFindExecutableText, Resources.ApplicationTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                LogFile.AddMessage("Could not find executable, closing form...");

                Close();
                return;
            }

            if (!Directory.Exists(ModsFolderPath))
            {
                if (MessageBox.Show(Resources.CannotFindModsDirectoryText, Resources.ApplicationTitle,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    LogFile.AddMessage($"Creating mods folder at \"{ModsFolderPath}\"...");
                    Directory.CreateDirectory(ModsFolderPath);
                }
            }
        }

        public void LoadMods()
        {
            if (File.Exists(ModsDbPath))
            {
                LogFile.AddMessage("Found ModsDB, loading mods...");
                ModsDb = new ModsDatabase(ModsDbPath, ModsFolderPath);
                LogFile.AddMessage($"\t{ModsDb.ActiveModCount} / {ModsDb.ModCount} mods are active.");
            }
            else
            {
                LogFile.AddMessage("Could not find ModsDB, creating one...");
                ModsDb = new ModsDatabase(ModsFolderPath);
            }

            LogFile.AddMessage($"Loaded total {ModsDb.ModCount} mods from \"{ModsFolderPath}\".");

            for (int i = 0; i < ModsDb.ModCount; ++i)
            {
                Mod modItem = ModsDb.GetMod(i);

                ListViewItem modListViewItem = new ListViewItem(modItem.Title);
                modListViewItem.Tag = modItem;
                modListViewItem.SubItems.Add(modItem.Version);
                modListViewItem.SubItems.Add(modItem.Author);
                modListViewItem.SubItems.Add(modItem.SaveFile.Length > 0 ? "Yes" : "No");
                modListViewItem.SubItems.Add(modItem.UpdateServer.Length > 0 ? "Check" : "N/A");

                if (ModsDb.IsModActive(modItem))
                {
                    modListViewItem.Checked = true;
                }

                ModsList.Items.Add(modListViewItem);
            }

            NoModsFoundLabel.Visible = linkLabel1.Visible = (ModsDb.ModCount <= 0);
            LogFile.AddMessage("Succesfully updated list view!");
        }

        // Move Active mods to the top of the list
        public void OrderModList()
        {
            IniFile modsDBIni = ModsDb.getIniFile();
            int count = int.Parse(modsDBIni["Main"]["ActiveModCount"]);
            int index = 0;
            for (int i = 0; i < count; i++)
            {
                foreach (ListViewItem lvi in ModsList.Items)
                {
                    if(lvi.Text.Equals(modsDBIni["Main"]["ActiveMod" + i]))
                    {
                        ModsList.Items.Remove(lvi);
                        ModsList.Items.Insert(index++, lvi);
                    }
                }
            }
        }

        public void RefreshModsList()
        {
            ModsList.Items.Clear();
            LoadMods();
            OrderModList();
            ModsList.Select();
        }

        private void SaveModDB()
        {
            // Getting the ModDb.ini file
            IniFile modsDBIni = ModsDb.getIniFile();
            // Deactivates All mods that were active
            ModsDb.DeactivateAllMods();
            // Activates all mods that are checked
            foreach (ListViewItem lvi in ModsList.Items)
            {
                if(lvi.Checked)
                    ModsDb.ActivateMod(ModsDb.GetMod(lvi.Text));
            }
            
            // Saving and refreshing the mod list
            ModsDb.SaveModsDb(ModsDbPath);
            RefreshModsList();

        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            RefreshModsList();
        }

        private void SaveAndPlayButton_Click(object sender, EventArgs e)
        {
            try
            {
                SaveModDB();
                AddMessage("ModsDB Saved");
                StartGame();
            }
            catch (Exception ex)
            {
                AddMessage("Exception thrown while saving ModsDB and starting: "+ex);
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                SaveModDB();
                AddMessage("ModsDB Saved");
            }
            catch (Exception ex)
            {
                AddMessage("Exception thrown while saving ModsDB: "+ex);
            }
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            try
            {
                StartGame();
            }
            catch (Exception ex)
            {
                AddMessage("Exception thrown while starting: " + ex);
            }
        }

        public void StartGame()
        {
            if (File.Exists(LWExecutablePath))
            {
                AddMessage("Starting Sonic Lost World...");
                Process.Start("steam://rungameid/329440");
                Close();
            }
            else if (File.Exists(GensExecutablePath))
            {
                AddMessage("Starting Sonic Generations...");
                Process.Start("steam://rungameid/71340");
                Close();
            }
        }

        public void AddMessage(string message)
        {
            LogFile.AddMessage(message);
            Invoke(new Action(() => StatusLabel.Text = message));
        }

        public bool isCPKREDIRInstalled()
        {
            bool gens = false;
            if (File.Exists(GensExecutablePath))
                gens = true;
            try
            {
                byte[] bytes = File.ReadAllBytes(gens ? GensExecutablePath : LWExecutablePath);
                for (int i = 11918000; i < bytes.Length; ++i)
                {
                    // 63 70 6B 72 65 64 69 72
                    // c  p  k  r  e  d  i  r 

                    if (bytes[  i  ] == 0x63 && bytes[i + 1] == 0x70 && bytes[i + 2] == 0x6B &&
                        bytes[i + 3] == 0x72 && bytes[i + 4] == 0x65 && bytes[i + 5] == 0x64 &&
                        bytes[i + 6] == 0x69 && bytes[i + 7] == 0x72)
                        return true;

                    // 69 6D 61 67 65 68 6C 70
                    // i  m  a  g  e  h  l  p
                    if (bytes[i] == 0x69 && bytes[i + 1] == 0x6D && bytes[i + 2] == 0x61 &&
                        bytes[i + 3] == 0x67 && bytes[i + 4] == 0x65 && bytes[i + 5] == 0x68 &&
                        bytes[i + 6] == 0x6C && bytes[i + 7] == 0x70)
                        return false;
                }
            }
            catch(Exception ex)
            {
                AddMessage("Exception thrown while checking executeable: " + ex);
            }
            AddMessage("Failed to check executeable");
            return false;
        }

        public bool InstallCPKREDIR()
        {
            string executablePath = LWExecutablePath;
            if (File.Exists(GensExecutablePath))
                executablePath = GensExecutablePath;
            try
            {
                AddMessage("Installing CPKREDIR");
                byte[] bytes = File.ReadAllBytes(executablePath);
                for (int i = 11918000; i < bytes.Length; ++i)
                {
                    // 63 70 6B 72 65 64 69 72
                    // c  p  k  r  e  d  i  r 

                    if (bytes[  i  ] == 0x63 && bytes[i + 1] == 0x70 && bytes[i + 2] == 0x6B &&
                        bytes[i + 3] == 0x72 && bytes[i + 4] == 0x65 && bytes[i + 5] == 0x64 &&
                        bytes[i + 6] == 0x69 && bytes[i + 7] == 0x72)
                    {
                        AddMessage("CPKREDIR is already installed");
                        return false;
                    }

                    // 69 6D 61 67 65 68 6C 70
                    // i  m  a  g  e  h  l  p

                    if (bytes[  i  ] == 0x69 && bytes[i + 1] == 0x6D && bytes[i + 2] == 0x61 &&
                        bytes[i + 3] == 0x67 && bytes[i + 4] == 0x65 && bytes[i + 5] == 0x68 &&
                        bytes[i + 6] == 0x6C && bytes[i + 7] == 0x70)
                    {
                        // writing "cpkredir" to the executeable
                        bytes[  i  ] = 0x63;
                        bytes[i + 1] = 0x70;
                        bytes[i + 2] = 0x6B;
                        bytes[i + 3] = 0x72;
                        bytes[i + 4] = 0x65;
                        bytes[i + 5] = 0x64;
                        bytes[i + 6] = 0x69;
                        bytes[i + 7] = 0x72;

                        // Backing up the original executeable
                        if (!File.Exists(executablePath.Substring(0, executablePath.Length-4) + "_Backup.exe"))
                        {
                            File.Move(executablePath, executablePath.Substring(0, executablePath.Length - 4) + "_Backup.exe");
                        }else
                        {
                            File.Delete(executablePath);
                        }

                        // Now we're writing the newly modified exe.
                        File.WriteAllBytes(executablePath, bytes);
                        AddMessage("Done.");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                AddMessage("Exception thrown while installing CPKREDIR: " + ex);
            }
            return false;
        }

        public bool UninstallCPKREDIR()
        {
            string executablePath = LWExecutablePath;
            if (File.Exists(GensExecutablePath))
                executablePath = GensExecutablePath;
            try
            {
                AddMessage("Uninstalling CPKREDIR");
                byte[] bytes = File.ReadAllBytes(executablePath);
                for (int i = 11918000; i < bytes.Length; ++i)
                {
                    // 63 70 6B 72 65 64 69 72
                    // c  p  k  r  e  d  i  r 

                    if (bytes[  i  ] == 0x63 && bytes[i + 1] == 0x70 && bytes[i + 2] == 0x6B &&
                        bytes[i + 3] == 0x72 && bytes[i + 4] == 0x65 && bytes[i + 5] == 0x64 &&
                        bytes[i + 6] == 0x69 && bytes[i + 7] == 0x72)
                    {
                        // writing "imagehlp" to the executeable
                        bytes[  i  ] = 0x69;
                        bytes[i + 1] = 0x6D;
                        bytes[i + 2] = 0x61;
                        bytes[i + 3] = 0x67;
                        bytes[i + 4] = 0x65;
                        bytes[i + 5] = 0x68;
                        bytes[i + 6] = 0x6C;
                        bytes[i + 7] = 0x70;

                        // Deleting the old executable
                        File.Delete(executablePath);
                        
                        // Now we're writing the newly modified exe.
                        File.WriteAllBytes(executablePath, bytes);
                        AddMessage("Done.");
                        return true;
                    }

                    // 69 6D 61 67 65 68 6C 70
                    // i  m  a  g  e  h  l  p

                    if (bytes[  i  ] == 0x69 && bytes[i + 1] == 0x6D && bytes[i + 2] == 0x61 &&
                        bytes[i + 3] == 0x67 && bytes[i + 4] == 0x65 && bytes[i + 5] == 0x68 &&
                        bytes[i + 6] == 0x6C && bytes[i + 7] == 0x70)
                    {
                        AddMessage("CPKREDIR is not installed");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                AddMessage("Exception thrown while uninstalling CPKREDIR: " + ex);
            }
            return false;
        }

        private void AboutButton_Click(object sender, EventArgs e)
        {
            new AboutForm().ShowDialog();
        }

        private void ReportLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/Radfordhound/SLW-Mod-Loader/issues/new");
        }


        private void ScanExecuteableButton_Click(object sender, EventArgs e)
        {
            StatusLabel.Text = "";
            PatchLabel.Text = (File.Exists(LWExecutablePath) ? Path.GetFileName(LWExecutablePath) : Path.GetFileName(GensExecutablePath)) +
                ": " + (isCPKREDIRInstalled() ? "Installed" : "Not Installed");
        }

        private void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var modItem = ModsDb.GetMod(ModsList.FocusedItem.Text);
            if (modItem == null) return;
            
            if(modItem.UpdateServer.Length == 0 && modItem.Url.Length != 0)
            { // No Update Server, But has Website
                if (MessageBox.Show($"{Program.ProgramName} can not check for updates for {modItem.Title} because no update server has been set.\n\nThis Mod does have a website, do you want to open it to check for updates manually?\n\n URL: {modItem.Url}", Program.ProgramName, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start(modItem.Url);
                }
            }else if (modItem.UpdateServer.Length == 0 && modItem.Url.Length == 0)
            { // No Update Server and Website
                MessageBox.Show($"{Program.ProgramName} can not check for updates for {modItem.Title} because no update server has been set.", Program.ProgramName, MessageBoxButtons.OK);
            }else if (modItem.UpdateServer.Length != 0)
            { // Has Update Server
                try
                {
                    WebClient wc = new WebClient();
                    wc.DownloadFile(modItem.UpdateServer + "mod_version.ini", Path.Combine(modItem.RootDirectory, "mod_version.ini.temp"));
                    IniFile mod_version = new IniFile(Path.Combine(modItem.RootDirectory, "mod_version.ini.temp"));
                    if (mod_version["Main"]["VersionString"] != modItem.Version)
                    {
                        if (MessageBox.Show($"There's a newer version of {modItem.Title} available!\n\n"+
                                $"Do you want to update from version {modItem.Version} to {mod_version["Main"]["VersionString"]}? (about {mod_version["Main"]["DownloadSizeString"]})", Program.ProgramName, MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            UpdateModForm muf = new UpdateModForm(modItem.Title, wc.DownloadString(modItem.UpdateServer + "mod_files.txt"), modItem.RootDirectory);
                            muf.ShowDialog();
                        }
                    }
                    else
                    {
                        MessageBox.Show($"{modItem.Title} is already up to date.");
                    }
                }catch(Exception ex)
                {
                    AddMessage("Exception thrown while updating: " + ex);
                    MessageBox.Show("Exception thrown while updating: \n\n" + ex);
                }
            }
        }

        private void ModsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            MoveUpButton.Enabled = MoveDownButton.Enabled = RemoveModButton.Enabled =
                checkForUpdatesToolStripMenuItem.Enabled = openModFolderToolStripMenuItem.Enabled = ModsList.SelectedItems.Count == 1;
        }

        private void AddModButton_Click(object sender, EventArgs e)
        {
            new AddModForm().ShowDialog();
            RefreshModsList();
        }

        private void InstallUninstallButton_Click(object sender, EventArgs e)
        {
            StatusLabel.Text = "";
            if (isCPKREDIRInstalled()) UninstallCPKREDIR();
            else InstallCPKREDIR();
            PatchLabel.Text = (File.Exists(LWExecutablePath) ? Path.GetFileName(LWExecutablePath) : Path.GetFileName(GensExecutablePath)) +
                ": " + (isCPKREDIRInstalled() ? "Installed" : "Not Installed");
        }

        private void openModFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("explorer", ModsDb.GetMod(ModsList.FocusedItem.Text).RootDirectory);
        }

        private void RemoveModButton_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Are you sure you want to delete \""+ ModsList.FocusedItem.Text+"\"?", Program.ProgramName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                Directory.Delete(ModsDb.GetMod(ModsList.FocusedItem.Text).RootDirectory, true);
                RefreshModsList();

            }
            ModsList.FocusedItem = null;
            RemoveModButton.Enabled = false;
        }

        // TODO: Use ModDB methods
        private void MoveUpButton_Click(object sender, EventArgs e)
        {
            // Checks if a item is selected and is not the first item
            if (ModsList.FocusedItem == null || ModsList.FocusedItem.Index == 0) return;
            // Getting the ModDb.ini file
            IniFile modsDBIni = ModsDb.getIniFile();
            // Gets the amount of active mods
            int count = int.Parse(modsDBIni["Main"]["ActiveModCount"]);
            // Checks if the selected item index is not above or equals to "ActiveModCount"
            if (ModsList.FocusedItem.Index >= count) return;
            // Get the title of the selected mod
            string modTitle = ModsList.FocusedItem.Text;
            // A list storing a list of active mods
            List<string> activeMods = new List<string>();
            // Adding the mod titles from the ini into the activeMods array and also removes them from the ini
            for (int i = 0; i < count; ++i)
            {
                activeMods.Add(modsDBIni["Main"]["ActiveMod" + i]);
                modsDBIni["Main"].RemoveParameter("ActiveMod" + i);
            }

            // Getting the position of the selected mod so we can move it up
            int pos = activeMods.IndexOf(modTitle);
            // Removes the title and reinserts it back in before where it used to be
            activeMods.Remove(modTitle);
            activeMods.Insert(pos - 1, modTitle);

            // Writing the activeMods array into the modDb.ini file
            for (int i = 0; i < activeMods.Count; ++i)
            {
                modsDBIni["Main"].AddParameter("ActiveMod" + i, activeMods[i]);
            }

            // Saving and refreshing the mod list
            ModsDb.SaveModsDb();
            RefreshModsList();
        }

        // TODO: Use ModDB methods
        private void MoveDownButton_Click(object sender, EventArgs e)
        {
            // Checks if a item is selected
            if (ModsList.FocusedItem == null) return;
            // Getting the ModDb.ini file
            IniFile modsDBIni = ModsDb.getIniFile();
            // Gets the amount of active mods
            int count = int.Parse(modsDBIni["Main"]["ActiveModCount"]);
            // Checks if the item seleced is not the last one
            if (ModsList.FocusedItem.Index >= count-1) return;
            // Get the title of the selected mod
            string modTitle = ModsList.FocusedItem.Text;
            // A list storing a list of active mods
            List<string> activeMods = new List<string>();
            // Adding the mod titles from the ini into the activeMods array and also removes them from the ini too
            for (int i = 0; i < count; ++i)
            {
                activeMods.Add(modsDBIni["Main"]["ActiveMod" + i]);
                modsDBIni["Main"].RemoveParameter("ActiveMod" + i);
            }

            // Getting the position of the selected mod so we can move it down
            int pos = activeMods.IndexOf(modTitle);
            // Removes the title and reinserts it back in after where it used to be
            activeMods.Remove(modTitle);
            activeMods.Insert(pos + 1, modTitle);

            // Writing the activeMods array into the modDb.ini file
            for (int i = 0; i < activeMods.Count; ++i)
            {
                modsDBIni["Main"].AddParameter("ActiveMod" + i, activeMods[i]);
            }

            // Saving and refreshing the mod list
            ModsDb.SaveModsDb();
            RefreshModsList();
        }

    }
}