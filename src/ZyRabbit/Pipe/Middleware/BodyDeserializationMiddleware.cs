﻿using Microsoft.Extensions.Logging;
using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ZyRabbit.Serialization;

namespace ZyRabbit.Pipe.Middleware
{
	public class MessageDeserializationOptions
	{
		public Func<IPipeContext, Type> BodyTypeFunc { get; set; }
		public Func<IPipeContext, string> BodyContentTypeFunc { get; set; }
		public Func<IPipeContext, bool> ActivateContentTypeCheck{ get; set; }
		public Func<IPipeContext, byte[]> BodyFunc { get; set; }
		public Action<IPipeContext, object> PersistAction { get; set; }
	}

	public class BodyDeserializationMiddleware : Middleware
	{
		protected readonly ISerializer Serializer;
		protected Func<IPipeContext, Type> MessageTypeFunc;
		protected Func<IPipeContext, byte[]> BodyBytesFunc;
		protected Func<IPipeContext, string> BodyContentTypeFunc { get; set; }
		protected Func<IPipeContext, bool> ActivateContentTypeCheck { get; set; }
		protected Action<IPipeContext, object> PersistAction;
		protected readonly ILogger<BodyDeserializationMiddleware> Logger;

		public BodyDeserializationMiddleware(ISerializer serializer, ILogger<BodyDeserializationMiddleware> logger, MessageDeserializationOptions options = null)
		{
			Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			MessageTypeFunc = options?.BodyTypeFunc ?? (context => context.GetMessageType());
			BodyBytesFunc = options?.BodyFunc ?? (context =>context.GetDeliveryEventArgs()?.Body);
			PersistAction = options?.PersistAction ?? ((context, msg) => context.Properties.TryAdd(PipeKey.Message, msg));
			BodyContentTypeFunc = options?.BodyContentTypeFunc ?? (context => context.GetDeliveryEventArgs()?.BasicProperties.ContentType);
			ActivateContentTypeCheck = options?.ActivateContentTypeCheck ?? (context => context.GetContentTypeCheckActivated());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			if (ContentTypeCheckActivated(context))
			{
				var msgContentType = GetMessageContentType(context);
				if (!CanSerializeMessage(msgContentType))
				{
					throw new SerializationException($"Registered serializer supports {Serializer.ContentType}, received message uses {msgContentType}.");
				}
			}
			var message = GetMessage(context);
			SaveInContext(context, message);
			return Next.InvokeAsync(context, token);
		}

		protected virtual bool ContentTypeCheckActivated(IPipeContext context)
		{
			return ActivateContentTypeCheck?.Invoke(context) ?? false;
		}

		protected virtual bool CanSerializeMessage(string msgContentType)
		{
			if (string.IsNullOrEmpty(msgContentType))
			{
				Logger.LogDebug("Received message has no content type defined. Assuming it can be processed.");
				return true;
			}
			return string.Equals(msgContentType, Serializer.ContentType, StringComparison.CurrentCultureIgnoreCase);
		}

		protected virtual string GetMessageContentType(IPipeContext context)
		{
			return BodyContentTypeFunc?.Invoke(context);
		}

		protected virtual object GetMessage(IPipeContext context)
		{
			var bodyBytes = GetBodyBytes(context);
			var messageType = GetMessageType(context);
			return Serializer.Deserialize(messageType, bodyBytes);
		}

		protected virtual Type GetMessageType(IPipeContext context)
		{
			var msgType = MessageTypeFunc(context);
			if (msgType == null)
			{
				Logger.LogWarning("Unable to find message type in Pipe context.");
			}
			return msgType;
		}

		protected virtual byte[] GetBodyBytes(IPipeContext context)
		{
			var bodyBytes = BodyBytesFunc(context);
			if (bodyBytes == null)
			{
				Logger.LogWarning("Unable to find Body (bytes) in Pipe context");
			}
			return bodyBytes;
		}

		protected virtual void SaveInContext(IPipeContext context, object message)
		{
			if (PersistAction == null)
			{
				Logger.LogInformation("No persist action defined. Message will not be saved in Pipe context.");
			}
			PersistAction?.Invoke(context, message);
		}
	}

	public static class BodyDeserializationMiddlewareExtensions
	{
		private const string ContentTypeCheck = "Deserialization:ContentType:Check";

		public static TPipeContext UseContentTypeCheck<TPipeContext>(this TPipeContext context, bool check = true) where TPipeContext : IPipeContext
		{
			context.Properties.TryAdd(ContentTypeCheck, check);
			return context;
		}

		public static bool GetContentTypeCheckActivated(this IPipeContext context)
		{
			return context.Get<bool>(ContentTypeCheck);
		}
	}
}
