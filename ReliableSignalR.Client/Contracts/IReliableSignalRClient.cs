using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace ReliableSignalR.Client.Contracts
{
	public interface IReliableSignalRClient
	{
		event Action<StateChange> OnConnectionStateChanged;

		IReliableSignalRClient SetEndpoint(string endpointUri);
		IReliableSignalRClient SetHubName(string hubName);
		IReliableSignalRClient SetTransportTimeout(TimeSpan transportConnectionTimeout);
		IReliableSignalRClient UseTracer(TextWriter textWritter, TraceLevels traceLevel = TraceLevels.All);
		IReliableSignalRClient SetHeadersProvider(Func<Dictionary<string, string>> headersProvider);
		IReliableSignalRClient RetryFailedInvokesOnReconnection();
		IReliableSignalRClient SetSecondsToReconnect(int seconds);
		IReliableSignalRClient SetProxySubscriber(Action<IHubProxy> proxySubscriber);
		ConnectionState ConnectionState { get; }
		Task<bool> Connect();
		void Disconnect();
		Task Invoke(string method, params object[] args);
		Task<T> Invoke<T>(string method, params object[] args);
	}
}