using Microsoft.AspNetCore.SignalR;

namespace NazcaWeb.Hubs
{
    public class VideoHub : Hub
    {
        public async Task UpdateDivContentAsync(string content)
        {
            await Clients.All.SendAsync("updateDiv", content);
        }
    }
}
