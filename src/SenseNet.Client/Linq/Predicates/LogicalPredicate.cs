using System.Collections.Generic;
using System.Linq;

namespace SenseNet.Client.Linq.Predicates;

/// <summary>
/// Defines a logical predicate that represents one level of the parenthesis in the CQL queries.
/// </summary>
public class LogicalPredicate : SnQueryPredicate
{
    /// <summary>
    /// Gets the list of the logical clauses.
    /// </summary>
    public List<LogicalClause> Clauses { get; private set; } = new List<LogicalClause>();

    /// <summary>
    /// Initializes a new instance of LogicalPredicate with empty clause list.
    /// </summary>
    public LogicalPredicate() { }

    /// <summary>
    /// Initializes a new instance of LogicalPredicate with an initial clause list.
    /// </summary>
    public LogicalPredicate(IEnumerable<LogicalClause> clauses)
    {
        Clauses.AddRange(clauses);
    }

    /// <summary>Returns a string that represents the current object.</summary>
    public override string ToString()
    {
        return $"({string.Join(" ", Clauses.Select(x => x.ToString()))})";
    }
}