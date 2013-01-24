using System;
using System.Collections.Generic;
using System.IO;
using ms.video.downloader.android.service.download;

namespace ms.video.downloader.android.service
{
    public class Settings
    {
        private readonly ApplicationConfiguration _configuration;

        public string CompanyFolder { get; private set; }
        public string AppFolder { get; private set; }
        public string AppVersionFolder { get; private set; }
        public string Version { get; private set; }
        public Guid Guid { get { return _configuration.Guid; } }
        public bool FirstTime { get; private set; }

        public Settings()
        {
            FirstTime = false;
            Version = "0.0.0.1";
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            CompanyFolder = path + @"\ms";
            if (!Directory.Exists(CompanyFolder)) Directory.CreateDirectory(CompanyFolder);
            AppFolder = CompanyFolder + @"\ms.video.downloader";
            if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
            AppVersionFolder = AppFolder + @"\" + Version;
            if (!Directory.Exists(AppVersionFolder)) Directory.CreateDirectory(AppVersionFolder);
        }

        #region Load / Save DownloadLists 

        public void SaveDownloadLists(DownloadLists lists)
        {
        }

        public bool FillDownloadLists(DownloadLists lists)
        {
            return false;
        }

        private class DownloadEntry
        {
            public DownloadEntry()
            {
                Url = "";
                ThumbnailUrl = "";
                Title = "";
                ExecutionStatus = ExecutionStatus.Normal;
                List = new List<DownloadEntry>();
            }

            public MediaType MediaType { get; set; }
            public string Url { get; set; }
            public string ThumbnailUrl { get; set; }
            public string Title { get; set; }
            public ExecutionStatus ExecutionStatus { get; set; }
            public List<DownloadEntry> List { get; private set; }
        }

        #endregion
    }

}
