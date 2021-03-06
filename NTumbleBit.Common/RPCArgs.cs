﻿using NBitcoin;
using NBitcoin.RPC;
using NTumbleBit.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace NTumbleBit.Common
{
	public class RPCArgs
	{
		public Uri Url
		{
			get; set;
		}
		public string User
		{
			get; set;
		}
		public string Password
		{
			get; set;
		}
		public string CookieFile
		{
			get; set;
		}
		public RPCClient ConfigureRPCClient(Network network)
		{
			RPCClient rpcClient = null;
			var url = Url;
			var usr = User;
			var pass = Password;
			if(url != null && usr != null && pass != null)
				rpcClient = new RPCClient(new System.Net.NetworkCredential(usr, pass), url, network);
			if(rpcClient == null)
			{
				if(url != null && CookieFile != null)
				{
					try
					{

						rpcClient = new RPCClient(File.ReadAllText(CookieFile), url, network);
					}
					catch(IOException)
					{
						Logs.Configuration.LogWarning("RPC Cookie file not found at " + CookieFile);
					}
				}

				if(rpcClient == null)
				{
					try
					{
						rpcClient = new RPCClient(network);
					}
					catch { }
					if(rpcClient == null)
					{
						Logs.Configuration.LogError("RPC connection settings not configured");
						throw new ConfigException();
					}
				}
			}

			Logs.Configuration.LogInformation("Testing RPC connection to " + rpcClient.Address.AbsoluteUri);
			try
			{
				rpcClient.SendCommand("whatever");
			}
			catch(RPCException ex)
			{
				if(ex.RPCCode != RPCErrorCode.RPC_METHOD_NOT_FOUND)
				{
					Logs.Configuration.LogError("Error connecting to RPC " + ex.Message);
					throw new ConfigException();
				}
			}
			catch(Exception ex)
			{
				Logs.Configuration.LogError("Error connecting to RPC " + ex.Message);
				throw new ConfigException();
			}
			Logs.Configuration.LogInformation("RPC connection successfull");

			if(rpcClient.GetBlockHash(0) != network.GenesisHash)
			{
				Logs.Configuration.LogError("The RPC server is not using the chain " + network.Name);
				throw new ConfigException();
			}
			var getInfo = rpcClient.SendCommand(RPCOperations.getinfo);
			var version = ((JObject)getInfo.Result)["version"].Value<int>();
			if(version < MIN_CORE_VERSION)
			{
				Logs.Configuration.LogError($"The minimum Bitcoin Core version required is {MIN_CORE_VERSION} (detected: {version})");
				throw new ConfigException();
			}
			Logs.Configuration.LogInformation($"Bitcoin Core version detected: {version}");
			return rpcClient;
		}

		const int MIN_CORE_VERSION = 130100;
		public static RPCClient ConfigureRPCClient(TextFileConfiguration confArgs, Network network, string prefix= null)
		{
			RPCArgs args = Parse(confArgs, network, prefix);
			return args.ConfigureRPCClient(network);
		}

		public static RPCArgs Parse(TextFileConfiguration confArgs, Network network, string prefix = null)
		{
			prefix = prefix ?? "";
			if(prefix != "")
			{
				if(!prefix.EndsWith("."))
					prefix += ".";
			}
			try
			{
				var url = confArgs.GetOrDefault<string>(prefix + "rpc.url", network == null ? null : "http://localhost:" + network.RPCPort + "/");
				return new RPCArgs()
				{
					User = confArgs.GetOrDefault<string>(prefix + "rpc.user", null),
					Password = confArgs.GetOrDefault<string>(prefix + "rpc.password", null),
					CookieFile = confArgs.GetOrDefault<string>(prefix + "rpc.cookiefile", null),
					Url = url == null ? null : new Uri(url)
				};
			}
			catch(FormatException)
			{
				throw new ConfigException("rpc.url is not an url");
			}
		}
	}
}
