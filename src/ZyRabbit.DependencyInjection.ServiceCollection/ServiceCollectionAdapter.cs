﻿using System;
using Microsoft.Extensions.DependencyInjection;

namespace ZyRabbit.DependencyInjection.ServiceCollection
{
	public class ServiceCollectionAdapter : IDependencyRegister
	{
		private readonly IServiceCollection _collection;

		public ServiceCollectionAdapter(IServiceCollection collection)
		{
			_collection = collection ?? throw new ArgumentNullException(nameof(collection));
		}

		public IDependencyRegister AddTransient<TService, TImplementation>() where TImplementation : class, TService where TService : class
		{
			_collection.AddTransient<TService, TImplementation>();
			return this;
		}

		public IDependencyRegister AddTransient<TService>(Func<IDependencyResolver, TService> instanceCreator) where TService : class
		{
			_collection.AddTransient(c => instanceCreator(new ServiceProviderAdapter(c)));
			return this;
		}

		public IDependencyRegister AddTransient<TService, TImplementation>(Func<IDependencyResolver, TImplementation> instanceCreator) where TService : class where TImplementation : class, TService
		{
			_collection.AddTransient<TService, TImplementation>(c => instanceCreator(new ServiceProviderAdapter(c)));
			return this;
		}

		public IDependencyRegister AddSingleton<TService>(TService instance) where TService : class
		{
			_collection.AddSingleton(instance);
			return this;
		}

		public IDependencyRegister AddSingleton<TService, TImplementation>(Func<IDependencyResolver, TService> instanceCreator) where TImplementation : class, TService where TService : class
		{
			_collection.AddSingleton(c => instanceCreator(new ServiceProviderAdapter(c)));
			return this;
		}

		public IDependencyRegister AddSingleton<TService>(Func<IDependencyResolver, TService> instanceCreator) where TService : class
		{
			_collection.AddSingleton<TService>(c => instanceCreator(new ServiceProviderAdapter(c)));
			return this;
		}

		public IDependencyRegister AddSingleton<TService, TImplementation>() where TImplementation : class, TService where TService : class
		{
			_collection.AddSingleton<TService, TImplementation>();
			return this;
		}

		public IDependencyRegister AddTransient(Type type, Func<IDependencyResolver, Type, object> instanceCreator)
		{
			_collection.AddTransient(type, c => instanceCreator(new ServiceProviderAdapter(c), type));
			return this;
		}

		public IDependencyRegister AddSingleton(Type type, Func<IDependencyResolver, Type, object> instanceCreator)
		{
			_collection.AddSingleton(type, c => instanceCreator(new ServiceProviderAdapter(c), type));
			return this;
		}

		public IDependencyRegister AddSingleton(Type type, Type implementationType)
		{
			_collection.AddSingleton(type, implementationType);
			return this;
		}
	}
}
