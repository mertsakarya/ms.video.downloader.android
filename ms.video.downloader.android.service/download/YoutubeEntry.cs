using System;
using System.Collections.ObjectModel;
using ms.video.downloader.android.service.msYoutube;

namespace ms.video.downloader.android.service.download
{

    public delegate void EntriesReady(ObservableCollection<Feed> entries);

    public delegate void EntryDownloadStatusEventHandler(Feed feed, DownloadState downloadState, double percentage);
    public delegate void EntryDownloadAvailableEventHandler(YoutubeEntry feed, VideoInfo videoInfo, MediaType mediaType);

    public class YoutubeEntry : Feed
    {
        private readonly MSYoutubeSettings _settings;
        private Uri _uri;

        public YoutubeEntry Parent { get; private set; }
        public string VideoExtension { get; set; }
        public string[] ThumbnailUrls { get; set; }
        public YoutubeUrl YoutubeUrl { get; protected set; }
        public StorageFolder BaseFolder { get; set; }
        public StorageFolder ProviderFolder { get; set; }
        public StorageFolder VideoFolder { get; set; }
        public StorageFolder DownloadFolder { get; set; }
        public MediaType MediaType { get; set; }
        public string ChannelName { get { return Parent == null ? "" : Parent.Title; } }
        public EntryDownloadStatusEventHandler OnEntryDownloadStatusChange;
        public EntryDownloadAvailableEventHandler OnEntryDownloadAvailable;

        public Uri Uri
        {
            get { return _uri; }
            set { _uri = value; if (value != null) YoutubeUrl = YoutubeUrl.Create(_uri); }
        }

        private YoutubeEntry(YoutubeEntry parent = null)
        {
            Parent = parent;
            _settings = new MSYoutubeSettings( "MS.Youtube.Downloader", "AI39si76x-DO4bui7H1o0P6x8iLHPBvQ24exnPiM8McsJhVW_pnCWXOXAa1D8-ymj0Bm07XrtRqxBC7veH6flVIYM7krs36kQg" ) {AutoPaging = true, PageSize = 50};
        }

        //public void DownloadAsync(MediaType mediaType, bool ignore)
        //{
        //    if (ExecutionStatus == ExecutionStatus.Deleted) { Delete(); return; }
        //    MediaType = mediaType;
        //    VideoInfo videoInfo = null;
        //    try {
        //        var videoInfos = DownloadHelper.GetDownloadUrlsAsync(Uri);
        //        foreach (VideoInfo info in videoInfos)
        //            if (info.VideoType == VideoType.Mp4 && info.Resolution == 360) { videoInfo = info; break; }
        //    } catch {
        //        videoInfo = null;
        //    }
        //    if (videoInfo == null) { UpdateStatus(DownloadState.Error); return; }
        //    Title = videoInfo.Title;
        //    VideoExtension = videoInfo.VideoExtension;
        //    if (OnEntryDownloadAvailable != null)
        //        OnEntryDownloadAvailable(this, videoInfo, MediaType);
        //}

        
        public void DownloadAsync(MediaType mediaType, bool ignore)
        {
            if (ExecutionStatus == ExecutionStatus.Deleted) { Delete(); return; }
            UpdateStatus(DownloadState.DownloadStart, 0.0);
            MediaType = mediaType;
            BaseFolder = KnownFolders.VideosLibrary;
            ProviderFolder = DownloadHelper.GetFolder(BaseFolder, Enum.GetName(typeof(ContentProviderType), YoutubeUrl.Provider));
            VideoFolder = DownloadHelper.GetFolder(ProviderFolder, DownloadHelper.GetLegalPath(ChannelName));

            if (MediaType == MediaType.Audio) {
                var audioFolder = KnownFolders.MusicLibrary;
                ProviderFolder = DownloadHelper.GetFolder(audioFolder, Enum.GetName(typeof(ContentProviderType), YoutubeUrl.Provider));
                DownloadFolder = DownloadHelper.GetFolder(ProviderFolder, DownloadHelper.GetLegalPath(ChannelName));
            }
            VideoInfo videoInfo = null;
            try {
                var videoInfos = DownloadHelper.GetDownloadUrlsAsync(Uri);
                foreach (VideoInfo info in videoInfos)
                    if (info.VideoType == VideoType.Mp4 && info.Resolution == 360) { videoInfo = info; break; }
            } catch {
                videoInfo = null;
            }
            if (videoInfo == null) { UpdateStatus(DownloadState.Error); return; }
            Title = videoInfo.Title;
            VideoExtension = videoInfo.VideoExtension;
            DownloadState = DownloadState.TitleChanged;
            var videoFile = DownloadHelper.GetLegalPath(Title) + VideoExtension;
            var fileExists = DownloadHelper.FileExists(VideoFolder, videoFile);
            if (!(ignore && fileExists)) {
                if (OnEntryDownloadStatusChange != null) OnEntryDownloadStatusChange(this, DownloadState, Percentage);
                DownloadHelper.DownloadToFileAsync(this, videoInfo.DownloadUri, VideoFolder, videoFile,
                    (count, total) => UpdateStatus(DownloadState.DownloadProgressChanged, ((double)count / total) * ((MediaType == MediaType.Audio) ? 50 : 100)));
            }
            DownloadState = DownloadState.DownloadFinish;
            if (MediaType == MediaType.Audio) {
                Percentage = 50.0;
                var converter = new AudioConverter(this, OnAudioConversionStatusChange);
                converter.ConvertToMp3(ignore);
            } else if (OnEntryDownloadStatusChange != null)
                UpdateStatus(DownloadState.Ready);
        }
        
