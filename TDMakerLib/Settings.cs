﻿using HelpersLib;
using HelpersLib.UITypeEditors;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Design;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Xml.Serialization;
using UploadersLib;

namespace TDMakerLib
{
    public class Settings : SettingsBase<Settings>
    {
        public Settings()
        {
            this.ApplyDefaultPropertyValues();
            AuthoringModes = new StringCollection();
            DiscMenus = new StringCollection();
            Extras = new StringCollection();
            MediaSources = new StringCollection();
            SupportedFileExtAudio = new StringCollection();
            SupportedFileExtVideo = new StringCollection();
        }

        #region DVD Properties

        [BrowsableAttribute(false)]
        public bool bAuthoring { get; set; }
        public string AuthoringMode = "Untouched";

        [BrowsableAttribute(false)]
        public bool bDiscMenu { get; set; }
        public string DiscMenu = "Intact";

        [BrowsableAttribute(false)]
        public bool bExtras { get; set; }
        public string Extra = "Intact";

        [BrowsableAttribute(false), DefaultValue(true)]
        public bool bTitle { get; set; }

        [BrowsableAttribute(false)]
        public bool bWebLink { get; set; }

        #endregion DVD Properties

        #region Input

        [Category(ComponentModelStrings.Input), DefaultValue(true), Description("Show Media Wizard always; otherwise it will only be shown when you import multiple files")]
        public bool ShowMediaWizardAlways { get; set; }

        [Category(ComponentModelStrings.Input), DefaultValue(true), Description("Process media immediately after loading file or folder")]
        public bool AnalyzeAuto { get; set; }

        [Category(ComponentModelStrings.InputMedia), Editor(ComponentModelStrings.UITypeEditor, typeof(System.Drawing.Design.UITypeEditor))]
        public StringCollection AuthoringModes { get; set; }

        [Category(ComponentModelStrings.InputMedia), Editor(ComponentModelStrings.UITypeEditor, typeof(System.Drawing.Design.UITypeEditor))]
        public StringCollection DiscMenus { get; set; }

        [Category(ComponentModelStrings.InputMedia), Editor(ComponentModelStrings.UITypeEditor, typeof(System.Drawing.Design.UITypeEditor))]
        public StringCollection Extras { get; set; }

        [Category(ComponentModelStrings.InputMedia), Editor(ComponentModelStrings.UITypeEditor, typeof(System.Drawing.Design.UITypeEditor))]
        public StringCollection MediaSources { get; set; }

        [Category(ComponentModelStrings.InputMedia), Editor(ComponentModelStrings.UITypeEditor, typeof(System.Drawing.Design.UITypeEditor)),
        Description("Supported file types by MediaInfo and MTN. Add more file types only if you are absolutely sure both MediaInfo and MTN can handle those.")]
        public StringCollection SupportedFileExtVideo { get; set; }

        [Category(ComponentModelStrings.InputMedia), Editor(ComponentModelStrings.UITypeEditor, typeof(System.Drawing.Design.UITypeEditor)),
        Description("Supported file types by TDMaker to create a Music Album NFO file. Add more file types only if you are absolutely sure both MediaInfo and MTN can handle those.")]
        public StringCollection SupportedFileExtAudio { get; set; }

        [Category(ComponentModelStrings.Input), DefaultValue(true), Description("Write debug information into a log file.")]
        public bool WriteDebugFile { get; set; }

        #endregion Input

        #region Screenshots

        [Category(ComponentModelStrings.Screenshots), DefaultValue(true), Description("Create screenshots using thumbnailer")]
        public bool CreateScreenshots { get; set; }

        [Category(ComponentModelStrings.Screenshots), DefaultValue(true), Description("Upload screenshots")]
        public bool UploadScreenshots { get; set; }

        [Category(ComponentModelStrings.Screenshots), DefaultValue(true), Description("Use full image URL in the torrent description.")]
        public bool UseFullPicture { get; set; }

        [Category(ComponentModelStrings.Screenshots), DefaultValue(LocationType.KnownFolder), Description("Create screenshots in the same folders as the media file, default torrent folder or in a custom folder")]
        public LocationType ScreenshotsLocation { get; set; }

        [Category(ComponentModelStrings.Screenshots), DefaultValue(true), Description("Keep or delete screenshots after processing files")]
        public bool KeepScreenshots { get; set; }

        #endregion Screenshots

        #region Screenshots / Thumbnailers

        [Category(ComponentModelStrings.Thumbnailers), DefaultValue(ThumbnailerType.FFmpeg), Description("Chooser thumbnailer application to take screenshots.")]
        public ThumbnailerType ThumbnailerType { get; set; }

        [EditorAttribute(typeof(ExeFileNameEditor), typeof(UITypeEditor))]
        [Category(ComponentModelStrings.ThumbnailersFFmpeg), Description("FFmpeg path")]
        public string FFmpegPath { get; set; }

        [EditorAttribute(typeof(ExeFileNameEditor), typeof(UITypeEditor))]
        [Category(ComponentModelStrings.ThumbnailersMPlayer), Description("MPlayer path")]
        public string MPlayerPath { get; set; }

        public ThumbnailerOptions ThumbnailerOptions = new ThumbnailerOptions();

        #endregion Screenshots / Thumbnailers

