﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ZyRabbit.DependencyInjection
{
	public class SimpleDependencyInjection : IDependencyRegister, IDependencyResolver
	{
		private readonly Dictionary<Type, Func<IDependencyResolver, Type, object>> _registrations = new Dictionary<Type, Func<IDependencyResolver, Type, object>>();

		public IDependencyRegister AddTransient<TService, TImplementation>(Func<IDependencyResolver, TImplementation> instanceCreator) where TService : class where TImplementation : class, TService
		{
			if (instanceCreator == null)
				throw new ArgumentNullException(nameof(instanceCreator));

			_registrations[typeof(TService)] = (resolver, type) => instanceCreator(resolver);
			return this;
		}

		public IDependencyRegister AddTransient<TService, TImplementation>() where TImplementation : class, TService where TService : class
		{
			AddTransient<TService, TImplementation>(resolver => GetService(typeof(TImplementation)) as TImplementation);
			return this;
		}

		public IDependencyRegister AddSingleton<TService>(TService instance) where TService : class
		{
			AddTransient<TService, TService>(resolver => instance);
			return this;
		}

		public IDependencyRegister AddSingleton<TService, TImplementation>(Func<IDependencyResolver, TService> instanceCreator) where TImplementation : class, TService where TService : class
		{
			if (instanceCreator == null)
				throw new ArgumentNullException(nameof(instanceCreator));

			var lazy = new Lazy<TImplementation>(() => (TImplementation)instanceCreator(this));
			AddTransient<TService,TImplementation>(resolver => lazy.Value);
			return this;
		}

		public IDependencyRegister AddSingleton<TService, TImplementation>() where TImplementation : class, TService where TService : class
		{
			var lazy = new Lazy<TImplementation>(() =>
			{
				var type = typeof(TImplementation);
				return (TImplementation)CreateInstance(type, Enumerable.Empty<object>());
			});
			AddTransient<TService, TImplementation>(resolver => lazy.Value);
			return this;
		}

		public TService GetService<TService>(params object[] additional)
		{
			return (TService)GetService(typeof(TService), additional);
		}

		private enum SearchResult
		{
			NotFound,
			Activation,
			Factory
		}

		private (Type, SearchResult) FindRegistrationKey(Type type)
		{
			if (_registrations.ContainsKey(type))
			{
				return (type, SearchResult.Factory);
			}

			var typeInfo = type.GetTypeInfo();
			if (!typeInfo.IsAbstract)
			{
				return (type, SearchResult.Activation);
			}

			if (typeInfo.IsAbstract && typeInfo.IsConstructedGenericType)
			{
				var typeDef = typeInfo.GetGenericTypeDefinition();
				if (_registrations.ContainsKey(typeDef))
				{
					return (typeDef, SearchResult.Factory);
				}
			}

			return (null, SearchResult.NotFound);
		}

		public object GetService(Type serviceType, params object[] additional)
		{
			if (serviceType == null)
				throw new ArgumentNullException(nameof(serviceType));

			var (type, result) = FindRegistrationKey(serviceType);
			switch (result)
			{
				case SearchResult.Activation:
					return CreateInstance(type, additional);
				case SearchResult.Factory:
					return _registrations[type](this, serviceType);
				default:
					throw new InvalidOperationException("No registration for " + serviceType);
			}
		}

		private bool TryGetService(Type serviceType, out object service, params object[] additional)
		{
			var (type, result) = FindRegistrationKey(serviceType);
			switch (result)
			{
				case SearchResult.Activation:
					service = CreateInstance(type, additional);
					return true;
				case SearchResult.Factory:
					service = _registrations[type](this, serviceType);
					return true;
				default:
					service = null;
					return false;
			}
		}

		private object CreateInstance(Type implementationType, IEnumerable<object> additional)
		{
			var additionalTypes = additional.Select(a => a.GetType());
			var ctors = implementationType
				.GetConstructors();
			var ctor = ctors
				.Where(c => c.GetParameters().All(p => {
					if (p.Attributes.HasFlag(ParameterAttributes.Optional) || additionalTypes.Contains(p.ParameterType))
						return true;
					var (_, result) = FindRegistrationKey(p.ParameterType);
					return result != SearchResult.NotFound;
				}))
				.OrderByDescending(c => c.GetParameters().Length)
				.FirstOrDefault();
			if (ctor == null)
			{
				throw new Exception($"Unable to find suitable constructor for {implementationType.Name}.");
			}
			var dependencies = ctor
				.GetParameters()
				.Select(parameter =>
				{
					if (additionalTypes.Contains(parameter.ParameterType))
					{
						return additional.First(a => a.GetType() == parameter.ParameterType);
					}
					object service;
					return TryGetService(parameter.ParameterType, out service) ? service : null;
				})
				.ToArray();
			return ctor.Invoke(dependencies);
		}

		private readonly ConcurrentDictionary<Type, object> _singletonInstances = new ConcurrentDictionary<Type, object>();

		public IDependencyRegister AddSingleton(Type type, Type implementationType)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (implementationType == null)
				throw new ArgumentNullException(nameof(implementationType));

			_registrations[type] = (resolver, requestedType) => {
				var result = _singletonInstances.GetOrAdd(requestedType, _ =>
				{
					if (implementationType.IsGenericTypeDefinition)
					{						
						var ts = implementationType.MakeGenericType(requestedType.GetGenericArguments());
						return CreateInstance(ts, Enumerable.Empty<object>());
					}

					return CreateInstance(requestedType, Enumerable.Empty<object>());
				});
				return result;
			};
			return this;
		}
	}
}
