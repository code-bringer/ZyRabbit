﻿using System;
using System.Collections.Generic;
using System.Reflection;
using RawRabbit.Configuration;

namespace RawRabbit.Common
{
	public interface IClientPropertyProvider
	{
		IDictionary<string, object> GetClientProperties(RawRabbitConfiguration cfg = null, BrokerConfiguration brokerCfg = null);
	}

	public class ClientPropertyProvider : IClientPropertyProvider
	{
		public IDictionary<string, object> GetClientProperties(RawRabbitConfiguration cfg = null, BrokerConfiguration brokerCfg = null)
		{
			var props = new Dictionary<string, object>
			{
				{ "product", "RawRabbit" },
				{ "version", typeof(BusClient).Assembly.GetName().Version.ToString() },
				{ "platform", ".NET" },
				{ "client_directory", typeof(BusClient).Assembly.CodeBase},
				{ "client_server", Environment.MachineName },
			};
			if (brokerCfg != null)
			{
				props.Add("broker_username", brokerCfg.Username);
			}
			if (cfg != null)
			{
				props.Add("request_timeout", cfg.RequestTimeout.ToString("g"));
			}

			return props;
		}
	}
}
