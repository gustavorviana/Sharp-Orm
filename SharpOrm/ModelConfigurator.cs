using SharpOrm.Builder.Tables;
using SharpOrm.DataTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharpOrm
{
    /// <summary>
    /// Defines methods for loading and configuring model mapper configurations.
    /// </summary>
    public interface IModelConfigurator
    {
        /// <summary>
        /// Loads all model mapper configurations from the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to scan for mapper configurations.</param>
        /// <returns>The current configurator instance for method chaining.</returns>
        IModelConfigurator LoadByAssembly(Assembly assembly);

        /// <summary>
        /// Loads model mapper configurations for a specific model type from the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to scan for mapper configurations.</param>
        /// <param name="modelType">The model type to load configurations for.</param>
        /// <returns>The current configurator instance for method chaining.</returns>
        IModelConfigurator LoadByAssembly(Assembly assembly, Type modelType);

        /// <summary>
        /// Loads a specific mapper configuration type.
        /// </summary>
        /// <param name="configuratorType">The type of the mapper configuration to load.</param>
        /// <returns>The current configurator instance for method chaining.</returns>
        IModelConfigurator Load(Type configuratorType);
    }

    /// <summary>
    /// Internal implementation of the model configurator that loads and applies model mapping configurations.
    /// </summary>
    internal class ModelConfigurator : IModelConfigurator
    {
        private HashSet<Type> _configurators = new HashSet<Type>();

        public IModelConfigurator LoadByAssembly(Assembly assembly)
        {
            var mapperConfigs = assembly
                .GetTypes()
                .Where(x => !x.IsAbstract && !x.IsInterface)
                .Where(ImplementsModelMapperConfiguration);

            foreach (var configType in mapperConfigs)
                Load(configType);

            return this;
        }

        public IModelConfigurator LoadByAssembly(Assembly assembly, Type modelType)
        {
            var mapperConfigs = assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => ImplementsModelMapperConfiguration(t, modelType));

            foreach (var configType in mapperConfigs)
                Load(configType);

            return this;
        }

        public IModelConfigurator Load(Type configuratorType)
        {
            _configurators.Add(configuratorType);
            return this;
        }

        private static bool ImplementsModelMapperConfiguration(Type type)
        {
            return type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IModelMapperConfiguration<>));
        }

        private static bool ImplementsModelMapperConfiguration(Type type, Type modelType)
        {
            return type.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IModelMapperConfiguration<>) &&
                i.GetGenericArguments()[0] == modelType);
        }

        public void Configure(TranslationRegistry registry)
        {

        }
    }
}
