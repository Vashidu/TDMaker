﻿using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using TDMakerLib;

namespace TDMaker
{
    public partial class MediaWizard : Form
    {
        public MediaWizardOptions Options = new MediaWizardOptions();

        private MediaWizard()
        {
            InitializeComponent();
        }

        public MediaWizard(List<string> FileOrDirPaths)
            : this()
        {
            PrepareUserActionMsg(FileOrDirPaths);
        }

        private void PrepareUserActionMsg(List<string> list_fd)
        {
            if (list_fd.Count == 1 && File.Exists(list_fd[0]))
            {
                lblUserActionMsg.Text = "You are about to analyze a single file...";
                this.Options.MediaTypeChoice = MediaType.MediaIndiv;
                rbFilesAsIndiv.Checked = true;
                gbQuestion.Enabled = false;
            }
            else if (list_fd.Count == 1 && Directory.Exists(list_fd[0]))
            {
                SourceType src = MediaHelper.GetSourceType(list_fd[0]);
                switch (src)
                {
                    case SourceType.Bluray:
                        lblUserActionMsg.Text = "You are about to analyze a Blu-ray Disc...";
                        this.Options.MediaTypeChoice = MediaType.MediaDisc;
                        break;

                    case SourceType.DVD:
                        lblUserActionMsg.Text = "You are about to analyze a DVD...";
                        this.Options.MediaTypeChoice = MediaType.MediaDisc;
                        break;

                    default:
                        lblUserActionMsg.Text = "You are about to analyze a directory...";
                        this.Options.MediaTypeChoice = MediaType.MediaCollection;
                        break;
                }
            }
            else
            {
                this.Options.MediaTypeChoice = MediaType.MediaCollection;

                bool bDirFound = false;
                bool bFileFound = false;
                int dirCount = 0;
                int filesCount = 0;

                foreach (string fd in list_fd)
                {
                    if (Directory.Exists(fd))
                    {
                        dirCount++;
                        bDirFound = true;
                    }
                    else if (File.Exists(fd))
                    {
                        filesCount++;
                        bFileFound = true;
                    }
                    if (dirCount > 1) break;
                }
                if (bDirFound && !bFileFound)
                {
                    if (dirCount > 1)
                    {
                        lblUserActionMsg.Text = "You are about to analyze a collection of directories...";
                    }
                }
                else // no dir found
                {
                    lblUserActionMsg.Text = "You are about to analyze a collection of files...";
                }
            }

            chkCreateTorrent.Checked = App.Settings.ProfileActive.CreateTorrent;
            chkScreenshotsCreate.Checked = App.Settings.ProfileActive.CreateScreenshots;
            chkScreenshotsUpload.Checked = App.Settings.ProfileActive.UploadScreenshots;
            gbQuestion.Enabled = this.Options.MediaTypeChoice != MediaType.MediaDisc;
        }

        private void rbFilesAsIndiv_CheckedChanged(object sender, System.EventArgs e)
        {
            this.Options.MediaTypeChoice = MediaType.MediaIndiv;
        }

        private void btnOK_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void rbFilesAsColl_CheckedChanged(object sender, System.EventArgs e)
        {
            this.Options.MediaTypeChoice = MediaType.MediaCollection;
        }

        private void chkScreenshotsInclude_CheckedChanged(object sender, System.EventArgs e)
        {
            this.Options.CreateScreenshots = chkScreenshotsCreate.Checked;
        }

        private void chkCreateTorrent_CheckedChanged(object sender, System.EventArgs e)
        {
            this.Options.CreateTorrent = chkCreateTorrent.Checked;
        }

        private void btnCancel_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void MediaWizard_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Options.DialogResult = this.DialogResult;
        }

        private void chkScreenshotsUpload_CheckedChanged(object sender, System.EventArgs e)
        {
            this.Options.UploadScreenshots = chkScreenshotsUpload.Checked;
        }
    }
}