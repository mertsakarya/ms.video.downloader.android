using System;
using System.Collections.Generic;
using System.Threading;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Android.App;
using Android.Content;
using Android.Widget;
using ms.video.downloader.android.service;
using ms.video.downloader.android.service.download;

namespace ms.video.downloader.android
{
    internal class DownloadBroadcastReceiver : BroadcastReceiver
    {
        private readonly YoutubeWebViewClient _webViewClient;

        public DownloadBroadcastReceiver(YoutubeWebViewClient webViewClient)
        {
            _webViewClient = webViewClient;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            var action = intent.Action;
            if (DownloadManager.ActionDownloadComplete.Equals(action)) {
                var downloadId = intent.GetLongExtra(DownloadManager.ExtraDownloadId, 0);
                var query = new DownloadManager.Query();
                query.SetFilterById(_webViewClient.Enqueue);
                var c = _webViewClient.Dm.InvokeQuery(query);
                if (c.MoveToFirst()) {
                    var columnIndex = c.GetColumnIndex(DownloadManager.ColumnStatus);
                    if ((int) Android.App.DownloadStatus.Successful == c.GetInt(columnIndex)) {
                        var view = (ImageView) _webViewClient.Controls.Activity.FindViewById(12);
                        var uriString = c.GetString(c.GetColumnIndex(DownloadManager.ColumnLocalUri));
                        view.SetImageURI(Android.Net.Uri.Parse(uriString));
                    }
                    var list = new List<string>();
                    var columnNames = c.GetColumnNames();
                    for (var i = 0; i < c.ColumnCount; i++) {
                        list.Add(columnNames[i]);
                    }
                    columnIndex = c.GetColumnIndex(DownloadManager.ColumnReason);
                    var s = c.GetString(columnIndex);
                    _webViewClient.Controls.Title.Text = s;
                }
            }
        }
    }

    public class YoutubeWebViewClient : WebViewClient
    {
        private readonly DownloadLists _lists;
        private readonly Settings _settings;
        private YoutubeUrl _youtubeUrl;
        public readonly ViewControls Controls;

        public long Enqueue;
        public DownloadManager Dm;

        public YoutubeWebViewClient(ViewControls controls)
        {
            Controls = controls; 
            Controls.DownloadButton.Enabled = false;
            Controls.DownloadButton.Click += DownloadButtonOnClick;
            Controls.DownloadFrame.Click += DownloadFrame_Click;
            _settings = new Settings();
            _lists = new DownloadLists(_settings, OnDownloadStatusChange, null); //, OnDownloadAvailable);
            var receiver = new DownloadBroadcastReceiver(this);
            Controls.Activity.RegisterReceiver(receiver, new IntentFilter(DownloadManager.ActionDownloadComplete));
        }

        void DownloadFrame_Click(object sender, EventArgs e)
        {
            var i = new Intent();
            i.SetAction(DownloadManager.ActionViewDownloads);
            Controls.Activity.StartActivity(i);
        }

        private void OnDownloadAvailable(YoutubeEntry feed, VideoInfo videoInfo, MediaType mediaType)
        {
            Controls.Title.Text = videoInfo.Title;
            Dm = (DownloadManager) Controls.Activity.BaseContext.GetSystemService(Context.DownloadService);
            var fileName = DownloadHelper.GetLegalPath(videoInfo.Title) + videoInfo.VideoExtension;
            var request =
                new DownloadManager.Request(Android.Net.Uri.Parse(videoInfo.DownloadUri.ToString()));
            request.SetAllowedNetworkTypes(DownloadNetwork.Wifi | DownloadNetwork.Mobile)
               .AddRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.97 Safari/537.11")
               .SetShowRunningNotification(true)
               .SetAllowedOverRoaming(true)
               .SetTitle(videoInfo.Title)
               .SetDescription(fileName)
               .SetDestinationInExternalPublicDir(Android.OS.Environment.DirectoryDownloads, fileName);
            Enqueue = Dm.Enqueue(request);
        }

        public override bool ShouldOverrideUrlLoading(WebView view, string url)
        {
            view.LoadUrl(url);
            return true;
        }

        public override void OnLoadResource(WebView view, string url)
        {
            base.OnLoadResource(view, url);
            if(url.Contains("&v="))
                try {
                    _youtubeUrl = YoutubeUrl.Create(new Uri(url));
                    if (!String.IsNullOrEmpty(_youtubeUrl.VideoId)) {
                        Controls.DownloadButton.Enabled = true;
                    } else {
                        Controls.DownloadButton.Enabled = false;
                    }
                }
                catch {}
        }
        private void OnDownloadStatusChange(Feed list, Feed feed, DownloadState downloadState, double percentage)
        {
            try { Controls.Activity.RunOnUiThread(() => UpdateStatus(list, feed, downloadState, percentage)); } catch { }
        }

        private void UpdateStatus(Feed list, Feed feed, DownloadState downloadState, double percentage)
        {
            switch (downloadState) {
                case DownloadState.AllStart:
                    Controls.ProgressBar.Progress = 0;
                    Controls.DownloadFrame.Visibility = ViewStates.Visible;
                    break;
                case DownloadState.AllFinished:
                    Log.Debug("STATUS", "DONE!");
                    Controls.ProgressBar.Progress = 0;
                    Controls.DownloadFrame.Visibility = ViewStates.Gone;
                    list.Entries.Clear();
                    return;
                case DownloadState.DownloadProgressChanged:
                    Controls.ProgressBar.Progress = (int)percentage;
                    break;
                case DownloadState.TitleChanged:
                    //MixpanelTrack("Download", new { feed.Title, _settings.Guid });
                    break;
            }
            if (feed != null) Controls.Title.Text = feed.ToString();
        }

        private void DownloadButtonOnClick(object sender, EventArgs eventArgs)
        {
            //Controls.ProgressBar.Progress = 0;
            //Controls.DownloadFrame.Visibility = ViewStates.Visible;

            ////ThreadPool.QueueUserWorkItem(o => {
            //    var entry = YoutubeEntry.Create(_youtubeUrl.Uri);
            //    entry.OnEntryDownloadAvailable += OnDownloadAvailable;
            //    entry.DownloadAsync(MediaType.Video, false);
            ////});
            _lists.Add(new List<YoutubeEntry>(1) { YoutubeEntry.Create(_youtubeUrl.Uri) }, MediaType.Video);
        }

    }
}
