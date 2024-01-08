using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MeaMod.DNS.Multicast;
using System.Net.Sockets;
using System.Text.Json;
using MeaMod.DNS.Model;

namespace OscManager
{
	public class OscQueryServer
	{
		public ushort tcpPort { get; }
		public ushort udpPort { get; }
		public string ipAddress { get; }
		private HttpListener httpListener;
		private MulticastService multicastService;
		private ServiceDiscovery serviceDiscovery;
		private static readonly HashSet<string> FoundServices = new HashSet<string>();
		private object? hostInfo;
		private object? queryData;
		public string serviceName { get; }
		public string tcpServiceName { get; } = "_oscjson._tcp";
		public string udpServiceName { get; } = "_osc._udp";

		public OscQueryServer(string serviceName, string ipAddress)
		{
			this.serviceName = serviceName;
			
			this.ipAddress = ipAddress;

			tcpPort = FindAvailableTcpPort();
			udpPort = FindAvailableUdpPort();

			SetupJsonObjects();

			httpListener = new HttpListener();
			httpListener.Prefixes.Add($"http://{ipAddress}:{tcpPort}/");
			httpListener.Start();
			httpListener.BeginGetContext(OnHttpRequest, null);
			Debug.WriteLine($"OSCQueryServer Listening at http://{ipAddress}:{tcpPort}");

			multicastService = new MulticastService
			{
				UseIpv6 = false,
				IgnoreDuplicateMessages = true,
			};
			serviceDiscovery = new ServiceDiscovery(multicastService);
			//ListenForServices();
			multicastService.Start();
			AdvertiseOscQueryServer();
		}

		private ushort FindAvailableTcpPort()
		{
			using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(new IPEndPoint(IPAddress.Parse(ipAddress), port: 0));
			ushort port = 0;
			if (socket.LocalEndPoint != null)
				port = (ushort)((IPEndPoint)socket.LocalEndPoint).Port;
			return port;
		}

		private ushort FindAvailableUdpPort()
		{
			using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			socket.Bind(new IPEndPoint(IPAddress.Parse(ipAddress), port: 0));
			ushort port = 0;
			if (socket.LocalEndPoint != null)
				port = (ushort)((IPEndPoint)socket.LocalEndPoint).Port;
			return port;
		}

		private async void OnHttpRequest(IAsyncResult result)
		{
			HttpListenerContext context = httpListener.EndGetContext(result);
			httpListener.BeginGetContext(OnHttpRequest, null);
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;
			string? path = request.Url?.AbsolutePath;
			if (path == null || request.RawUrl == null)
				return;

			if (!request.RawUrl.Contains("HOST_INFO") && path != "/")
			{
				response.StatusCode = 404;
				response.StatusDescription = "Not Found";
				response.Close();
				return;
			}

			string? json = JsonSerializer.Serialize(request.RawUrl.Contains("HOST_INFO") ? hostInfo : queryData);
			response.Headers.Add("pragma:no-cache");
			response.ContentType = "application/json";
			byte[] buffer = Encoding.UTF8.GetBytes(json);
			response.ContentLength64 = buffer.Length;
			await response.OutputStream.WriteAsync(buffer);
			response.OutputStream.Close();
		}

		private void ListenForServices()
		{
			multicastService.NetworkInterfaceDiscovered += (_, args) =>
			{
				Debug.WriteLine("OSCQueryMDNS: Network interface discovered");
				multicastService.SendQuery($"{tcpServiceName}.local");
				multicastService.SendQuery($"{udpServiceName}.local");
			};

			multicastService.AnswerReceived += OnAnswerReceived;
		}

		private static void OnAnswerReceived(object? sender, MessageEventArgs args)
		{
			var response = args.Message;
			try
			{
				foreach (var record in response.AdditionalRecords.OfType<SRVRecord>())
				{
					var domainName = record.Name.Labels;
					var instanceName = domainName[0];
					var type = domainName[2];
					var serviceId = $"{record.CanonicalName}:{record.Port}";
					if (type == "_udp")
						continue; // ignore UDP services

					if (record.TTL == TimeSpan.Zero)
					{
						Debug.WriteLine("OSCQueryMDNS: Goodbye message from {RecordCanonicalName}", record.CanonicalName);
						FoundServices.Remove(serviceId);
						continue;
					}

					if (FoundServices.Contains(serviceId))
						continue;

					var ips = response.AdditionalRecords.OfType<ARecord>().Select(r => r.Address);
					// TODO: handle more than one IP address
					var ipAddress = ips.FirstOrDefault();
					FoundServices.Add(serviceId);
					Debug.WriteLine("OSCQueryMDNS: Found service {ServiceId} {InstanceName} {IpAddress}:{RecordPort}", serviceId, instanceName, ipAddress, record.Port);

					if (instanceName.StartsWith("VRChat-Client-") && ipAddress != null)
					{
						//_lastVrcHttpServer = new IPEndPoint(ipAddress, record.Port);
						//FetchJsonFromVrc(ipAddress, record.Port).GetAwaiter();
					}
				}
			}
			catch (Exception ex)
			{
				//Debug.WriteLine("Failed to parse from {ArgsRemoteEndPoint}: {ExMessage}", args.RemoteEndPoint, ex.Message);
			}
		}

		private void AdvertiseOscQueryServer()
		{
			ServiceProfile httpProfile = new ServiceProfile(
				serviceName,
				tcpServiceName,
				tcpPort,
				new[] { IPAddress.Parse(ipAddress) }
				);

			ServiceProfile oscProfile = new ServiceProfile(
				serviceName,
				udpServiceName,
				udpPort,
				new[] { IPAddress.Parse(ipAddress) }
			);

			serviceDiscovery.Advertise(httpProfile);
			serviceDiscovery.Advertise(oscProfile);
		}

		private void SetupJsonObjects()
		{
			queryData = new
			{
				DESCRIPTION = "",
				FULL_PATH = "/",
				ACCESS = 0,
				CONTENTS = new
				{
					avatar = new
					{
						FULL_PATH = "/avatar",
						ACCESS = 2
					}
				}
			};

			hostInfo = new
			{
				NAME = serviceName,
				OSC_PORT = (int)udpPort,
				OSC_IP = ipAddress,
				OSC_TRANSPORT = "UDP",
				EXTENSIONS = new
				{
					ACCESS = true,
					CLIPMODE = true,
					RANGE = true,
					TYPE = true,
					VALUE = true
				}
			};
		}
	}
}
