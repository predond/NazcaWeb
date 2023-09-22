using Microsoft.CodeAnalysis.Elfie.Diagnostics;

namespace NazcaWeb.Models
{
    public class IRC
    {
        private CancellationTokenSource cancellationTokenSource;
        public CancellationToken cancellationToken;
        private CancellationTokenSource returnCancellationTokenSource;
        private CancellationToken returnCancellationToken;
        private Stack<string> playingHistory;
        private bool videoReturning = false;

        IRD controller;

        public bool ReadyToGo = false;
        public bool IsPlaying = false;
        public string ProcessedPath = "";
        public string StartPath = @"E:\Nazca\VinGen\Filmy";

        // Delegaty dla zdarzeń
        public delegate void InitializedEventHandler(object sender, EventArgs e);
        public delegate void VideoStartedEventHandler(object sender, VideoEventArgs e);
        public delegate void VideoStoppedEventHandler(object sender, EventArgs e);
        public delegate void ActualPathChangedEventHandler(object sender, string e);

        // Zdarzenia
        public event InitializedEventHandler Initialized;
        public event VideoStartedEventHandler VideoStarted;
        public event VideoStoppedEventHandler VideoStopped;
        public event ActualPathChangedEventHandler ActualPathChanged;

        public IRC(string deviceAddress)
        {
            Task.Run(() => {
                controller = new IRD(deviceAddress);
                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;
                returnCancellationTokenSource = new CancellationTokenSource();
                returnCancellationToken = returnCancellationTokenSource.Token;
                playingHistory = new Stack<string>();
                Initialized?.Invoke(this, EventArgs.Empty);
                ReadyToGo = true;
            });
        }

        public async void SimPlayVideo(VideoItem videoItem, Action<string, bool> onSegmentProcessed)
        {
            var path = videoItem.FullPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in path.Select((v, i) => new { i, v }))
            {
                onSegmentProcessed(segment.v, segment.i == path.Length - 1);
                await Task.Delay(500);
            }
        }

        public bool BrowseForVideo(VideoItem video, bool startTV, bool startFromVT, Action<string, bool, bool> onSegmentProcessed)
        {
            Task.Run(async () =>
            {
                IsPlaying = true;
                playingHistory.Clear();
                VideoStarted?.Invoke(this, new VideoEventArgs(video));
                var processedPath = StartPath;
                onSegmentProcessed(processedPath, false, false);
                if (startTV)
                {
                    //MessageBox.Show("Włączam telewizor");
                    controller.SendQuery("TV.Power");
                    await Task.Delay(5000, cancellationToken);
                }
                if (startTV || startFromVT)
                {
                    //MessageBox.Show("Viera Tools");
                    controller.SendQuery("TV.HOME");
                    await Task.Delay(1500, cancellationToken);
                    //MessageBox.Show("OK");
                    controller.SendQuery("TV.OK");
                    await Task.Delay(1500, cancellationToken);
                    //MessageBox.Show("W górę");
                    controller.SendQuery("TV.Arrow up");
                    await Task.Delay(750, cancellationToken);
                    //MessageBox.Show("OK");
                    controller.SendQuery("TV.OK");
                    await Task.Delay(500, cancellationToken);
                    //MessageBox.Show("W górę");
                    controller.SendQuery("TV.Arrow up");
                    await Task.Delay(750, cancellationToken);
                    //MessageBox.Show("OK");
                    controller.SendQuery("TV.OK");
                    await Task.Delay(500, cancellationToken);
                }

                // \\DISKSTATION\video1\Filmy Dawid\Private\86 - Eighty Six\86 Eighty Six 01.mp4
                foreach (var target in video.FullPath.Replace("\\\\", "\\").Replace(processedPath, "").Split('\\', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        IsPlaying = false;
                        Console.WriteLine("Zakończyłem pracę sekwencyjną! 1");
                        break;
                    }

                    var actPath = Path.Combine(processedPath, target);
                    var details = File.GetAttributes(actPath);
                    var targetIndex = -1;
                    var videosList = new List<string>();
                    if ((details & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        var videos = Directory.GetDirectories(processedPath).OrderBy(v => v, new TVComparer()).ToList();
                        videos.Remove(Path.Combine(processedPath, "#recycle"));
                        targetIndex = videos.IndexOf(actPath);
                        videosList = videos;
                    }
                    else
                    {
                        var videos = Directory.GetFileSystemEntries(processedPath, "*", SearchOption.TopDirectoryOnly)
                                                                        .OrderBy(e => !Directory.Exists(e))
                                                                        .ThenBy(e => e, new TVComparer()).ToList();
                        videos.Remove(Path.Combine(processedPath, "#recycle"));
                        targetIndex = videos.IndexOf(actPath);
                        videosList = videos;
                    }

                    processedPath = Path.Combine(processedPath, target);

                    playingHistory.Push("TV.OK");
                    for (int i = 0; i < targetIndex; i++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            IsPlaying = false;
                            Console.WriteLine("Zakończyłem pracę sekwencyjną! 2");
                            break;
                        }
                        //MessageBox.Show("W dół");
                        controller.SendQuery("TV.Arrow down");
                        playingHistory.Push("TV.Arrow up");
                        onSegmentProcessed(videosList[i].Split('\\').Last(), false, true);
                        ProcessedPath = videosList[i].Replace("\\", "\\\\");
                        ActualPathChanged?.Invoke(this, ProcessedPath);
                        Console.WriteLine("Przechodzę przez... " + videosList[i].Split('\\').Last());
                        await Task.Delay(480, cancellationToken);
                    }
                    onSegmentProcessed(videosList[targetIndex].Split('\\').Last(), File.Exists(videosList[targetIndex]), true);
                    ProcessedPath = videosList[targetIndex].Replace("\\", "\\\\");
                    ActualPathChanged?.Invoke(this, ProcessedPath);

                    //MessageBox.Show("OK");
                    await Task.Delay(750, cancellationToken);
                    controller.SendQuery("TV.OK");
                    if (!File.Exists(processedPath))
                        onSegmentProcessed("", false, false);
                    await Task.Delay(750, cancellationToken);
                }
                IsPlaying = false;
                VideoStopped?.Invoke(this, new VideoEventArgs(video));
            }, cancellationToken);

            return true;
        }

