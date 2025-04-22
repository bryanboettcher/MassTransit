namespace MassTransit.DapperIntegration.Saga
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq.Expressions;
    using System.Reflection;
    using Internals;


    public static class SqlExpressionVisitor
    {
        public static List<SqlPredicate> CreateFromExpression(Expression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Lambda:
                    return LambdaVisit((LambdaExpression)node);
                case ExpressionType.AndAlso:
                    return AndAlsoVisit((BinaryExpression)node);
                case ExpressionType.Not:
                    return NegatedVisit((UnaryExpression)node);
                case ExpressionType.NotEqual:
                    return ComparisonVisit((BinaryExpression)node, "<>");
                case ExpressionType.Equal:
                    return ComparisonVisit((BinaryExpression)node, "=");
                case ExpressionType.LessThan:
                    return ComparisonVisit((BinaryExpression)node, "<");
                case ExpressionType.LessThanOrEqual:
                    return ComparisonVisit((BinaryExpression)node, "<=");
                case ExpressionType.GreaterThan:
                    return ComparisonVisit((BinaryExpression)node, ">");
                case ExpressionType.GreaterThanOrEqual:
                    return ComparisonVisit((BinaryExpression)node, ">=");
                case ExpressionType.MemberAccess:
                    return MemberAccessVisit((MemberExpression)node);
                default:
                    throw new Exception("Node type not supported.");
            }
        }

        static List<SqlPredicate> LambdaVisit(LambdaExpression node)
        {
            return CreateFromExpression(node.Body);
        }

        static List<SqlPredicate> AndAlsoVisit(BinaryExpression node)
        {
            var result = new List<SqlPredicate>();

            result.AddRange(CreateFromExpression(node.Left));
            result.AddRange(CreateFromExpression(node.Right));

            return result;
        }

        static List<SqlPredicate> ComparisonVisit(BinaryExpression node, string op)
        {
            var left = (MemberExpression)node.Left;

            var name = left.Member.GetCustomAttribute<ColumnAttribute>()?.Name ?? left.Member.Name;

            object value;

            if (node.Right is ConstantExpression right)
                value = right.Value;
            else
                value = Expression.Lambda<Func<object>>(Expression.Convert(node.Right, typeof(object))).CompileFast().Invoke();

            return [new(name, value, op)];
        }

        static List<SqlPredicate> NegatedVisit(UnaryExpression node)
        {
            var property = (MemberExpression) node.Operand;
            var name = property.Member.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Member.Name;

            if (node.Type != typeof(bool))
                throw new InvalidOperationException("Negation is only supported for boolean properties");
            
            return [new(name, false)];
        }

        static List<SqlPredicate> MemberAccessVisit(MemberExpression node)
        {
            var name = node.Member.GetCustomAttribute<ColumnAttribute>()?.Name ?? node.Member.Name;
            object value;

            if (node.Type == typeof(bool))
                value = true; // No support for Not yet.
            else if (node.Type.IsValueType)
                value = Activator.CreateInstance(node.Type);
            else
                value = null;

            return [new(name, value)];
        }
    }


    public class SqlPredicate
    {
        public SqlPredicate(string name, object value, string @operator = "=")
        {
            Name = name;
            Operator = @operator;
            Value = value;
        }

        public string Name { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
    }
}
