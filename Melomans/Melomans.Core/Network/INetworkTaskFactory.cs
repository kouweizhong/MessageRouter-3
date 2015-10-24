﻿using System;
using Melomans.Core.Message;
using Melomans.Core.Models;
using Sockets.Plugin;

namespace Melomans.Core.Network
{
	public interface INetworkTaskFactory
	{
		INetworkTask<TMessage> CreateMulticastTask<TMessage>(TMessage message, UdpSocketMulticastClient client)
			where TMessage: class, IMessage;

		INetworkTask<TMessage> CreateAddressTask<TMessage>(Meloman meloman, TMessage message)
			where TMessage : class, IMessage;

		INetworkTask<TMessage> CreateMulticastReaderTask<TMessage>() 
			where TMessage : class, IMessage;

		INetworkTask<TMessage> CreateAddressReaderTask<TMessage>()
			where TMessage : class, IMessage;
	}
}