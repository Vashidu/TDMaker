﻿using BDInfo;
using ShareX.HelpersLib;
using ShareX.MediaLib;
using ShareX.UploadersLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using TDMakerGUI.Properties;
using TDMakerLib;
using UploadersLib;
using UploadersLib.HelperClasses;

namespace TDMaker
{
    public partial class MainWindow : Form
    {
        private bool IsGuiReady, IsClosing = false;

        #region Main Window events

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.All : DragDropEffects.None;
        }

        private void MainWindow_DragDrop(object sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData(DataFormats.FileDrop, true);
            LoadMedia(paths);
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            ConfigureTemplates();

            LoadSettingsToControls();

            sBar.Text = string.Format(Resources.MainWindow_bwApp_RunWorkerCompleted_Ready_);

            tttvMain.MainTabControl = tcMain;

            Icon = Resources.GenuineAdvIcon;
        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {
            App.LoadProxySettings();
            rtbDebugLog.Text = DebugHelper.Logger.ToString();
            DebugHelper.Logger.MessageAdded += Logger_MessageAdded;

            ValidateThumbnailerPaths(sender, e);

            if (ProgramUI.ExplorerFilePaths.Count > 0)
            {
                LoadMedia(ProgramUI.ExplorerFilePaths.ToArray());
            }

            IsGuiReady = true;

            UpdateGuiControls();

#if !DEBUG
            AutoCheckUpdate();
#endif
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            IsClosing = true;
        }

        #endregion Main Window events

        private void ValidateThumbnailerPaths(object sender, EventArgs e)
        {
            switch (App.Settings.ThumbnailerType)
            {
                case ThumbnailerType.FFmpeg:
                    if (!File.Exists(App.Settings.FFmpegPath))
                    {
                        DialogResult result = MessageBox.Show(Resources.MainWindow_MainWindow_Shown_, Application.ProductName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            btnDownloadFFmpeg_Click(sender, e);
                        }
                        else if (result == DialogResult.No)
                        {
                            OpenFileDialog dlg = new OpenFileDialog();
                            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                            dlg.Title = Resources.MainWindow_MainWindow_Shown_Browse_for_ffmpeg_exe;
                            dlg.Filter = Resources.MainWindow_MainWindow_Shown_Applications__ffmpeg_exe__ffmpeg_exe;
                            if (dlg.ShowDialog() == DialogResult.OK)
                            {
                                App.Settings.FFmpegPath = dlg.FileName;
                            }
                        }
                    }
                    break;
                case ThumbnailerType.MPlayer:
                    if (!File.Exists(App.Settings.MPlayerPath))
                    {
                        var dlg = new OpenFileDialog
                        {
                            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                        };
                        const string mplayer = "http://mplayerwin.sourceforge.net/downloads.html";
                        dlg.Title = Resources.MainWindow_MainWindow_Shown_Browse_for_mplayer_exe_or_download_from_ + mplayer;
                        dlg.Filter = Resources.MainWindow_MainWindow_Shown_Applications__mplayer_exe__mplayer_exe;
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            App.Settings.MPlayerPath = dlg.FileName;
                        }
                        else
                        {
                            URLHelpers.OpenURL(mplayer);
                        }
                    }
                    break;
            }
        }

        private void AutoCheckUpdate()
        {
            if (App.Settings.AutoCheckUpdate)
            {
                Thread updateThread = new Thread(CheckUpdate);
                updateThread.IsBackground = true;
                updateThread.Start();
            }
        }

        private void CheckUpdate()
        {
            UpdateChecker updateChecker = AboutBox.CheckUpdate();
            UpdateMessageBox.Start(updateChecker);
        }

        private void Logger_MessageAdded(string message)
        {
            if (!IsClosing && !rtbDebugLog.IsDisposed)
            {
                MethodInvoker method = delegate
                {
                    rtbDebugLog.AppendText(message + Environment.NewLine);
                };

                if (InvokeRequired)
                {
                    Invoke(method);
                }
                else
                {
                    method.Invoke();
                }
            }
        }

