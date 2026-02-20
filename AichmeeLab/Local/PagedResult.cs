namespace AichmeeLab.Local
{
    public class PagedResult<T>
    {
        public List<T> ?Items { get; set; }
        public int  PageCount { get; set; }
    }
}
