using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Android.OS;

namespace ms.video.downloader.android
{
    public class ViewControls
    {
        public Button DownloadButton { get; set; }
        public ProgressBar ProgressBar { get; set; }
        public TextView Title { get; set; }
        public View DownloadFrame { get; set; }
        public Activity Activity { get; set; }
        public Context Context { get; set; }
    }

    [Activity(Label = "MS Video Downloader", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity
    {
        private WebView _webView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            var downloadButton = FindViewById<Button>(Resource.Id.downloadButton);
            FindViewById<Button>(Resource.Id.backButton).Click += (sender, args) => { if (_webView.CanGoBack()) _webView.GoBack(); };
            FindViewById<Button>(Resource.Id.forwardButton).Click += (sender, args) => { if (_webView.CanGoForward()) _webView.GoForward(); };
            _webView = FindViewById<WebView>(Resource.Id.webView1);
            var progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar1);
            var title = FindViewById<TextView>(Resource.Id.textView1);
            var frame = FindViewById(Resource.Id.frameDownload);
            frame.Visibility = ViewStates.Gone;
            var viewControls = new ViewControls {
                DownloadButton = downloadButton,
                ProgressBar = progressBar,
                Title = title,
                DownloadFrame = frame,
                Activity = this
            };
            _webView.Settings.JavaScriptEnabled = true;
            _webView.SetWebViewClient(new YoutubeWebViewClient(viewControls));
            _webView.LoadUrl("http://www.youtube.com/");
        }
    }
}

