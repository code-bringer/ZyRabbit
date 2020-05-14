﻿using ZyRabbit.Pipe;

namespace ZyRabbit.Enrichers.HttpContext
{
	public static class PipeContextHttpExtensions
	{
		public const string HttpContext = "HttpContext";

#if NETSTANDARD2_0
		public static IPipeContext UseHttpContext(this IPipeContext pipeContext, Microsoft.AspNetCore.Http.HttpContext httpContext)
		{
			pipeContext.Properties.AddOrReplace(HttpContext, httpContext);
			return pipeContext;
		}

		public static Microsoft.AspNetCore.Http.HttpContext GetHttpContext(this IPipeContext pipeContext)
		{
			return pipeContext.Get<Microsoft.AspNetCore.Http.HttpContext>(HttpContext);
		}
#endif

#if NET451
		public static IPipeContext UseHttpContext(this IPipeContext pipeContext, System.Web.HttpContext httpContext)
		{
			pipeContext.Properties.AddOrReplace(HttpContext, httpContext);
			return pipeContext;
		}

		public static System.Web.HttpContext GetHttpContext(this IPipeContext pipeContext)
		{
			return pipeContext.Get<System.Web.HttpContext>(HttpContext);
		}
#endif
	}
}