        private async void ReturnToOriginalPathAsync()
        {
            videoReturning = true;

            var path = ProcessedPath.Replace("\\\\", "\\").Replace(StartPath, "").Split("\\", StringSplitOptions.RemoveEmptyEntries).Reverse();
            foreach (var segment in path)
            {
                if (returnCancellationToken.IsCancellationRequested)
                    break;
                controller.SendQuery("TV.Back");
                await Task.Delay(750, returnCancellationToken).TryAsync();
                Console.WriteLine("Cofam \"" + segment + "\" ze ścieżki \"" + ProcessedPath + "\", do ścieżki \"" + ProcessedPath.Replace(segment, "").Trim('\\') + "\".");
                ProcessedPath = ProcessedPath.Replace(segment, "").TrimEnd('\\');
                ActualPathChanged?.Invoke(this, ProcessedPath);
            }

            if (!returnCancellationToken.IsCancellationRequested)
                controller.SendQuery("TV.OK");

            /*var cmd = "";
            while (playingHistory.Count > 0)
            {
                cmd = playingHistory.Pop();
                controller.SendQuery(cmd);
                await Task.Delay(cmd.Contains("OK") ? 750 : 480, token);
            }*/

            videoReturning = false;
            VideoStopped?.Invoke(this, EventArgs.Empty);
        }

        public void StopVideoProcessing()
        {
            if (!videoReturning)
            {
                cancellationTokenSource?.Cancel();

                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;

                ReturnToOriginalPathAsync();
            }
            else
            {
                returnCancellationTokenSource?.Cancel();

                returnCancellationTokenSource = new CancellationTokenSource();
                returnCancellationToken = returnCancellationTokenSource.Token;
            }
        }
    }

    class TVComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            bool xIsLower = char.IsLower(x[0]);
            bool yIsLower = char.IsLower(y[0]);

            if (xIsLower && !yIsLower)
            {
                return 1;
            }
            else if (!xIsLower && yIsLower)
            {
                return -1;
            }
            else
            {
                return string.Compare(x, y, StringComparison.Ordinal);
            }
        }
    }

    public class VideoEventArgs : EventArgs
    {
        public VideoItem VideoItem { get; }

        public VideoEventArgs(VideoItem videoItem)
        {
            VideoItem = videoItem;
        }
    }

    public static class TaskExtensions
    {
        public static async Task<bool> TryAsync(this Task task)
        {
            try
            {
                await task;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
