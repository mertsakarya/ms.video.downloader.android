using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Android.OS;
using ms.video.downloader.android.service;
using ms.video.downloader.android.service.download;

namespace ms.video.downloader.android
{
    [Activity(Label = "ms.video.downloader.android", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity
    {
        public DownloadLists Lists;
        private LocalService _settings;
        private WebView _webView1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            
            var button = FindViewById<Button>(Resource.Id.MyButton);
            _settings = new LocalService();
            _webView1 = FindViewById<WebView>(Resource.Id.webView1);
            _webView1.Settings.JavaScriptEnabled = true;
            _webView1.SetWebViewClient(new YoutubeWebViewClient(button, _settings));
            Title = "ms.video.downloader for Android ver. " + _settings.Version;
            _webView1.LoadUrl("http://www.youtube.com/");

            //Loading(false);
        }
    }
}

