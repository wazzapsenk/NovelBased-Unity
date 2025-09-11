namespace Nullframes.Intrigues
{
    [System.Serializable]
    public class HeirFilter
    {
        public delegate TResult HFilter<in TDecedent, in THeir, out TResult>(TDecedent arg1, THeir arg2);
        public delegate TResult HOrderBy<in TDecedent, in THeir, out TResult>(TDecedent arg1, THeir arg2);

        public HFilter<Actor, Actor, bool> FilterAbsolute { get; set; }
        public HFilter<Actor, Actor, bool> Filter { get; set; }
        public HFilter<Actor, Actor, bool> OrderFilter { get; set; }
        public HOrderBy<Actor, Actor, object> OrderBy { get; set; }
        public HOrderBy<Actor, Actor, object> OrderByDesc { get; set; }
    }
}