using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ms.video.downloader.android.service.download
{
    public class AudioConverter
    {
        private readonly EntryDownloadStatusEventHandler _onEntryDownloadStatusChange;
        private readonly YoutubeEntry _youtubeEntry;
        private readonly string _applicationPath;

        public AudioConverter(YoutubeEntry youtubeEntry, EntryDownloadStatusEventHandler onEntryDownloadStatusChange)
        {
            _youtubeEntry = youtubeEntry;
            _onEntryDownloadStatusChange = onEntryDownloadStatusChange;
            _applicationPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        }

        public void ConvertToMp3(bool ignoreIfFileExists = false)
        {

            if (_youtubeEntry.DownloadState != DownloadState.DownloadFinish) return;
            var title = DownloadHelper.GetLegalPath(_youtubeEntry.Title);
            var audioFileName = title + ".mp3";
            var videoFileName = title + _youtubeEntry.VideoExtension;
            var fileExists = DownloadHelper.FileExists(_youtubeEntry.VideoFolder, videoFileName);
            if (!fileExists) return;
            fileExists = DownloadHelper.FileExists(_youtubeEntry.DownloadFolder, audioFileName);
            if (ignoreIfFileExists && fileExists) {
                if (_onEntryDownloadStatusChange != null) _onEntryDownloadStatusChange(_youtubeEntry, DownloadState.Ready, 100.0);
            } else {
                try {
                    if (fileExists) _youtubeEntry.DownloadFolder.GetFileAsync(audioFileName).DeleteAsync();
                    var videoFile = _youtubeEntry.VideoFolder.GetFileAsync(videoFileName);
                    var audioFile = _youtubeEntry.DownloadFolder.CreateFileAsync(audioFileName);
                    Task.Factory.StartNew(() => TranscodeFile(videoFile, audioFile));
                }
                catch  {
                    if (_onEntryDownloadStatusChange != null) _onEntryDownloadStatusChange(_youtubeEntry, DownloadState.Error, 100.0);
                }
            }
        }


        protected void TranscodeFile(StorageFile videoFile, StorageFile audioFile)
        {
            var arguments = String.Format("-i \"{0}\" -acodec mp3 -y -ac 2 -ab 160 \"{1}\"", videoFile, audioFile);
            var process = new Process {
                EnableRaisingEvents = true,
                StartInfo = {
                    FileName = _applicationPath + "\\Executables\\ffmpeg.exe",
                    Arguments = arguments,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                }
            };
            if (_onEntryDownloadStatusChange != null) _onEntryDownloadStatusChange(_youtubeEntry, DownloadState.ConvertAudioStart, 50);

            process.Start();
            using (var d = process.StandardError) {
                var duration = new TimeSpan();
                TimeSpan current;
                do {
                    var s = d.ReadLine() ?? "";
                    Debug.WriteLine(s);
                    if (s.Contains("Duration: ")) {
                        duration = ParseDuration("Duration: ", ',', s);
                    }
                    else {
                        if (s.Contains(" time=")) {
                            current = ParseDuration(" time=", ' ', s);
                            var percentage = (current.TotalMilliseconds / duration.TotalMilliseconds) * 50;
                            if (_onEntryDownloadStatusChange != null) _onEntryDownloadStatusChange(_youtubeEntry, DownloadState.DownloadProgressChanged, 50 + percentage );
                        }
                    }
                } while (!d.EndOfStream);
            }
            process.WaitForExit();
            DownloadState state;
            state = process.ExitCode == 0 ? DownloadState.Ready : DownloadState.Error;
            if (_onEntryDownloadStatusChange != null) _onEntryDownloadStatusChange(_youtubeEntry, state, 100.0);
            process.Close();
        }

        private TimeSpan ParseDuration(string start, char end, string s)
        {
            if (s == null) return new TimeSpan(0);
            var i = s.IndexOf(start, StringComparison.Ordinal);
            if (i < 0) return new TimeSpan(0);
            i += start.Length;
            var j = s.IndexOf(end, i);
            j = j - i;
            var timespan = s.Substring(i, j);
            var ts = TimeSpan.Parse(timespan);
            return ts;
        }

        
    }
}