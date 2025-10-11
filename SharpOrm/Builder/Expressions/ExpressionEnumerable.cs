using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpOrm.Builder.Expressions
{
    /// <summary>
    /// Provides enumeration capabilities for traversing member expressions in a lambda expression tree.
    /// Supports both single member access and anonymous type projections with multiple members.
    /// </summary>
    internal class ExpressionEnumerable : IEnumerable<MemberInfo>, IEnumerator<MemberInfo>
    {
        private Queue<MemberExpression> _memberExpressions;
        private Stack<MemberInfo> _currentPath;
        private readonly bool _allowNativeType;
        private LambdaExpression _expression;
        private bool _initialized;
        private bool _disposed;

        /// <summary>
        /// Gets the current element in the collection (non-generic implementation).
        /// </summary>
        object System.Collections.IEnumerator.Current => Current;

        /// <summary>
        /// Gets the current <see cref="MemberInfo"/> in the enumeration.
        /// </summary>
        public MemberInfo Current { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionEnumerable"/> class.
        /// </summary>
        /// <param name="expression">The lambda expression to enumerate.</param>
        /// <param name="allowNativeType">Indicates whether native types are allowed in member access.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
        public ExpressionEnumerable(LambdaExpression expression, bool allowNativeType)
        {
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
            _allowNativeType = allowNativeType;
        }

        /// <summary>
        /// Advances the enumerator to the next member in the current path.
        /// </summary>
        /// <returns><c>true</c> if the enumerator successfully advanced; otherwise, <c>false</c>.</returns>
        public bool MoveNext()
        {
            var hasNext = HasNext();

            Current = hasNext ? _currentPath.Pop() : null;

            return hasNext;
        }

        /// <summary>
        /// Determines whether there are more members in the current path.
        /// </summary>
        /// <returns><c>true</c> if there are more members; otherwise, <c>false</c>.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the object has been disposed.</exception>
        public bool HasNext()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (!_initialized)
                Initialize();

            return _currentPath?.Count > 0;
        }

        /// <summary>
        /// Advances to the next member expression path in the collection.
        /// </summary>
        /// <returns><c>true</c> if successfully moved to the next path; otherwise, <c>false</c>.</returns>
        public bool MoveNextPath()
        {
            var hasNext = HasNextPath();

            _currentPath = hasNext ? ExtractMemberPath(_memberExpressions.Dequeue()) : null;
            Current = null;

            return hasNext;
        }

        /// <summary>
        /// Determines whether there are more member expression paths to enumerate.
        /// </summary>
        /// <returns><c>true</c> if there are more paths; otherwise, <c>false</c>.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the object has been disposed.</exception>
        public bool HasNextPath()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (!_initialized)
                Initialize();

            return _memberExpressions?.Count > 0;
        }

        private void Initialize()
        {
            _memberExpressions = ExtractMemberExpressions(_expression);
            _initialized = true;
        }

        /// <summary>
        /// Extracts all member expressions from a lambda expression.
        /// Supports both single member access and anonymous type projections.
        /// </summary>
        /// <param name="expression">The lambda expression to extract members from.</param>
        /// <returns>A queue containing all member expressions found.</returns>
        /// <exception cref="ArgumentException">Thrown when an argument in an anonymous type is not a member access expression.</exception>
        /// <exception cref="NotSupportedException">Thrown when the expression type is not supported.</exception>
        internal static Queue<MemberExpression> ExtractMemberExpressions(LambdaExpression expression)
        {
            var body = UnwrapUnaryExpression(expression.Body);
            var items = new Queue<MemberExpression>();

            if (body is NewExpression newExpression)
            {
                foreach (var argument in newExpression.Arguments)
                {
                    var memberExpression = UnwrapUnaryExpression(argument) as MemberExpression;
                    if (memberExpression == null)
                        throw new ArgumentException("Each property in the anonymous type must be a member access expression");

                    items.Enqueue(memberExpression);
                }
            }
            else if (body is MemberExpression memberExpression)
            {
                items.Enqueue(memberExpression);
            }
            else
            {
                throw new NotSupportedException(string.Format("Expression type {0} is not supported", body.GetType().Name));
            }

            return items;
        }

        /// <summary>
        /// Extracts the full member access path from a member expression, creating a stack from leaf to root.
        /// </summary>
        /// <param name="memberExpression">The member expression to extract the path from.</param>
        /// <returns>A stack containing the member path, with the root member at the bottom.</returns>
        private Stack<MemberInfo> ExtractMemberPath(MemberExpression memberExpression)
        {
            if (!_allowNativeType)
                SqlExpressionVisitor.ValidateMemberType(memberExpression.Member);

            var members = new Stack<MemberInfo>();

            while (memberExpression != null)
            {
                members.Push(memberExpression.Member);
                memberExpression = memberExpression.Expression as MemberExpression;
            }

            return members;
        }

        /// <summary>
        /// Unwraps a unary expression to reveal the underlying member expression if present.
        /// </summary>
        /// <param name="expression">The expression to unwrap.</param>
        /// <returns>The unwrapped expression, or the original expression if no unwrapping is needed.</returns>
        internal static Expression UnwrapUnaryExpression(Expression expression)
        {
            var unaryExpression = expression as UnaryExpression;
            if (unaryExpression != null && unaryExpression.Operand is MemberExpression)
                return unaryExpression.Operand;

            return expression;
        }

        /// <summary>
        /// Resets the enumerator to its initial state, clearing all internal collections.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the object has been disposed.</exception>
        public void Reset()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            _currentPath?.Clear();
            _memberExpressions?.Clear();

            Current = null;
            _initialized = false;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ExpressionEnumerable"/>.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _expression = null;
            _currentPath?.Clear();
            _memberExpressions?.Clear();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        public IEnumerator<MemberInfo> GetEnumerator() => this;

        /// <summary>
        /// Returns an enumerator that iterates through the collection (non-generic implementation).
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this;
    }
}