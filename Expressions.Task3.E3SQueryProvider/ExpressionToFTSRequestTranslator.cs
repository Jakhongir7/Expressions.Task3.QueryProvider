using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Where")
            {
                var predicate = node.Arguments[1];
                Visit(predicate);
                return node;
            }

            if (node.Method.DeclaringType == typeof(string))
            {
                if (node.Method.Name == "Equals" && node.Arguments.Count == 1)
                {
                    Visit(node.Object);
                    _resultStringBuilder.Append("(");
                    Visit(node.Arguments[0]);
                    _resultStringBuilder.Append(")");
                    return node;
                }

                Visit(node.Object);
                _resultStringBuilder.Append("(");

                if (node.Method.Name == "StartsWith")
                {
                    Visit(node.Arguments[0]);
                    _resultStringBuilder.Append("*)");
                }
                else if (node.Method.Name == "EndsWith")
                {
                    _resultStringBuilder.Append("*");
                    Visit(node.Arguments[0]);
                    _resultStringBuilder.Append(")");
                }
                else if (node.Method.Name == "Contains")
                {
                    _resultStringBuilder.Append("*");
                    Visit(node.Arguments[0]);
                    _resultStringBuilder.Append("*)");
                }
                else
                {
                    throw new NotSupportedException($"Method '{node.Method.Name}' is not supported");
                }

                return node;
            }

            return base.VisitMethodCall(node);
        }


        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    VisitBinaryEqual(node);
                    break;

                case ExpressionType.AndAlso:
                    Visit(node.Left);
                    _resultStringBuilder.Append(" AND ");
                    Visit(node.Right);
                    break;

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            }

            return node;
        }

        private void VisitBinaryEqual(BinaryExpression node)
        {
            if (node.Left.NodeType == ExpressionType.MemberAccess && node.Right.NodeType == ExpressionType.Constant)
            {
                Visit(node.Left);
                _resultStringBuilder.Append("(");
                Visit(node.Right);
                _resultStringBuilder.Append(")");
            }
            else if (node.Left.NodeType == ExpressionType.Constant && node.Right.NodeType == ExpressionType.MemberAccess)
            {
                Visit(node.Right);
                _resultStringBuilder.Append("(");
                Visit(node.Left);
                _resultStringBuilder.Append(")");
            }
            else
            {
                throw new NotSupportedException($"Unsupported operands in expression: {node}");
            }
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);

            return node;
        }

        #endregion
    }
}
