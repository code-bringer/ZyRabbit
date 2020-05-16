﻿using System.Threading;

#if NET451
using System.Runtime.Remoting.Messaging;
#endif

namespace ZyRabbit.Enrichers.GlobalExecutionId.Dependencies
{
	public class GlobalExecutionIdRepository
	{
#if NETSTANDARD2_0
		private static readonly AsyncLocal<string> GlobalExecutionId = new AsyncLocal<string>();
#elif NET451
		protected const string GlobalExecutionId = "ZyRabbit:GlobalExecutionId";
#endif

		public static string Get()
		{
#if NETSTANDARD2_0
			return GlobalExecutionId?.Value;
#elif NET451
			return CallContext.LogicalGetData(GlobalExecutionId) as string;
#endif
		}

		public static void Set(string id)
		{
#if NETSTANDARD2_0
			GlobalExecutionId.Value = id;
#elif NET451
			CallContext.LogicalSetData(GlobalExecutionId, id);
#endif
		}
	}
}
