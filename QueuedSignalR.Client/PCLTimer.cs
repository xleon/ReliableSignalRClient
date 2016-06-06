using System;
using System.Threading.Tasks;

namespace QueuedSignalR.Client
{
	public class PCLTimer
	{
		private bool _timerRunning;
		public bool IsRunning => _timerRunning;
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
			if (!_timerRunning)
			{
				_timerRunning = true;
				var t = RunTimer();
			}

			return this;
		}

		public void Stop()
		{
			_timerRunning = false;
		}

		private async Task RunTimer()
		{
			while (_timerRunning)
			{
				await Task.Delay(_interval);

				if (_timerRunning)
				{
					_tick();

					if (_runOnce)
					{
						Stop();
					}
				}
			}
		}
	}
}