namespace SharpOrm.DataTranslation
{    /// <summary>
     /// Represents a specialized translator that handles nullable types in addition to regular types.
     /// This marker interface extends <see cref="ISqlTranslation"/> to indicate support for nullable value types.
     /// Doc: https://github.com/gustavorviana/Sharp-Orm/wiki/Custom-SQL-Translation
     /// </summary>
     /// <remarks>
     /// Implementations of this interface are expected to handle both the underlying type and its nullable variant.
     /// For example, a translator that returns <c>true</c> for <see cref="ISqlTranslation.CanWork"/> with <see cref="int"/> 
     /// should also handle <see cref="System.Nullable{T}"/> where T is <see cref="int"/>.
     /// </remarks>
    public interface INullableSqlTranslation : ISqlTranslation
    {
    }
}
