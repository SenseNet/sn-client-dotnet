using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SenseNet.Client.Linq;


internal class ProjectionVisitor : ExpressionVisitor
{

    private List<string> _referenceFields = new();
    private List<string> _fields = new();

    public string[]? ExpandedFields { get; private set; }
    public string[]? SelectedFields { get; private set; }

    private void Compute()
    {
        var expanded = _referenceFields.Distinct().Select(x=>x.Replace('.', '/')).ToArray();
        if (expanded.Length > 0)
            ExpandedFields = expanded;

        var selected = new List<string>();

        foreach (var field in _fields.Distinct().Select(x => x.Replace('.', '/')))
        {
            if (selected.Any(x => x.StartsWith(field + "/")))
                continue;
            selected.Add(field);
        }

        if (selected.Count > 0)
            SelectedFields = selected.ToArray();
    }

    private string _expectedPrefix = string.Empty;
    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        if (node.Parameters.Count != 1)
            throw new NotSupportedException("Invalid Select expression. Too many lambda parameters.");
        _expectedPrefix = node.Parameters[0].Name! + ".";

        return base.VisitLambda(node);
    }

    private bool _parsing;
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (_parsing)
            throw new NotSupportedException("Invalid Select expression.");
        _parsing = true;

        if (node.Arguments.Count != 1)
            throw new NotSupportedException("Invalid Select expression. Too many arguments.");

        if (node.Arguments[0] is NewArrayExpression arrayInit)
        {
            var expressionId = 0;
            foreach (var expression in arrayInit.Expressions)
            {
                expressionId++;
                if (expression.NodeType == ExpressionType.MemberAccess)
                    continue;
                if (expression.NodeType == ExpressionType.Convert)
                {
                    var convertExpr = (UnaryExpression) expression;
                    if(convertExpr.Operand.NodeType == ExpressionType.MemberAccess)
                        continue;
                }
                throw new NotSupportedException($"Invalid Select expression. The {FormatId(expressionId)} parameter is forbidden. Only the property-access expressions are allowed.");
            }
        }

        base.VisitMethodCall(node);

        Compute();
        return node;
    }


    private static string FormatId(int id)
    {
        if (id < 1) return id.ToString();
        if (id == 1) return "first";
        if (id == 2) return "second";
        if (id == 3) return "third";
        if (id < 20) return $"{id}th";
        // id >= 20)
        var mod = id % 10;
        if (mod == 1) return $"{id}st";
        if (mod == 2) return $"{id}nd";
        if (mod == 3) return $"{id}rd";
        return $"{id}th";
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var text = node.ToString();
        if(!text.StartsWith(_expectedPrefix))
            throw new NotSupportedException($"Invalid Select expression. The {text} is forbidden. Only the property-access expressions of the '{_expectedPrefix}' are allowed.");
        text = text.Substring(_expectedPrefix.Length);

        if (typeof(Content).IsAssignableFrom(node.Type))
            _referenceFields.Add(text);
        _fields.Add(text);

        return base.VisitMember(node);
    }
}