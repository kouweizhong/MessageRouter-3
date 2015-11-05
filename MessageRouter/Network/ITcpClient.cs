﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace MessageRouter.Network
{
	public interface ITcpClient : IDisposable
	{
		Stream ReadStream { get; }

		Stream WriteStream { get; }

	    Task ConnectAsync(string userId);

		Task DisconnectAsync();
	}
}
