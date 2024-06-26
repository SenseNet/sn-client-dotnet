namespace SenseNet.Client.Linq.Predicates;

/// <summary>
/// Defines a range predicate inspired by Lucene query syntax.
/// </summary>
public class RangePredicate : SnQueryPredicate
{
    /// <summary>
    /// Gets the field name of the predicate.
    /// </summary>
    public string FieldName { get; private set; }

    /// <summary>
    /// Gets the minimum value of the range. It can be null.
    /// </summary>
    public IndexValue Min { get; private set; }

    /// <summary>
    /// Gets the maximum value of the range. It can be null.
    /// </summary>
    public IndexValue Max { get; private set; }

    /// <summary>
    /// Gets the value that is true if the minimum value is in the range.
    /// </summary>
    public bool MinExclusive { get; private set; }

    /// <summary>
    /// Gets the value that is true if the maximum value is in the range.
    /// </summary>
    public bool MaxExclusive { get; private set; }

    /// <summary>
    /// Initializes a new instance of RangePredicate.
    /// </summary>
    public RangePredicate(string fieldName, IndexValue min, IndexValue max, bool minExclusive, bool maxExclusive)
    {
        FieldName = fieldName;
        Min = min;
        Max = max;
        MinExclusive = minExclusive;
        MaxExclusive = maxExclusive;
    }

    /// <summary>Returns a string that represents the current object.</summary>
    public override string ToString()
    {
        string oneTerm = null;

        string op = null;
        if (Min == null)
        {
            op = !MaxExclusive ? "<=" : "<";
            oneTerm = Max.ValueAsString;
        }
        if (Max == null)
        {
            op = !MinExclusive ? ">=" : ">";
            oneTerm = Min?.ValueAsString;
        }

        if (op != null)
            return $"{FieldName}:{op}{oneTerm}";

        var start = !MinExclusive ? '[' : '{';
        var end = !MaxExclusive ? ']' : '}';
        return $"{FieldName}:{start}{Min} TO {Max}{end}";
    }
}
