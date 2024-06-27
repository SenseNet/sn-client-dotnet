namespace SenseNet.Client.Linq.Predicates;

public abstract class SnQueryPredicate
{
    /// <summary>
    /// Gets or sets the weight of the current predicate. The value can be null.
    /// </summary>
    public double? Boost { get; set; }
}