        #region Screenshots / Uploaders

        public ImageDestination ImageUploaderType = ImageDestination.Imgur;
        public FileDestination ImageFileUploaderType = FileDestination.Pomf;
        [Category(ComponentModelStrings.ScreenshotsImageUploaders), DefaultValue(""), Description("PtpImg registration code")]
        public string PtpImgCode { get; set; }

        [Category(ComponentModelStrings.ScreenshotsImageUploaders), DefaultValue(""), Description("ImageShack registration code")]
        public string ImageShackRegCode { get; set; }

        [Category(ComponentModelStrings.ScreenshotsImageUploaders), DefaultValue(false), Description("Use ImageShack registration code")]
        public bool UseImageShackRegCode { get; set; }

        [Category(ComponentModelStrings.ScreenshotsImageUploaders), DefaultValue(5), Description("Buffer size power")]
        public double BufferSizePower { get; set; }

        #endregion Screenshots / Uploaders

        #region Publish

        [Category(ComponentModelStrings.Publish), DefaultValue(false), Description("Setting true will center align the description")]
        public bool AlignCenter { get; set; }

        [Category(ComponentModelStrings.Publish), DefaultValue(false), Description("Setting true will retain the formatting on some message boards")]
        public bool PreText { get; set; }

        [Category(ComponentModelStrings.Publish), DefaultValue(false), Description("Write the torrent description to file")]
        public bool WritePublish { get; set; }

        [Category(ComponentModelStrings.Publish), DefaultValue(true), Description("Have larger text when [pre] tag is set")]
        public bool LargerPreText { get; set; }

        [Category(ComponentModelStrings.Publish), DefaultValue(PublishInfoType.MediaInfo), Description("Use internal template, external templates or information in MediaInfo in the torrent description in Publish tab")]
        public PublishInfoType PublishInfoTypeChoice { get; set; }

        [Category(ComponentModelStrings.PublishFontSizes), DefaultValue(5), Description("Font Size for Heading 1")]
        public int FontSizeHeading1 { get; set; }

        [Category(ComponentModelStrings.PublishFontSizes), DefaultValue(4), Description("Font Size for Heading 2")]
        public int FontSizeHeading2 { get; set; }

        [Category(ComponentModelStrings.PublishFontSizes), DefaultValue(3), Description("Font Size for Heading 3")]
        public int FontSizeHeading3 { get; set; }

        [Category(ComponentModelStrings.PublishFontSizes), DefaultValue(2), Description("Font Size for Body")]
        public int FontSizeBody { get; set; }

        [Category(ComponentModelStrings.PublishFontSizes), DefaultValue(1), Description("Font Size increment")]
        public int FontSizeIncr { get; set; }

        [Browsable(false)]
        public int TemplateIndex { get; set; }

        #endregion Publish

        #region Torrent creator

        [Browsable(false)]
        public int TrackerGroupActive { get; set; }

        [Category(ComponentModelStrings.TorrentCreator), DefaultValue(false), Description("Create Torrent")]
        public bool TorrentCreateAuto { get; set; }

        [Category(ComponentModelStrings.TorrentCreator), DefaultValue(LocationType.KnownFolder), Description("Create torrents in the same folders as the media file, default torrent folder or in a custom folder")]
        public LocationType TorrentLocationChoice { get; set; }

        [Category(ComponentModelStrings.TorrentCreator), DefaultValue(false), Description("Save torrent files in sub-folders organized by tracker namer")]
        public bool TorrentsOrganize { get; set; }

        public List<TrackerGroup> TrackerGroups = new List<TrackerGroup>();

        [Category(ComponentModelStrings.TorrentCreator), DefaultValue(false), Description("Create XML Torrent Upload file")]
        public bool XMLTorrentUploadCreate { get; set; }

        #endregion Torrent creator

        #region Paths

        [Category(ComponentModelStrings.Paths), Description("Browse to reconfigure the MediaInfo.dll folder path")]
        [EditorAttribute(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string CustomMediaInfoDllDir { get; set; }

        [Category(ComponentModelStrings.Paths), DefaultValue(false), Description("Use custom Templates directory")]
        public bool UseCustomTemplatesDir { get; set; }

        [Category(ComponentModelStrings.Paths), Description("Browse to reconfigure UploadersConfig file path")]
        [EditorAttribute(typeof(JsonFileNameEditor), typeof(UITypeEditor))]
        public string CustomUploadersConfigPath { get; set; }

        [Category(ComponentModelStrings.Paths), Description("Browse to reconfigure the Templates folder path")]
        [EditorAttribute(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string CustomTemplatesDir { get; set; }

        [Category(ComponentModelStrings.Paths), DefaultValue(false), Description("Use custom Torrents directory")]
        public bool UseCustomTorrentsDir { get; set; }

        [Category(ComponentModelStrings.Paths), Description("Browse to change where torrent files are saved")]
        [EditorAttribute(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string CustomTorrentsDir { get; set; }

        [Category(ComponentModelStrings.Paths), Description("Browse to change where screenshots are saved")]
        [EditorAttribute(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string CustomScreenshotsDir { get; set; }

        #endregion Paths

        #region Proxy

        public ProxyInfo ProxySettings = new ProxyInfo();

        #endregion Proxy
    }
}