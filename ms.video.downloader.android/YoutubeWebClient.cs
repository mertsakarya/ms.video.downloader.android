using System;
using Android.Webkit;
using Android.Widget;
using ms.video.downloader.android.service;
using ms.video.downloader.android.service.download;


namespace ms.video.downloader.android
{
    public class YoutubeWebViewClient : WebViewClient
    {
        private Button _downloadButton;
        private YoutubeUrl _youtubeUrl;
        public DownloadLists Lists;
        private LocalService _settings;


        public YoutubeWebViewClient(Button downloadButton, LocalService settings)
        {
            _downloadButton = downloadButton;
            _downloadButton.Click += DownloadButtonOnClick;
            _settings = settings;
            Lists = new DownloadLists(_settings, OnDownloadStatusChange);
        }

        public override bool ShouldOverrideUrlLoading(WebView view, string url)
        {
            view.LoadUrl(url);
            return true;
        }

        public override void OnPageFinished(WebView view, string url)
        {
            base.OnPageFinished(view, url);
            _youtubeUrl = YoutubeUrl.Create(new Uri(url));
            if (!String.IsNullOrEmpty(_youtubeUrl.VideoId)) {
                
            }
        }

        private void OnDownloadStatusChange(Feed list, Feed feed, DownloadState downloadState, double percentage)
        {
            throw new NotImplementedException();
        }

        private void DownloadButtonOnClick(object sender, EventArgs eventArgs)
        {
            throw new NotImplementedException();
        }


    }
}