        private void OpenFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = true;
            dlg.Title = Resources.MainWindow_OpenFile_Browse_for_Media_file___;
            dlg.InitialDirectory = App.Settings.ProfileActive.DefaultMediaDirectory;
            StringBuilder sbExt = new StringBuilder();
            sbExt.Append("Media Files (");
            StringBuilder sbExtDesc = new StringBuilder();
            foreach (string ext in App.Settings.SupportedFileExtVideo)
            {
                sbExtDesc.Append("*");
                sbExtDesc.Append(ext);
                sbExtDesc.Append("; ");
            }
            sbExt.Append(sbExtDesc.ToString().TrimEnd().TrimEnd(';'));
            sbExt.Append(")|");
            foreach (string ext in App.Settings.SupportedFileExtVideo)
            {
                sbExt.Append("*");
                sbExt.Append(ext);
                sbExt.Append("; ");
            }
            dlg.Filter = sbExt.ToString().TrimEnd();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadMedia(dlg.FileNames);
            }
        }

        private void OpenFolder()
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = Resources.MainWindow_OpenFolder_Browse_for_media_disc_folder___;
            dlg.SelectedPath = App.Settings.ProfileActive.DefaultMediaDirectory;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadMedia(new string[] { dlg.SelectedPath });
            }
        }

        private void LoadMedia(string[] ps)
        {
            if (1 == ps.Length)
            {
                txtTitle.Text = MediaHelper.GetMediaName(ps[0]);
                GuessSource(txtTitle.Text);
            }

            if (!App.Settings.ProfileActive.WritePublish && ps.Length > 1)
            {
                if (MessageBox.Show(Resources.MainWindow_LoadMedia_, Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    App.Settings.ProfileActive.WritePublish = true;
                }
            }

            foreach (string p in ps)
            {
                if (File.Exists(p) || Directory.Exists(p))
                {
                    DebugHelper.WriteLine(string.Format("Queued {0} to create a torrent", p));
                    lbFiles.Items.Add(p);

                    UpdateGuiControls();
                }
            }

            if (App.Settings.AnalyzeAuto)
            {
                AnalyzeMedia(ps);
            }
        }

        private void GuessSource(string source)
        {
            if (Regex.IsMatch(source, "DVD", RegexOptions.IgnoreCase))
            {
                cboSource.Text = "DVD";
            }
            else if (Regex.IsMatch(source, "HDTV", RegexOptions.IgnoreCase))
            {
                cboSource.Text = "HDTV";
            }
            else if (Regex.IsMatch(source, "Blu", RegexOptions.IgnoreCase) || Regex.IsMatch(source, "BDRip", RegexOptions.IgnoreCase))
            {
                cboSource.Text = "Blu-ray";
            }
            else if (Regex.IsMatch(source, "TV", RegexOptions.IgnoreCase))
            {
                cboSource.Text = "TV";
            }
        }

        private void AnalyzeMedia(string[] files)
        {
            List<string> fd = new List<string>();
            fd.AddRange(files);
            AnalyzeMedia(fd);
        }

        private void AnalyzeMedia(List<string> files)
        {
            if (!ValidateInput()) return;

            DialogResult dlgResult = DialogResult.OK;
            List<TaskSettings> taskSettingsList = new List<TaskSettings>();

            MediaWizardOptions mo = Adapter.GetMediaType(files);

            if (mo.ShowWizard)
            {
                ShowMediaWizard(ref mo, files);
            }

            if (mo.PromptShown)
            {
                dlgResult = mo.DialogResult;
            }
            else
            {
                // fill previous settings
                mo.CreateTorrent = App.Settings.ProfileActive.CreateTorrent;
                mo.CreateScreenshots = App.Settings.ProfileActive.CreateScreenshots;
                mo.UploadScreenshots = App.Settings.ProfileActive.UploadScreenshots;
            }

            if (!mo.PromptShown && App.Settings.ShowMediaWizardAlways)
            {
                MediaWizard mw = new MediaWizard(files);
                dlgResult = mw.ShowDialog();
                if (dlgResult == DialogResult.OK)
                {
                    mo = mw.Options;
                }
            }

            if (dlgResult == DialogResult.OK)
            {
                if (mo.MediaTypeChoice == MediaType.MediaCollection)
                {
                    TaskSettings ts = new TaskSettings();
                    ts.MediaOptions = mo;

                    files.Sort();
                    string firstPath = files[0];
                    PrepareNewMedia(ts, File.Exists(firstPath) ? Path.GetDirectoryName(files[0]) : firstPath);
                    foreach (string p in files)
                    {
                        if (File.Exists(p))
                        {
                            ts.Media.FileCollection.Add(p);
                        }
                    }
                    taskSettingsList.Add(ts);
                }
                else
                {
                    foreach (string fd in files)
                    {
                        if (File.Exists(fd) || Directory.Exists(fd))
                        {
                            TaskSettings ts = new TaskSettings();
                            ts.MediaOptions = mo;

                            PrepareNewMedia(ts, fd);
                            ts.Media.DiscType = MediaHelper.GetSourceType(fd);

                            if (ts.Media.DiscType == SourceType.Bluray)
                            {
                                ts.Media.Overall = new MediaFile(FileSystemHelper.GetLargestFilePathFromDir(fd), cboSource.Text);
                                ts.Media.Overall.Summary = BDInfo(fd);
                            }

                            if (!string.IsNullOrEmpty(txtTitle.Text))
                            {
                                ts.Media.SetTitle(txtTitle.Text);
                            }

                            taskSettingsList.Add(ts);
                        }
                    }
                }

                foreach (TaskSettings ts in taskSettingsList)
                {
                    WorkerTask task = WorkerTask.CreateTask(ts);
                    task.UploadProgressChanged += Task_UploadProgressChanged;
                    task.MediaLoaded += Task_MediaLoaded;
                    task.StatusChanged += Task_StatusChanged;
                    task.ScreenshotUploaded += Task_ScreenshotUploaded;
                    task.TorrentInfoCreated += Task_TorrentInfoCreated;
                    task.TorrentProgressChanged += Task_TorrentProgressChanged;
                    task.TaskCompleted += Task_TaskCompleted;
                    TaskManager.Start(task);
                }

                UpdateGuiControls();
                pBar.Value = 0;
            }
        }

        #region WorkerTask events

        private void Task_TorrentProgressChanged(WorkerTask task)
        {
            pBar.Value = (int)task.Info.TorrentProgress;
        }

        private void Task_UploadProgressChanged(WorkerTask task)
        {
            pBar.Value = TaskManager.GetAverageProgress();
        }

        private void Task_TaskCompleted(WorkerTask task)
        {
            if (task.Success)
            {
                foreach (MediaFile mf in task.Info.TaskSettings.Media.MediaFiles)
                {
                    lbFiles.Items.Remove(mf.FilePath);
                }

                lbPublish.SelectedIndex = lbPublish.Items.Count - 1;
                sBar.Text = Resources.MainWindow_bwApp_RunWorkerCompleted_Ready_;
            }
            else
            {
                sBar.Text = Resources.MainWindow_bwApp_RunWorkerCompleted_Ready__One_or_more_tasks_failed_;
            }

            UpdateGuiControls();
        }

        private void Task_TorrentInfoCreated(WorkerTask task)
        {
            lbPublish.Items.Add(task);

            // initialize quick publish checkboxes
            chkQuickFullPicture.Checked = App.Settings.ProfileActive.UseFullPictureURL;
            chkQuickAlignCenter.Checked = App.Settings.ProfileActive.AlignCenter;
            chkQuickPre.Checked = App.Settings.ProfileActive.PreText;
            cboQuickPublishType.SelectedIndex = (int)App.Settings.ProfileActive.Publisher;
            cboQuickTemplate.Text = App.Settings.ProfileActive.PublisherExternalTemplateName;
        }

        private void Task_ScreenshotUploaded(ScreenshotInfo si)
        {
            lbScreenshots.Items.Add(si);
            lbScreenshots.SelectedIndex = lbScreenshots.Items.Count - 1;
        }

        private void Task_MediaLoaded(WorkerTask task)
        {
            MediaInfo2 mi = task.Info.TaskSettings.Media;
            gbDVD.Enabled = (task.Info.TaskSettings.MediaOptions.MediaTypeChoice == MediaType.MediaDisc);
            foreach (MediaFile mf in mi.MediaFiles)
            {
                lbMediaInfo.Items.Add(mf);
                lbMediaInfo.SelectedIndex = lbMediaInfo.Items.Count - 1;
            }
        }

        private void Task_StatusChanged(WorkerTask task)
        {
            sBar.Text = task.Info.Status;
        }

        #endregion WorkerTask events

        private static MediaWizardOptions ShowMediaWizard(ref MediaWizardOptions mwo, List<string> FileOrDirPaths)
        {
            MediaWizard mw = new MediaWizard(FileOrDirPaths);
            mwo.DialogResult = mw.ShowDialog();
            if (mwo.DialogResult == DialogResult.OK)
            {
                mwo = mw.Options;
            }
            mwo.PromptShown = true;
            return mwo;
        }

        private string BDInfo(string p)
        {
            BDInfoSettings.AutosaveReport = true;
            BDInfo.FormMain info = new BDInfo.FormMain(new string[] { p });

            info.ShowDialog(this);

            return info.Report;
        }

        private bool ValidateInput()
        {
            StringBuilder sbMsg = new StringBuilder();

            // checks
            if (string.IsNullOrEmpty(cboSource.Text) && App.Settings.ProfileActive.Publisher != PublishInfoType.MediaInfo)
            {
                sbMsg.AppendLine("Source information is mandatory. Use the Source drop down menu to select the correct source type.");
            }

            if (sbMsg.Length > 0)
            {
                MessageBox.Show(Resources.MainWindow_ValidateInput_ + sbMsg.ToString(), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            return true;
        }

        private List<string> CreateFileList(string p)
        {
            List<string> fl = new List<string>();
            if (File.Exists(p))
            {
                fl.Add(p);
            }
            else if (Directory.Exists(p))
            {
            }
            return fl;
        }

        private void PrepareNewMedia(TaskSettings taskSettings, string p)
        {
            MediaInfo2 mi = new MediaInfo2(taskSettings.MediaOptions, p);

            mi.Extras = cboExtras.Text;
            if (cboSource.Text == "DVD")
            {
                mi.Source = Adapter.GetDVDString(p);
            }
            else
            {
                mi.Source = cboSource.Text;
            }
            mi.Menu = cboDiscMenu.Text;
            mi.Authoring = cboAuthoring.Text;
            mi.WebLink = txtWebLink.Text;

            if (App.Settings.ProfileActive.Publisher == PublishInfoType.ExternalTemplate)
            {
                mi.TemplateLocation = Path.Combine(App.TemplatesDir, App.Settings.ProfileActive.PublisherExternalTemplateName);
            }

            taskSettings.Media = mi;
        }

        private void btnCreateTorrent_Click(object sender, EventArgs e)
        {
            CreateTorrentButton();
        }

        private void SettingsWrite()
        {
            App.Settings.Save(App.SettingsFilePath);
            App.UploadersConfig.Save(App.UploadersConfigPath);
        }

        private void ConfigureTemplates()
        {
            App.WriteTemplates(false);

            // Read Templates to GUI
            if (Directory.Exists(App.TemplatesDir))
            {
                string[] dirs = Directory.GetDirectories(App.TemplatesDir);
                string[] templateNames = new string[dirs.Length];
                for (int i = 0; i < templateNames.Length; i++)
                {
                    templateNames[i] = Path.GetFileName(dirs[i]);
                }
                cboQuickTemplate.Items.Clear();
                cboQuickTemplate.Items.AddRange(templateNames);
                cboQuickTemplate.Text = App.Settings.ProfileActive.PublisherExternalTemplateName;
            }
        }

        #region Load settings

        private void LoadSettingsToControls()
        {
            LoadSettingsInputControls();
            LoadSettingsInputMediaControls();

            LoadSettingsMediaInfoControls();

            LoadSettingsScreenshotControls();

            LoadSettingsPublishControls();
            LoadSettingsPublishTemplatesControls();

            pgApp.SelectedObject = App.Settings;
        }

        private void LoadSettingsMediaInfoControls()
        {
            if (string.IsNullOrEmpty(App.Settings.CustomMediaInfoDllDir))
            {
                App.Settings.CustomMediaInfoDllDir = Application.StartupPath;
            }
            Kernel32Helper.SetDllDirectory(App.Settings.CustomMediaInfoDllDir);
        }

        private void LoadSettingsScreenshotControls()
        {
            chkUploadScreenshots.Checked = App.Settings.ProfileActive.CreateScreenshots;
            btnUploadersConfig.Visible = cboFileUploader.Visible = cboImageUploader.Visible = string.IsNullOrEmpty(App.Settings.PtpImgCode);
            chkUploadScreenshots.Text = string.IsNullOrEmpty(App.Settings.PtpImgCode) ? "Upload screenshot to:" : "Upload screenshots to ptpimg.me";

            cboImageUploader.Items.Clear();
            cboImageUploader.Items.AddRange(Helpers.GetLocalizedEnumDescriptions<ImageDestination>());
            cboImageUploader.SelectedIndex = (int)App.Settings.ProfileActive.ImageUploaderType;

            cboFileUploader.Items.Clear();
            cboFileUploader.Items.AddRange(Helpers.GetLocalizedEnumDescriptions<FileDestination>());
            cboFileUploader.SelectedIndex = (int)App.Settings.ProfileActive.ImageFileUploaderType;

            if (listBoxProfiles.Items.Count == 0)
            {
                App.Settings.Profiles.ForEach(x => listBoxProfiles.Items.Add(x));
            }
            listBoxProfiles.SelectedIndex = App.Settings.ProfileIndex;
        }

        private void LoadSettingsInputControls()
        {
            if (App.Settings.MediaSources.Count == 0)
            {
                App.Settings.MediaSources.AddRange(new string[]
                {
                    "CAM", "TC", "TS", "R5", "DVD-Screener",
                    "DVD", "TV", "HDTV", "Blu-ray", "HD-DVD",
                    "Laser Disc", "VHS", "Unknown"
                });
            }
            if (App.Settings.Extras.Count == 0)
            {
                App.Settings.Extras.AddRange(new string[] { "Intact", "Shrunk", "Removed", "None on Source" });
            }
            if (App.Settings.AuthoringModes.Count == 0)
            {
                App.Settings.AuthoringModes.AddRange(new string[] { "Untouched", "Shrunk" });
            }
            if (App.Settings.DiscMenus.Count == 0)
            {
                App.Settings.DiscMenus.AddRange(new string[] { "Intact", "Removed", "Shrunk" });
            }
            if (App.Settings.SupportedFileExtVideo.Count == 0)
            {
                App.Settings.SupportedFileExtVideo.AddRange(new string[] { ".3g2", ".3gp", ".3gp2", ".3gpp", ".amr", ".asf", ".asx", ".avi", ".d2v", ".dat", ".divx", ".drc", ".dsa", ".dsm", ".dss", ".dsv", ".flc", ".fli", ".flic", ".flv", ".hdmov", ".ivf", ".m1v", ".m2ts", ".m2v", ".m4v", ".mkv", ".mov", ".mp2v", ".mp4", ".mpcpl", ".mpe", ".mpeg", ".mpg", ".mpv", ".mpv2", ".ogm", ".qt", ".ram", ".ratdvd", ".rm", ".rmvb", ".roq", ".rp", ".rpm", ".rt", ".swf", ".ts", ".vob", ".vp6", ".wm", ".wmp", ".wmv", ".wmx", ".wvx" });
            }
            if (App.Settings.SupportedFileExtAudio.Count == 0)
            {
                App.Settings.SupportedFileExtAudio.AddRange(new string[] { ".aac", ".aiff", ".ape", ".flac", ".m4a", ".mp3", ".mpc", ".ogg", ".mp4", ".wma" });
            }

            cboSource.Items.Clear();
            foreach (string src in App.Settings.MediaSources)
            {
                cboSource.Items.Add(src);
            }

            cboAuthoring.Items.Clear();
            foreach (string ed in App.Settings.AuthoringModes)
            {
                cboAuthoring.Items.Add(ed);
            }

            cboDiscMenu.Items.Clear();
            foreach (string ex in App.Settings.DiscMenus)
            {
                cboDiscMenu.Items.Add(ex);
            }

            cboExtras.Items.Clear();
            foreach (string ex in App.Settings.Extras)
            {
                cboExtras.Items.Add(ex);
            }
        }

        private void LoadSettingsInputMediaControls()
        {
            chkAuthoring.Checked = App.Settings.bAuthoring;
            cboAuthoring.Text = App.Settings.AuthoringMode;

            chkDiscMenu.Checked = App.Settings.bDiscMenu;
            cboDiscMenu.Text = App.Settings.DiscMenu;

            chkExtras.Checked = App.Settings.bExtras;
            cboExtras.Text = App.Settings.Extra;

            chkTitle.Checked = App.Settings.bTitle;
            chkWebLink.Checked = App.Settings.bWebLink;
        }

        private void LoadSettingsPublishControls()
        {
            if (cboQuickPublishType.Items.Count == 0)
            {
                cboQuickPublishType.Items.AddRange(Enum.GetNames(typeof(PublishInfoType)));
            }
            cboQuickPublishType.SelectedIndex = (int)App.Settings.ProfileActive.Publisher;
        }

        private void LoadSettingsPublishTemplatesControls()
        {
            // Proxy
            cbProxyMethod.Items.AddRange(Helpers.GetLocalizedEnumDescriptions<ProxyMethod>());
            cbProxyMethod.SelectedIndex = (int)App.Settings.ProxySettings.ProxyMethod;
            txtProxyUsername.Text = App.Settings.ProxySettings.Username;
            txtProxyPassword.Text = App.Settings.ProxySettings.Password;
            txtProxyHost.Text = App.Settings.ProxySettings.Host ?? string.Empty;
            nudProxyPort.Value = App.Settings.ProxySettings.Port;
            UpdateProxyControls();
        }

        #endregion Load settings

        private void UpdateGuiControls()
        {
            if (IsGuiReady)
            {
                gbDVD.Enabled = gbSource.Enabled = App.Settings.ProfileActive.Publisher != PublishInfoType.MediaInfo;

                btnCreateTorrent.Enabled = !TaskManager.IsBusy && lbPublish.Items.Count > 0;
                btnAnalyze.Enabled = !TaskManager.IsBusy && lbFiles.Items.Count > 0;

                btnPublish.Enabled = !TaskManager.IsBusy && !string.IsNullOrEmpty(txtPublish.Text);
            }
        }

        private void pbScreenshot_MouseClick(object sender, MouseEventArgs e)
        {
            PictureBox screenshot = sender as PictureBox;
            if (screenshot != null) Helpers.OpenFile(screenshot.ImageLocation);
        }

        private void tmrStatus_Tick(object sender, EventArgs e)
        {
            tssPerc.Text = (TaskManager.IsBusy ? string.Format("{0}%", (100.0 * (double)pBar.Value / (double)pBar.Maximum).ToString("0")) : "");
            btnAnalyze.Text = Resources.MainWindow_tmrStatus_Tick_Create__description + (lbFiles.SelectedItems.Count > 1 ? "s" : "");
            btnCreateTorrent.Text = Resources.MainWindow_tmrStatus_Tick_Create__torrent + (lbPublish.SelectedItems.Count > 1 ? "s" : "");
            btnBrowse.Enabled = !TaskManager.IsBusy;
            btnBrowseDir.Enabled = !TaskManager.IsBusy;
            btnAnalyze.Enabled = !TaskManager.IsBusy && lbFiles.Items.Count > 0;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void CopyPublish()
        {
            if (!string.IsNullOrEmpty(txtPublish.Text))
            {
                Clipboard.SetText(txtPublish.Text);
            }
        }

        private void btnPublish_Click(object sender, EventArgs e)
        {
            CopyPublish();
        }

        private void chkScreenshotUpload_CheckedChanged(object sender, EventArgs e)
        {
            App.Settings.ProfileActive.UploadScreenshots = chkUploadScreenshots.Checked;
        }

        private void btnAnalyze_Click(object sender, EventArgs e)
        {
            string[] files = new string[lbFiles.Items.Count];
            for (int i = 0; i < lbFiles.Items.Count; i++)
            {
                files[i] = lbFiles.Items[i].ToString();
            }
            AnalyzeMedia(files);
        }

        private void ShowAboutWindow()
        {
            AboutBox ab = new AboutBox();
            ab.ShowDialog();
        }

        private void CreateTorrentButton()
        {
            foreach (WorkerTask task in lbPublish.SelectedItems)
            {
                task.CreateTorrent();

                if (App.Settings.ProfileActive.XMLTorrentUploadCreate)
                {
                    string fp = Path.Combine(task.Info.TaskSettings.TorrentFolder, MediaHelper.GetMediaName(task.Info.TaskSettings.Media.Location)) + ".xml";
                    FileSystem.GetXMLTorrentUpload(task.Info.TaskSettings).Write(fp);
                }
            }
        }

        private WorkerTask GetTask()
        {
            WorkerTask task = null;
            if (lbPublish.SelectedIndex > -1)
            {
                task = lbPublish.Items[lbPublish.SelectedIndex] as WorkerTask;
            }
            return task;
        }

        private void CreatePublishUser()
        {
            if (!TaskManager.IsBusy)
            {
                WorkerTask task = GetTask();
                if (task != null)
                {
                    var pop = new PublishOptions
                    {
                        AlignCenter = chkQuickAlignCenter.Checked,
                        FullPicture = chkQuickFullPicture.Checked,
                        PreformattedText = chkQuickPre.Checked,
                        PublishInfoTypeChoice = (PublishInfoType)cboQuickPublishType.SelectedIndex,
                        TemplateLocation = Path.Combine(App.TemplatesDir, cboQuickTemplate.Text)
                    };

                    txtPublish.Text = Adapter.ToPublishString(task.Info.TaskSettings, pop);

                    if (task.Info.TaskSettings.MediaOptions.MediaTypeChoice == MediaType.MusicAudioAlbum)
                    {
                        txtPublish.BackColor = Color.Black;
                        txtPublish.ForeColor = Color.White;
                    }
                    else
                    {
                        txtPublish.BackColor = SystemColors.Window;
                        txtPublish.ForeColor = SystemColors.WindowText;
                    }
                }
            }
        }

        private void chkQuickPre_CheckedChanged(object sender, EventArgs e)
        {
            CreatePublishUser();
        }

        private void chkQuickAlignCenter_CheckedChanged(object sender, EventArgs e)
        {
            CreatePublishUser();
        }

        private void chkQuickFullPicture_CheckedChanged(object sender, EventArgs e)
        {
            CreatePublishUser();
        }

        private void txtPublish_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(1))
            {
                TextBox tb = (TextBox)sender;
                tb.SelectAll();
                e.Handled = true;
            }
        }

        private void cboScreenshotDest_SelectedIndexChanged(object sender, EventArgs e)
        {
            App.Settings.ProfileActive.ImageUploaderType = (ImageDestination)cboImageUploader.SelectedIndex;
            cboFileUploader.Enabled = App.Settings.ProfileActive.ImageUploaderType == ImageDestination.FileUploader;
        }

        private void btnTemplatesRewrite_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Resources.MainWindow_btnTemplatesRewrite_Click_, Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                App.WriteTemplates(true);
            }
        }

        private void OpenVersionHistory()
        {
            URLHelpers.OpenURL("https://github.com/McoreD/TDMaker/wiki/Changelog");
        }

        private void cboQuickTemplate_SelectedIndexChanged(object sender, EventArgs e)
        {
            CreatePublishUser();
        }

        private void WriteMediaInfo(string info)
        {
            if (GetTask() != null)
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = Resources.MainWindow_WriteMediaInfo_Text_Files____txt____txt;
                dlg.FileName = GetTask().Info.TaskSettings.Media.Title;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter sw = new StreamWriter(dlg.FileName))
                    {
                        sw.WriteLine(info);
                    }
                }
            }
        }

        private void miFileSaveInfoAs_Click(object sender, EventArgs e)
        {
            string info = "";
            if (tcMain.SelectedTab == tpMediaInfo)
            {
                info = txtMediaInfo.Text;
            }
            else
            {
                info = txtPublish.Text;
            }
            WriteMediaInfo(info);
        }

        private void miFileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tcMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tcMain.SelectedTab == tpMediaInfo)
            {
                miFileSaveInfoAs.Text = Resources.MainWindow_tcMain_SelectedIndexChanged__Save_Media_Info_As___;
            }
            else
            {
                miFileSaveInfoAs.Text = Resources.MainWindow_tcMain_SelectedIndexChanged__Save_Publish_Info_As___;
            }
        }

        private void miFileSaveTorrent_Click(object sender, EventArgs e)
        {
            CreateTorrentButton();
        }

        private void tsmiAbout_Click(object sender, EventArgs e)
        {
            ShowAboutWindow();
        }

        private void MainWindow_Resize(object sender, EventArgs e)
        {
            this.Refresh();
        }

        private void miFileOpenFile_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void miFileOpenFolder_Click(object sender, EventArgs e)
        {
            OpenFolder();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyPublish();
        }

        private void miFoldersScreenshots_Click(object sender, EventArgs e)
        {
            FileSystem.OpenDirScreenshots();
        }

        private void miFoldersTorrents_Click(object sender, EventArgs e)
        {
            FileSystem.OpenDirTorrents();
        }

        private void miFoldersLogs_Click(object sender, EventArgs e)
        {
            FileSystem.OpenDirLogs();
        }

        private void miFoldersLogsDebug_Click(object sender, EventArgs e)
        {
            FileSystem.OpenFileDebug();
        }

        private void miFoldersSettings_Click(object sender, EventArgs e)
        {
            FileSystem.OpenDirSettings();
        }

        private void miFoldersTemplates_Click(object sender, EventArgs e)
        {
            FileSystem.OpenDirTemplates();
        }

        private void miHelpVersionHistory_Click(object sender, EventArgs e)
        {
            OpenVersionHistory();
        }

        private void pgApp_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (IsGuiReady)
            {
                LoadSettingsToControls();
                ValidateThumbnailerPaths(s, e);

                if (File.Exists(App.Settings.CustomUploadersConfigPath))
                {
                    App.UploadersConfig = UploadersConfig.Load(App.Settings.CustomUploadersConfigPath);
                }
            }
        }

        private void cboAuthoring_SelectedIndexChanged(object sender, EventArgs e)
        {
            App.Settings.AuthoringMode = cboAuthoring.Text;
        }

        private void chkSourceEdit_CheckedChanged(object sender, EventArgs e)
        {
            App.Settings.bAuthoring = chkAuthoring.Checked;
        }

        private void cboExtras_SelectedIndexChanged(object sender, EventArgs e)
        {
            App.Settings.Extra = cboExtras.Text;
        }

        private void chkExtras_CheckedChanged(object sender, EventArgs e)
        {
            App.Settings.bExtras = chkExtras.Checked;
        }

        private void cboDiscMenu_SelectedIndexChanged(object sender, EventArgs e)
        {
            App.Settings.DiscMenu = cboDiscMenu.Text;
        }

        private void chkDiscMenu_CheckedChanged(object sender, EventArgs e)
        {
            App.Settings.bDiscMenu = chkDiscMenu.Checked;
        }

        private void chkSource_CheckedChanged(object sender, EventArgs e)
        {
            chkSource.CheckState = CheckState.Indeterminate;
        }

        private void chkTitle_CheckedChanged(object sender, EventArgs e)
        {
            App.Settings.bTitle = chkTitle.Checked;
        }

        private void txtTitle_TextChanged(object sender, EventArgs e)
        {
            // we dont save this
        }

        private void chkWebLink_CheckedChanged(object sender, EventArgs e)
        {
            App.Settings.bWebLink = chkWebLink.Checked;
        }

        private void txtWebLink_TextChanged(object sender, EventArgs e)
        {
            // we dont save this
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            SettingsWrite();
        }

        private void lbScreenshots_SelectedIndexChanged(object sender, EventArgs e)
        {
            int sel = lbScreenshots.SelectedIndex;
            if (sel > -1)
            {
                ScreenshotInfo ss = lbScreenshots.Items[sel] as ScreenshotInfo;
                pbScreenshot.Tag = ss;
                if (ss != null && File.Exists(ss.LocalPath))
                {
                    pbScreenshot.LoadImageFromFileAsync(ss.LocalPath);
                    pgScreenshot.SelectedObject = ss;
                }
                else if (!string.IsNullOrEmpty(ss.FullImageLink))
                {
                    pbScreenshot.LoadImageFromURLAsync(ss.FullImageLink);
                }
                else
                {
                    pbScreenshot.LoadImage(new Bitmap(300, 300));
                    pgScreenshot.SelectedObject = null;
                }
            }
        }

        private void pbScreenshot_MouseDown(object sender, MouseEventArgs e)
        {
            ScreenshotInfo ss = pbScreenshot.Tag as ScreenshotInfo;
            if (ss != null) // fixed issue #10
            {
                if (File.Exists(ss.LocalPath))
                {
                    Helpers.OpenFile(ss.LocalPath);
                }
                else if (!string.IsNullOrEmpty(ss.FullImageLink))
                {
                    URLHelpers.OpenURL(ss.FullImageLink);
                }
            }
        }

        private void LbMediaInfoSelectedIndexChanged(object sender, EventArgs e)
        {
            OnLbMediaInfoSelectedIndexChanged();
        }

        private void OnLbMediaInfoSelectedIndexChanged()
        {
            if (lbMediaInfo.SelectedIndex > -1)
            {
                MediaFile mediaFile = lbMediaInfo.Items[lbMediaInfo.SelectedIndex] as MediaFile;

                if (mediaFile != null)
                {
                    if (!chkMediaInfoComplete.Checked)
                    {
                        txtMediaInfo.Text = mediaFile.Summary;
                    }
                    else
                    {
                        txtMediaInfo.Text = mediaFile.SummaryComplete;
                    }
                }
            }
        }

        private void LbPublishSelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbPublish.SelectedIndex > -1)
            {
                CreatePublishUser();
            }
        }

        private void btnBrowseDir_Click(object sender, EventArgs e)
        {
            OpenFolder();
        }

        private void cboSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            // we do nothing
        }

        private void cboPublishType_SelectedIndexChanged(object sender, EventArgs e)
        {
            CreatePublishUser();
            cboQuickTemplate.Enabled = (PublishInfoType)cboQuickPublishType.SelectedIndex == PublishInfoType.ExternalTemplate;
        }

        private void lbFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                List<string> temp = lbFiles.SelectedItems.Cast<string>().ToList();
                foreach (string fd in temp)
                {
                    lbFiles.Items.Remove(fd);
                }
            }
        }

        private void btnUploadersConfig_Click(object sender, EventArgs e)
        {
            bool firstInstance;
            UploadersConfigForm form = UploadersConfigForm.GetFormInstance(App.UploadersConfig, out firstInstance);
            form.ShowDialog();
        }

        private void chkMediaInfoComplete_CheckedChanged(object sender, EventArgs e)
        {
            OnLbMediaInfoSelectedIndexChanged();
        }

        private void cboImageFileUploader_SelectedIndexChanged(object sender, EventArgs e)
        {
            App.Settings.ProfileActive.ImageFileUploaderType = (FileDestination)cboFileUploader.SelectedIndex;
        }

        private void btnDownloadFFmpeg_Click(object sender, EventArgs e)
        {
            FFmpegDownloader.DownloadFFmpeg(true, DownloaderForm_InstallRequested);
        }

        private void DownloaderForm_InstallRequested(string filePath)
        {
            string extractPath = Path.Combine(App.ToolsDir, "ffmpeg.exe");
            bool result = FFmpegDownloader.ExtractFFmpeg(filePath, extractPath);

            if (result)
            {
                this.InvokeSafe(() =>
                {
                    App.Settings.FFmpegPath = extractPath;
                });

                MessageBox.Show(Resources.MainWindow_DownloaderForm_InstallRequested_Successfully_downloaded_FFmpeg_, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(Resources.MainWindow_DownloaderForm_InstallRequested_Failed_to_download_FFmpeg_, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtProxyHost_TextChanged(object sender, EventArgs e)
        {
            App.Settings.ProxySettings.Host = txtProxyHost.Text;
        }

        private void cbProxyMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            App.Settings.ProxySettings.ProxyMethod = (ProxyMethod)cbProxyMethod.SelectedIndex;

            if (App.Settings.ProxySettings.ProxyMethod == ProxyMethod.Automatic)
            {
                App.Settings.ProxySettings.IsValidProxy();
                txtProxyHost.Text = App.Settings.ProxySettings.Host ?? string.Empty;
                nudProxyPort.Value = App.Settings.ProxySettings.Port;
            }

            UpdateProxyControls();
        }

        private void UpdateProxyControls()
        {
            switch (App.Settings.ProxySettings.ProxyMethod)
            {
                case ProxyMethod.None:
                    txtProxyUsername.Enabled = txtProxyPassword.Enabled = txtProxyHost.Enabled = nudProxyPort.Enabled = false;
                    break;
                case ProxyMethod.Manual:
                    txtProxyUsername.Enabled = txtProxyPassword.Enabled = txtProxyHost.Enabled = nudProxyPort.Enabled = true;
                    break;
                case ProxyMethod.Automatic:
                    txtProxyUsername.Enabled = txtProxyPassword.Enabled = true;
                    txtProxyHost.Enabled = nudProxyPort.Enabled = false;
                    break;
            }
        }

        private void nudProxyPort_ValueChanged(object sender, EventArgs e)
        {
            App.Settings.ProxySettings.Port = (int)nudProxyPort.Value;
        }

        private void txtProxyPassword_TextChanged(object sender, EventArgs e)
        {
            App.Settings.ProxySettings.Password = txtProxyPassword.Text;
        }

        private void txtProxyUsername_TextChanged(object sender, EventArgs e)
        {
            App.Settings.ProxySettings.Username = txtProxyUsername.Text;
        }

        private void btnAddScreenshotProfile_Click(object sender, EventArgs e)
        {
            ProfileOptions profile = new ProfileOptions() { Name = "New Profile" };
            listBoxProfiles.Items.Add(profile);
            App.Settings.Profiles.Add(profile);
            listBoxProfiles.SelectedIndex = (listBoxProfiles.Items.Count - 1);
        }

        private void btnRemoveScreenshotProfile_Click(object sender, EventArgs e)
        {
            int sel = listBoxProfiles.SelectedIndex;
            if (listBoxProfiles.SelectedIndex > 0 && App.Settings.Profiles.Count > listBoxProfiles.SelectedIndex)
            {
                ProfileOptions profile = App.Settings.Profiles[listBoxProfiles.SelectedIndex];
                App.Settings.Profiles.Remove(profile);
                listBoxProfiles.Items.Remove(profile);
                listBoxProfiles.SelectedIndex = Math.Min(sel, listBoxProfiles.Items.Count - 1);
            }
        }

        private void listBoxProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            App.Settings.ProfileIndex = listBoxProfiles.SelectedIndex;
            LoadProfileControls();
            UpdateGuiControls();
        }

        private void LoadProfileControls()
        {
            Text = string.Format("{0} - {1}", App.GetProductName(), App.Settings.ProfileActive.Name);
            pgProfileOptions.SelectedObject = App.Settings.ProfileActive;

            if (IsGuiReady)
            {
                chkUploadScreenshots.Checked = App.Settings.ProfileActive.UploadScreenshots;
                cboImageUploader.SelectedIndex = (int)App.Settings.ProfileActive.ImageUploaderType;
                cboFileUploader.SelectedIndex = (int)App.Settings.ProfileActive.ImageFileUploaderType;
            }
        }

        private void pgProfileOptions_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            UpdateGuiControls();
        }
    }
}