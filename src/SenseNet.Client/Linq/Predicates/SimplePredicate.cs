﻿namespace SenseNet.Client.Linq.Predicates
{
    /// <summary>
    /// Represents a simple clause on the CQL query.
    /// </summary>
    public class SimplePredicate : SnQueryPredicate
    {
        /// <summary>
        /// Gets the field name of the clause.
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// Gets the value of the clause.
        /// </summary>
        public IndexValue Value { get; private set; }

        /// <summary>
        /// Gets a value for compiling fuzzy queries.
        /// </summary>
        public double? FuzzyValue { get; private set; }

        /// <summary>
        /// Initializes a new SimplePredicate instance.
        /// </summary>
        /// <param name="fieldName">Name of the field in the clause.</param>
        /// <param name="value">Value of the clause.</param>
        /// <param name="fuzzyValue">Fuzzy value. Optional, default value is null.</param>
        public SimplePredicate(string fieldName, IndexValue value, double? fuzzyValue = null)
        {
            FieldName = fieldName;
            Value = value;
            FuzzyValue = fuzzyValue;
        }

        /// <summary>Returns a string that represents the current object.</summary>
        public override string ToString()
        {
            return $"{FieldName}:{Value.ValueAsString}";
        }
    }
}
