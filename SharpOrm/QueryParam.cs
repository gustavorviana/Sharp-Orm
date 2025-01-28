using SharpOrm.DataTranslation;
using System;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace SharpOrm
{
    /// <summary>
    /// Represents a query parameter for database commands.
    /// </summary>
    public class QueryParam
    {
        /// <summary>
        /// Gets or sets the translation registry used for data translation.
        /// </summary>
        public TranslationRegistry Translation { get; set; }

        private DbParameter param;

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name { get; }

        private object _value;

        /// <summary>
        /// Gets the value of the parameter.
        /// </summary>
        public object Value
        {
            get
            {
                if (Direction == ParameterDirection.Output || (Direction == ParameterDirection.InputOutput && param != null))
                    return GetTranslationRegistry().FromSql(param?.Value);

                return _value;
            }
        }

        /// <summary>
        /// Gets the direction of the parameter.
        /// </summary>
        public ParameterDirection Direction { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryParam"/> class with the specified name and direction.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="direction">The direction of the parameter.</param>
        public QueryParam(string name, ParameterDirection direction) : this(name, null, direction)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryParam"/> class with the specified name, value, and direction.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="direction">The direction of the parameter.</param>
        public QueryParam(string name, object value, ParameterDirection direction = ParameterDirection.Input)
        {
            Name = name.StartsWith("@") ? name : $"@{name}";
            _value = value;
        }

        /// <summary>
        /// Initializes the parameter for the specified command.
        /// </summary>
        /// <param name="cmd">The database command.</param>
        /// <returns>The initialized database parameter.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the command is null.</exception>
        internal DbParameter Init(DbCommand cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));

            var foundParam = GetConfigured(cmd);
            if (foundParam != null)
                return foundParam;

            var param = cmd.CreateParameter();
            param.ParameterName = Name;

            if (_value != null)
                param.Value = GetTranslationRegistry().ToSql(_value);

            param.Direction = Direction;

            if (Direction == ParameterDirection.Output || Direction == ParameterDirection.InputOutput)
                this.param = param;

            cmd.Parameters.Add(param);

            return param;
        }

        private DbParameter GetConfigured(DbCommand command)
        {
            return command.Parameters.OfType<DbParameter>().FirstOrDefault(x => x.ParameterName == Name);
        }

        private TranslationRegistry GetTranslationRegistry()
        {
            return Translation ?? TranslationRegistry.Default;
        }
    }
}
