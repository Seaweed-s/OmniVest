using System.Diagnostics;
using System.Net;
using System.Threading.Channels;
using LucHeart.CoreOSC;
//using Serilog;

namespace OscManager
{
	public class OscClient
	{
		public event EventHandler<OscMessage>? MessageReceived;

		ushort sendPort, recvPort;

		private bool receving = false;
		private OscDuplex? gameConnection;
		private readonly Channel<OscMessage> gameSenderChannel;

		public OscClient(ushort sendPort, ushort recvPort)
		{
			this.sendPort = sendPort;
			this.recvPort = recvPort;

			//gameConnection = new OscDuplex(new IPEndPoint(IPAddress.Loopback, recvPort), new IPEndPoint(IPAddress.Loopback, sendPort));

			gameSenderChannel = Channel.CreateUnbounded<OscMessage>(new UnboundedChannelOptions()
			{
				SingleReader = true
			});
		}

		public ValueTask SendGameMessage(string address, params object?[]? arguments)
		{
			arguments ??= Array.Empty<object>();
			return gameSenderChannel.Writer.WriteAsync(new OscMessage(address, arguments));
		}

		private async Task GameSenderLoop()
		{
			//Logger.Debug("Starting game sender loop");
			if (gameConnection == null) return;

			await foreach (var oscMessage in gameSenderChannel.Reader.ReadAllAsync())
			{
				try
				{
					await gameConnection.SendAsync(oscMessage);
				}
				catch (Exception e)
				{
					Debug.WriteLine("GameSenderClient send failed");
				}
			}
		}

		public void StartReciever()
		{
			if (!receving)
			{
				receving = true;
                gameConnection = new OscDuplex(new IPEndPoint(IPAddress.Loopback, recvPort), new IPEndPoint(IPAddress.Loopback, sendPort));
                Task.Run(RecieverLoop);
			}

		}

		public void StopReciever()
		{
			if (gameConnection != null)
			{
                gameConnection.Dispose();
            }
			receving = false;
		}

		protected virtual void OnMessageReceived(OscMessage message)
		{
			MessageReceived?.Invoke(this, message);
		}

		// Loop task for recieving OSC data from another application
		private async Task RecieverLoop()
		{
			while (receving)
			{
				//Debug.WriteLine("Reciever Loop");
				try
				{
					if (gameConnection != null)
					{
                        OscMessage recieved = await gameConnection.ReceiveMessageAsync();
                        if (!receving)
                        {
                            break;
                        }
                        OnMessageReceived(recieved);

                        string address = recieved.Address;

                        //Debug.WriteLine($"Recieved Message: {address}");
                        //Debug.WriteLine($"Recived Data: {recieved.Arguments.ElementAtOrDefault(0)}");
                    } else
					{
						break;	// Just break the loop if no game connection is established for now...
					}
				}
				catch
				{
					Debug.WriteLine("Reciever Error");
				}
			}
		}
	}
}