        private void OnAudioConversionStatusChange(Feed feed, DownloadState downloadState, double percentage)
        {
            UpdateStatus(downloadState, percentage);
        }

        internal void UpdateStatus(DownloadState state, double percentage = 100.0)
        {
            DownloadState = state;
            Percentage = percentage;
            if (OnEntryDownloadStatusChange != null)
                OnEntryDownloadStatusChange(this, DownloadState, Percentage);
        }

        public override string ToString()
        {
            if (Title != null) return Title;
            if (Uri != null) return Uri.ToString();
            return Guid.ToString();
        }

        public YoutubeEntry Clone()
        {
            var entry = new YoutubeEntry {
                Title = Title,
                BaseFolder = BaseFolder,
                Parent = Parent,
                Description = Description,
                DownloadFolder = DownloadFolder,
                ProviderFolder = ProviderFolder,
                MediaType = MediaType,
                ThumbnailUrl = ThumbnailUrl,
                Uri = Uri,
                VideoExtension = VideoExtension,
                VideoFolder = VideoFolder,
                ExecutionStatus = ExecutionStatus
            };
            if (entry.ExecutionStatus == ExecutionStatus.Deleted) entry.DownloadState = DownloadState.Deleted;
            if (ThumbnailUrls != null && ThumbnailUrls.Length > 0) {
                entry.ThumbnailUrls = new string[ThumbnailUrls.Length];
                for (var i = 0; i < ThumbnailUrls.Length; i++)
                    entry.ThumbnailUrls[i] = ThumbnailUrls[i];
            }
            return entry;
        }

        public static YoutubeEntry Create(Uri uri, YoutubeEntry parent = null)
        {
            var entry = new YoutubeEntry(parent);
            if (uri != null) 
                entry.Uri = uri;
            return entry;
        }

        public override void Delete()
        {
            if (DownloadState == DownloadState.Error || DownloadState == DownloadState.Ready) return;
            try {
                var title = DownloadHelper.GetLegalPath(Title);
                var videoFile = title + VideoExtension;
                if (DownloadHelper.FileExists(VideoFolder, videoFile)) DownloadHelper.GetFile(VideoFolder, videoFile).DeleteAsync();
                if (MediaType == MediaType.Audio) {
                    var audioFile = title + ".mp3";
                    if (DownloadHelper.FileExists(DownloadFolder, audioFile)) DownloadHelper.GetFile(DownloadFolder, audioFile).DeleteAsync();
                }
            } catch { }
            base.Delete();
            UpdateStatus(DownloadState.Deleted);
        }
    }
}
