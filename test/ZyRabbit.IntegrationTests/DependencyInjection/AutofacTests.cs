﻿using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using ZyRabbit.Common;
using ZyRabbit.Configuration;
using ZyRabbit.DependencyInjection.Autofac;
using ZyRabbit.Instantiation;
using ZyRabbit.IntegrationTests.TestMessages;
using Xunit;
using Microsoft.Extensions.Logging;
using ZyRabbit.Operations.StateMachine.Middleware;

namespace ZyRabbit.IntegrationTests.DependencyInjection
{
	public sealed class AutofacTests
	{
		[Fact]
		public async Task Should_Be_Able_To_Publish_Message_From_Resolved_Client()
		{
			/* Setup */
			var builder = new ContainerBuilder();
			builder.RegisterZyRabbit();
			using var container = builder.Build();

			/* Test */
			var client = container.Resolve<IBusClient>();
			await client.PublishAsync(new BasicMessage());
			await client.DeleteExchangeAsync<BasicMessage>();
			var disposer = container.Resolve<IResourceDisposer>();

			/* Assert */
			disposer.Dispose();
		}

		[Fact]
		public async Task Should_Honor_Client_Configuration()
		{
			/* Setup */
			var builder = new ContainerBuilder();
			var config = ZyRabbitConfiguration.Local;
			config.VirtualHost = "/foo";

			/* Test */
			await Assert.ThrowsAsync<DependencyResolutionException>(async () =>
			{
				builder.RegisterZyRabbit(new ZyRabbitOptions
				{
					ClientConfiguration = config
				});
				using var container = builder.Build();
				var client = container.Resolve<IBusClient>();
				await client.CreateChannelAsync();
			});
		}

		[Fact]
		public void Should_Be_Able_To_Resolve_Client_With_Plugins_From_Autofac()
		{
			/* Setup */
			var builder = new ContainerBuilder();
			builder.RegisterZyRabbit(new ZyRabbitOptions
			{
				Plugins = p => p.UseStateMachine()
			});
			using var container = builder.Build();

			/* Test */
			var client = container.Resolve<IBusClient>();
			var middleware = container.Resolve<RetrieveStateMachineMiddleware>();

			/* Assert */
			Assert.NotNull(client);
			Assert.NotNull(middleware);
		}

		[Fact]
		public void Should_Be_Able_To_Resolve_Logger()
		{
			/* Setup */
			var builder = new ContainerBuilder();
			builder.RegisterZyRabbit();
			using var container = builder.Build();

			/* Test */
			var logger1 = container.Resolve<ILogger<IExclusiveLock>>();
			var logger2 = container.Resolve<ILogger<IExclusiveLock>>();
			Assert.Same(logger1, logger2);
			Assert.NotNull(logger1);
		}
	}
}
