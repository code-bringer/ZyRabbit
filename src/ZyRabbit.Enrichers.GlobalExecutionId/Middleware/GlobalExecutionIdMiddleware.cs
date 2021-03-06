﻿using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZyRabbit.Pipe;
using ZyRabbit.Pipe.Middleware;

namespace ZyRabbit.Enrichers.GlobalExecutionId.Middleware
{
	public class GlobalExecutionOptions
	{
		public Func<IPipeContext, string> IdFunc { get; set; }
		public Action<IPipeContext, string> PersistAction { get; set; }
	}

	public class GlobalExecutionIdMiddleware : StagedMiddleware
	{
		public override string StageMarker => Pipe.StageMarker.Initialized;
		protected Func<IPipeContext, string> IdFunc;
		protected Action<IPipeContext, string> PersistAction;

		protected static readonly AsyncLocal<string> ExecutionId = new AsyncLocal<string>();
		private readonly ILogger<GlobalExecutionIdMiddleware> Logger;

		public GlobalExecutionIdMiddleware(ILogger<GlobalExecutionIdMiddleware> logger, GlobalExecutionOptions options = null)
		{
			IdFunc = options?.IdFunc ?? (context => context.GetGlobalExecutionId());
			PersistAction = options?.PersistAction ?? ((context, id) => context.Properties.TryAdd(PipeKey.GlobalExecutionId, id));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var fromContext = GetExecutionIdFromContext(context);
			if (!string.IsNullOrWhiteSpace(fromContext))
			{
				Logger.LogInformation("GlobalExecutionId {globalExecutionId} was allready found in PipeContext.", fromContext);
				return Next.InvokeAsync(context, token);
			}
			var fromProcess = GetExecutionIdFromProcess();
			if (!string.IsNullOrWhiteSpace(fromProcess))
			{
				Logger.LogInformation("Using GlobalExecutionId {globalExecutionId} that was found in the execution process.", fromProcess);
				PersistAction(context, fromProcess);
				return Next.InvokeAsync(context, token);
			}
			var created = CreateExecutionId(context);
			Logger.LogInformation("Creating new GlobalExecutionId {globalExecutionId} for this execution.", created);
			PersistAction(context, created);
			return Next.InvokeAsync(context, token);
		}

		protected virtual string CreateExecutionId(IPipeContext context)
		{
			var executionId = Guid.NewGuid().ToString();
			SaveIdInProcess(executionId);
			return executionId;
		}

		protected virtual string GetExecutionIdFromProcess()
		{
			string executionId = ExecutionId?.Value;
			return executionId;
		}

		protected virtual string GetExecutionIdFromContext(IPipeContext context)
		{
			var id = IdFunc(context);
			if (!string.IsNullOrWhiteSpace(id))
			{
				SaveIdInProcess(id);
			}
			return id;
		}

		protected virtual void SaveIdInProcess(string executionId)
		{
			ExecutionId.Value = executionId;
		}
	}
}
