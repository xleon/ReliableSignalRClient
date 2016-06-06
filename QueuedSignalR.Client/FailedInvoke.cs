namespace QueuedSignalR.Client
{
	internal class FailedInvoke
	{
		public string MethodName { get; }
		public object[] Args { get; }

		public FailedInvoke(string methodName, params object[] args)
		{
			MethodName = methodName;
			Args = args;
		}
	}
}