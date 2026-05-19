namespace AichmeeLab.Api.LocalModels
{
    class MonitorDbSettings
    {
        public string DatabaseName { get; set; } = string.Empty;

        public string AdminsCollectionName { get; set; } = string.Empty;
        public string WatchlistCollectionName {get; set;} =string.Empty;
    }
}
