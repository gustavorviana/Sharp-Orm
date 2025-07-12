using System;

namespace SharpOrm.Builder.Grammars.Table.Constraints
{
    public abstract class ConstraintBuilder<T> : ConstraintBuilder where T : Constraint
    {
        private static readonly Type _type = typeof(T);

        public override bool CanWork(Type type) => type == _type;

        public override SqlExpression Build(Constraint constraint)
        {
            return Build((T)constraint);
        }

        protected abstract SqlExpression Build(T constraint);
    }

    /// <summary>
    /// Interface for building SQL constraint statements.
    /// </summary>
    public abstract class ConstraintBuilder : ICanWork<Type>
    {
        /// <summary>
        /// Builds the SQL expression for the constraint.
        /// </summary>
        /// <param name="constraint">The constraint to build.</param>
        /// <returns>The SQL expression representing the constraint.</returns>
        public abstract SqlExpression Build(Constraint constraint);
        public abstract bool CanWork(Type type);
    }
}
