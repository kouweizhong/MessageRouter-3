﻿using System.Runtime.Serialization;
using Melomans.Core.Models;
using Melomans.Core.Network;

namespace Melomans.Core.Message
{
	[DataContract]
	[Message(AccessGroups.System)]
	public class EchoMessage:IMessage
	{
		[DataMember]
		public Meloman Meloman { get; set; }
	}
}
