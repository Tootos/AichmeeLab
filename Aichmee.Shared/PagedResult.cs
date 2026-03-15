namespace Aichmee.Shared
{
    public class PagedResult<T>
    {
        public List<T> ?Items { get; set; }
        public long  PageCount { get; set; }
    }
}
