using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace QueuedSignalR.Client
{
	public interface IQueuedSignalRClient
	{
		event Action<StateChange> OnConnectionStateChanged;

		IQueuedSignalRClient SetEndpoint(string endpointUri);
		IQueuedSignalRClient SetHubName(string hubName);
		IQueuedSignalRClient SetTransportTimeout(TimeSpan transportConnectionTimeout);
		IQueuedSignalRClient UseTracer(TextWriter textWritter, TraceLevels traceLevel = TraceLevels.All);
		IQueuedSignalRClient SetHeadersProvider(Func<Dictionary<string, string>> headersProvider);
		IQueuedSignalRClient RetryFailedInvokesOnReconnection();
		IQueuedSignalRClient SetSecondsToReconnect(int seconds);
		IQueuedSignalRClient SetProxySubscriber(Action<IHubProxy> proxySubscriber);
		ConnectionState ConnectionState { get; }
		Task<bool> Connect();
		void Disconnect();
		Task Invoke(string method, params object[] args);
		Task<T> Invoke<T>(string method, params object[] args);
	}
}