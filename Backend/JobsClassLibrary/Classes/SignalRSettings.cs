
namespace JobsClassLibrary.Classes
{
    public class SignalRSettings
    {
        public required string BaseUrl { get; set; }
        public required string HubPath { get; set; }
        public string FullHubUrl => $"{BaseUrl.TrimEnd('/')}{HubPath}";
    }
}
