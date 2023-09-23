using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NazcaWeb.Hubs;
using NazcaWeb.Models;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using TagLib;

namespace NazcaWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRC _irc;
        private readonly IHubContext<VideoHub> _hubContext;

        public HomeController(ILogger<HomeController> logger, IRC irc, IHubContext<VideoHub> hubContext)
        {
            _logger = logger;
            _irc = irc;
            _hubContext = hubContext;

            _irc.Initialized += _irc_InitializedAsync;
            _irc.VideoStarted += _irc_VideoStartedAsync;
            _irc.VideoStopped += _irc_VideoStoppedAsync;
            _irc.ActualPathChanged += _irc_ActualPathChanged;
        }

        public IActionResult Index()
        {
            Console.WriteLine($"[{DateTime.Now.ToString("T")}] Podłączył się klient (" + HttpContext.Connection.RemoteIpAddress + ").");
            Task.Run(() =>
            {
                VideoModel.GetVideos(IRC.StartPath);
                Console.WriteLine($"[{DateTime.Now.ToString("T")}] Gotowe!");
            });
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetNewList(bool nazca = false)
        {
            return PartialView(nazca ? "_VideosListNazca" : "_VideosListNormal", VideoModel.GroupedFiles);
        }

        [HttpPost]
        public async Task PlayVideo([FromBody]StartVideoRequest videoId)
        {
            var video = VideoModel.GetVideoById(Guid.Parse(videoId.VideoId));
            if (video != null)
            {
                await _hubContext.Clients.All.SendAsync("clearDiv");
                try
                {
                    _irc.BrowseForVideo(video, false, false, async (segmentPath, active, overwrite) =>
                    {
                        if (overwrite)
                        {
                            //await _hubContext.Clients.All.SendAsync("overwriteDiv", segmentPath, active ? "active" : "");
                        }
                        else
                        {
                            Console.WriteLine("Przetwarzam... " + segmentPath);
                            //await _hubContext.Clients.All.SendAsync("updateDiv", segmentPath, active ? "active" : "");
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Operacje zostały anulowane!");
                }
            }
        }

        [HttpPost]
        public async Task StopVideo([FromBody]StopVideoRequest path)
        {
            Console.WriteLine("Ścieżka controller: " + path.Path);
            _irc.StopVideoProcessing();
            await _hubContext.Clients.All.SendAsync("clearDiv");
            await _hubContext.Clients.All.SendAsync("updateDiv", path.Path);
            await _hubContext.Clients.All.SendAsync("overwriteDiv", "", "active");
        }

        [Route("Films/Nazca")]
        public IActionResult Nazca()
        {
            //var obj = VideoModel.GetVideos(@"\\DISKSTATION\video1\Filmy Dawid\Private");
            ViewBag.nazcaMode = true;
            return View("VideoSearcher", VideoModel.GroupedFiles);
        }

        public IActionResult VideoDetails(Guid videoId)
        {
            var retVal = VideoModel.GetVideoById(videoId);
            if (retVal != null && retVal.Duration == TimeSpan.Zero)
            {
                var details = TagLib.File.Create(retVal.FullPath.Replace("\\\\", "\\"), ReadStyle.Average);
                retVal.Duration = details != null ? details.Properties.Duration : TimeSpan.Zero;
                retVal.MimeType = details != null ? details.MimeType.Split('/').Last().ToUpper() : "";
            }

            return View(retVal);
        }

        [HttpGet]
        [Route("Films")]
        public IActionResult VideoSearcher()
        {
            /*var obj = new VideoItem[] {
                        new VideoItem() { Title = "Film A", FullPath = @"directory\A" },
                        new VideoItem() { Title = "Film B", FullPath = @"directory\B" },
                        new VideoItem() { Title = "Film C", FullPath = @"directory\C" }};*/
            return View("VideoSearcher", VideoModel.GroupedFiles);
        }

        [HttpPost]
        [Route("Films")]
        public IActionResult VideoSearcher(string? search)
        {
            return View(VideoModel.GetVideosByName(search ?? "" ));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async void _irc_InitializedAsync(object sender, EventArgs e)
        {
            //Console.WriteLine("[EVENT] Zainicjalizowano klasę IRC");
            await _hubContext.Clients.All.SendAsync("toggleButtons", false, true);
        }

        private async void _irc_VideoStartedAsync(object sender, VideoEventArgs e)
        {
            //Console.WriteLine("[EVENT] Rozpoczęto odtwarzanie " + e.VideoItem.Title);
            await _hubContext.Clients.All.SendAsync("toggleButtons", true, false);
        }

        private async void _irc_VideoStoppedAsync(object sender, EventArgs e)
        {
            //Console.WriteLine("[EVENT] Zatrzymano odtwarzanie filmu");
            await _hubContext.Clients.All.SendAsync("restorePath");
            await _hubContext.Clients.All.SendAsync("toggleButtons", false, true);
        }

        private async void _irc_ActualPathChanged(object sender, string e)
        {
            //Console.WriteLine("[EVENT] Zmieniła się aktualna ścieżka -> " + e);
            await _hubContext.Clients.All.SendAsync("clearDiv");
            await _hubContext.Clients.All.SendAsync("updateDiv", e);
        }
    }

    public class StartVideoRequest
    {
        public string VideoId { get; set; }
    }

    public class StopVideoRequest
    {
        public string Path { get; set; }
    }
}