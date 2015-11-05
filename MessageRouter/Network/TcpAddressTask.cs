﻿using System;
using System.IO;
using System.ServiceModel.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageRouter.Message;

namespace MessageRouter.Network
{
	public class TcpAddressTask<TMessage> : NetworkTaskBase<TMessage>, INetworkTask<TMessage>
		where TMessage:class, IMessage
	{
		private readonly string _userId;
		private readonly INetworkClientFactory _clientFactory;
		private readonly TMessage _message;
		private readonly IMessageSerializer _serializer;
		private readonly IMessageService _messageService;

		public TcpAddressTask(string userId, 
			INetworkClientFactory clientFactory,
			TMessage message,
			IMessageSerializer serializer,
			IMessageService messageService)
		{
		    _userId = userId;
			_clientFactory = clientFactory;
			_message = message;
			_serializer = serializer;
			_messageService = messageService;
			
		}

		private async Task<bool> GetResponse(Stream readStream, CancellationToken cancellationToken)
		{
			MemoryStream memoryBuffer = null;
			try
			{
				var buffer = new byte[1024];
			    memoryBuffer = await ToMemoryStream(cancellationToken, readStream);
				if (IsCancellationRequested)
					throw new OperationCanceledException();

				NetworkState responseCode;
				if (!Enum.TryParse(Encoding.UTF8.GetString(memoryBuffer.ToArray(), 0, (int) memoryBuffer.Length), out responseCode))
					throw new InvalidDataException();

				switch (responseCode)
				{
					case NetworkState.AccessDenied:
						throw new SecurityAccessDeniedException(string.Format("Access denied for type message {0}", typeof (TMessage)));
					case NetworkState.Error:
						throw new IOException();
				}
				return true;
			}
			finally
			{
				if(memoryBuffer != null)
					memoryBuffer.Dispose();
			}
		}

		protected async override Task Run(CancellationToken cancellationToken)
		{
			ITcpClient client = null;
			try
			{
				if (!_messageService.CanSend(_userId, typeof (TMessage)))
					throw new SecurityAccessDeniedException(string.Format("Access denied for type message {0}", typeof (TMessage)));
                client = _clientFactory.CreateTcpClient();
				await client.ConnectAsync(_userId);
				var definition = _messageService.GetDefinition<TMessage>();
				var buffer = BitConverter.GetBytes(_messageService.CreateMessageHash(definition));
				await client.WriteStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
				await client.WriteStream.FlushAsync(cancellationToken);
				if(!await GetResponse(client.ReadStream, cancellationToken))
					return;
				if (IsCancellationRequested)
					throw new OperationCanceledException();
			    using (var memoryBuffer = new MemoryStream())
			    {
                    await _serializer.WriteMessage(_message, memoryBuffer);
			        await Send(cancellationToken, client.WriteStream, memoryBuffer.ToArray());
			    }
				
				if (IsCancellationRequested)
					throw new OperationCanceledException();
				
				await client.WriteStream.FlushAsync(cancellationToken);
				if(!await GetResponse(client.ReadStream, cancellationToken))
					return;
				var streaming = Message as IStreamingMessage;
				if (streaming != null)
				{
					var readedCount = 0;
					ulong readed = 0;
					RaiseReport(new ProgressInfo<TMessage>(_message, streaming.StreamLength, readed));
					buffer = new byte[2048];
					do
					{
						readedCount = await streaming.Stream.ReadAsync(buffer, 0, buffer.Length);
						await client.WriteStream.WriteAsync(buffer, 0, readedCount, cancellationToken);
						await client.WriteStream.FlushAsync(cancellationToken);
						if(IsCancellationRequested)
							throw new OperationCanceledException();
						readed += (ulong) readedCount;
						RaiseReport(new ProgressInfo<TMessage>(_message, streaming.StreamLength, readed));
					} while (readedCount > 0);
				}
				RaiseSuccess(Message);
				await client.DisconnectAsync();
			}
			finally
			{
				
				if(client != null)
					client.Dispose();
			}
		}

		protected override TMessage Message
		{
			get { return _message; }
		}
	}
}
