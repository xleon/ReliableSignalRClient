using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace QueuedSignalR.Client
{
	public class QueuedSignalRClient : IQueuedSignalRClient
    {
		public event Action<StateChange> OnConnectionStateChanged;
		public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;

		private HubConnection _connection;
		private IHubProxy _proxy;
		private Func<Dictionary<string, string>> _headersProvider;
		private int _secondsToReconnect = 5;
		private readonly string _className;
		private TextWriter _tracer;
		private TraceLevels _traceLevel;
		private string _endpointUri;
		private string _hubName;
		private TimeSpan? _transportConnectionTimeout;
		private Queue<FailedInvoke> _queue;
		private Action<IHubProxy> _proxySubscriber;
		private PCLTimer _timer;

		#region Constructor and fluent settings

		public QueuedSignalRClient()
		{
			_className = nameof(QueuedSignalRClient);
		}

		public IQueuedSignalRClient SetTransportTimeout(TimeSpan transportConnectionTimeout)
		{
			_transportConnectionTimeout = transportConnectionTimeout;
			return this;
		}

		public IQueuedSignalRClient UseTracer(TextWriter textWritter, TraceLevels traceLevel = TraceLevels.All)
		{
			_tracer = textWritter;
			_traceLevel = traceLevel;
			return this;
		}

		public IQueuedSignalRClient SetEndpoint(string endpointUri)
		{
			_endpointUri = endpointUri;
			return this;
		}

		public IQueuedSignalRClient SetHubName(string hubName)
		{
			_hubName = hubName;
			return this;
		}

		public IQueuedSignalRClient SetHeadersProvider(Func<Dictionary<string, string>> headersProvider)
		{
			_headersProvider = headersProvider;
			return this;
		}

		public IQueuedSignalRClient RetryFailedInvokesOnReconnection()
		{
			_queue = new Queue<FailedInvoke>();
			return this;
		}

		public IQueuedSignalRClient SetSecondsToReconnect(int seconds)
		{
			_secondsToReconnect = seconds;
			return this;
		}

		public IQueuedSignalRClient SetProxySubscriber(Action<IHubProxy> proxySubscriber)
		{
			_proxySubscriber = proxySubscriber;
			return this;
		}

		#endregion

		#region Connection life cycle

		public virtual async Task<bool> Connect()
		{
			if (string.IsNullOrEmpty(_endpointUri))
			{
				throw new InvalidOperationException("WithEndpoint() must be called before you connect");
			}

			if (string.IsNullOrEmpty(_hubName))
			{
				throw new InvalidOperationException("WithHubName() must be called before you connect");
			}

			if (_connection != null)
			{
				return false;
			}

			_connection = new HubConnection(_endpointUri);
			_connection.StateChanged += OnConnectionStateChangedHandler;
			_connection.Reconnected += OnReconnectedHandler;

			if (_transportConnectionTimeout != null)
				_connection.TransportConnectTimeout = _transportConnectionTimeout.Value;

			if (_tracer != null)
			{
				_connection.TraceWriter = _tracer;
				_connection.TraceLevel = _traceLevel;
			}

			if (_headersProvider != null)
			{
				foreach (var header in _headersProvider())
				{
					_connection.Headers.Add(header.Key, header.Value);
				}
			}

			_proxy = _connection.CreateHubProxy(_hubName);
			_proxySubscriber?.Invoke(_proxy);

			if (_connection.State != ConnectionState.Disconnected)
				return false;

			try
			{
				_tracer?.WriteLine($"[{_className}] CONNECTING...");
				await _connection.Start();

				if (_queue != null)
				{
					await ProcessInvokeQueue();
				}

				return true;
			}
			catch (Exception ex)
			{
				_tracer?.WriteLine($"[{_className}] CONNECTION START ERROR: {ex.Message}");
			}

			return false;
		}

		public virtual void Disconnect()
		{
			_timer?.Stop();
			DismissCurrentConnection();
			_proxy = null;
		}

		protected virtual void DismissCurrentConnection()
		{
			if (_connection == null) return;

			OnConnectionStateChanged?.Invoke(new StateChange(_connection.State, ConnectionState.Disconnected));

			_connection.StateChanged -= OnConnectionStateChangedHandler;
			_connection.Reconnected -= OnReconnectedHandler;

			var connectionToDispose = _connection;
			_connection = null;

			// connection disposing can block the UI thread for about 20 seconds
			// this doesn´t always happen but just in case we run it on a new thread
			Task.Run(() =>
			{
				try
				{
					connectionToDispose.Dispose();
				}
				catch (Exception ex)
				{
					_tracer?.WriteLine($"[{_className}] Connection could not be disposed: {ex.Message}");
				}
			});
		}

		protected virtual async void OnReconnectedHandler()
		{
			if (_queue != null)
			{
				await ProcessInvokeQueue();
			}
		}

		protected virtual void OnConnectionStateChangedHandler(StateChange change)
		{
			ConnectionState = change.NewState;
			OnConnectionStateChanged?.Invoke(change);

			if (!change.NewState.Equals(ConnectionState.Disconnected)) return;

			// SignalR doesn´t do anything after disconnected state, so we need to manually reconnect
			if (_timer == null)
			{
				_timer = new PCLTimer(TimeSpan.FromSeconds(_secondsToReconnect), async () =>
				{
					DismissCurrentConnection();
					await Connect();
				}, true);
			}

			_timer.Start();
		}

		#endregion

		#region Invokes and Queue

		public async Task Invoke(string method, params object[] args)
		{
			await Connect(); // In case there isn´t a current connection

			try
			{
				await _proxy.Invoke(method, args); //.ConfigureAwait(false);
			}
			catch (InvalidOperationException ex)
			{
				_tracer?.WriteLine($"[{_className}] Could not invoke '{method}': {ex.Message}");
				_queue?.Enqueue(new FailedInvoke(method, args));
			}
			catch (Exception ex)
			{
				_tracer?.WriteLine($"[{_className}] Could not invoke '{method}': {ex.Message}");
				_queue?.Enqueue(new FailedInvoke(method, args));
			}
		}

		public async Task<T> Invoke<T>(string method, params object[] args)
		{
			await Connect(); // In case there isn´t a current connection

			try
			{
				return await _proxy.Invoke<T>(method, args); //.ConfigureAwait(false);
			}
			catch (InvalidOperationException ex)
			{
				_tracer?.WriteLine($"[{_className}] Could not invoke '{method}': {ex.Message}");
				_queue?.Enqueue(new FailedInvoke(method, args));
				return default(T);
			}
			catch (Exception ex)
			{
				_tracer?.WriteLine($"[{_className}] Could not invoke '{method}': {ex.Message}");
				_queue?.Enqueue(new FailedInvoke(method, args));
				return default(T);
			}
		}

		protected virtual async Task ProcessInvokeQueue()
		{
			// we don´t want to process the queue until we are connected
			if (_queue.Count > 0 && ConnectionState == ConnectionState.Connected)
			{
				while (_queue.Count > 0)
				{
					var item = _queue.Dequeue();

					try
					{
						await _proxy.Invoke(item.MethodName, item.Args); //.ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						_tracer?.WriteLine($"[{_className}] Could not process pending item '{item.MethodName}': {ex.Message}");
					}
				}
			}
		}

		#endregion
	}
}
