using System;
using System.Threading.Tasks;

namespace QueuedSignalR.Client
{
	public class PCLTimer
	{
		public bool IsRunning { get; private set; }

		private readonly TimeSpan _interval;
		private readonly Action _tick;
		private readonly bool _runOnce;

		public PCLTimer(TimeSpan interval, Action tick, bool runOnce = false)
		{
			_interval = interval;
			_tick = tick;
			_runOnce = runOnce;
		}

		public PCLTimer Start()
		{
			if (IsRunning)
				return this;

			IsRunning = true;
			var t = RunTimer();

			return this;
		}

		public void Stop() => 
			IsRunning = false;

		private async Task RunTimer()
		{
			while (IsRunning)
			{
				await Task.Delay(_interval);

				if (!IsRunning) continue;

				_tick();

				if (_runOnce)
					Stop();
			}
		}
	}
}