using SharpOrm.Builder;

namespace SharpOrm
{
    /// <summary>
    /// Represents a callback method to be invoked with a <see cref="QueryBase"/> object.
    /// </summary>
    /// <param name="query">The query that will be passed to the callback method.</param>
    public delegate void QueryCallback(QueryBase query);
}
