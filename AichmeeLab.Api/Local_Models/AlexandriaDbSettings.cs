

namespace AichmeeLab.Api.LocalModels
{
    class AlexandriaDbSettings
    {
        public string DatabaseName { get; set; } = string.Empty;

        public string ArticlesCollectionName { get; set; } = string.Empty;
        public string ImagesCollectionName { get; set; } = string.Empty;

        public string TagsCollectionName { get; set; } = string.Empty;
    }
}
