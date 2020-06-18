using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Rsi.DependencyInjection
{
	/// <summary>
	/// Mock service registration extensions
	/// </summary>
	public static class ServiceProviderExtensions
	{
		/// <summary>
		/// Creates a new scope with mock service registrations
		/// </summary>
		/// <param name="serviceProvider">Current scope</param>
		/// <param name="servicesConfiguration">Mock services configuration delegate</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceScope CreateScope(this IServiceProvider serviceProvider, Action<IServiceCollection> servicesConfiguration)
		{
			if (serviceProvider == null)
				throw new ArgumentNullException(nameof(serviceProvider));
			
			return new NestedServiceScope(serviceProvider, servicesConfiguration);
		}

		/// <summary>
		/// Returns the current service provider with mock service registrations for the specified test host service provider
		/// </summary>
		/// <param name="serviceProvider">Test host service provider</param>
		/// <returns></returns>
		public static IServiceProvider GetCurrentScopeServiceProvider(this IServiceProvider serviceProvider)
		{
			var currentScope = NestedServiceScope.GetCurrentScopeByServiceProvider(serviceProvider);
			return currentScope == null ? serviceProvider : currentScope.ServiceProvider;
		}
		
		internal static object CreateInstance(this IServiceProvider serviceProvider, ServiceDescriptor descriptor)
		{
			if (descriptor.ImplementationInstance != null)
				return descriptor.ImplementationInstance;

			if (descriptor.ImplementationFactory != null)
				return descriptor.ImplementationFactory(serviceProvider);

			return ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, descriptor.ImplementationType);
		}

		public static Dictionary<Type, ServiceDescriptor> GetDescriptors(this IServiceProvider provider)
		{
			var result = new Dictionary<Type, ServiceDescriptor>();

			var p = provider is NestedServiceProvider ? ((NestedServiceProvider)provider).RootServiceProvider : provider;

			var engine = p.GetFieldValue("_engine");						

			var callSiteFactory = engine.GetPropertyValue("CallSiteFactory");
			var descriptorLookup = callSiteFactory.GetFieldValue("_descriptorLookup");

			if (descriptorLookup is IDictionary dictionary)
			{
				foreach (DictionaryEntry entry in dictionary)
					result.Add((Type)entry.Key, (ServiceDescriptor)entry.Value.GetPropertyValue("Last"));
			}

			return result;
		}

		private static object GetFieldValue(this object obj, string fieldName) => GetFieldInfo(obj.GetType(), fieldName)?.GetValue(obj);
		private static object GetPropertyValue(this object obj, string propertyName) => GetPropertyInfo(obj.GetType(), propertyName).GetValue(obj, null);
		private static FieldInfo GetFieldInfo(Type type, string fieldName)
		{
			FieldInfo fieldInfo;
			do
			{
				fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				type = type.BaseType;
			} while (fieldInfo == null && type != null);

			return fieldInfo;
		}

		private static PropertyInfo GetPropertyInfo(Type type, string propertyName)
		{
			PropertyInfo propertyInfo;
			do
			{
				propertyInfo = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				type = type.BaseType;
			} while (propertyInfo == null && type != null);

			return propertyInfo;
		}
	}
}