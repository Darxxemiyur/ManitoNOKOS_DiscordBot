using Name.Bayfaderix.Darxxemiyur.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static Name.Bayfaderix.Darxxemiyur.Common.AsyncJobManager;

namespace Manito.Discord.Client
{
	public class ExecThread : IModule
	{
		private readonly AsyncJobManager _manager;
		private readonly MyDomain _domain;

		public class Job : AsyncJob
		{
			/// <summary>
			/// Cancellation tokens will be delivered to the supplied task.
			/// </summary>
			public Job(Func<CancellationToken, Task<object>> work, CancellationToken token = default) : base(work, token) { }

			/// <summary>
			/// Cancellation tokens will be delivered to the supplied task.
			/// </summary>
			public Job(Func<Task<object>> work, CancellationToken token = default) : base(work, token) { }

			/// <summary>
			/// Cancellation tokens will be delivered to the supplied task.
			/// </summary>
			public Job(Func<CancellationToken, Task> work, CancellationToken token = default) : base(work, token) { }

			/// <summary>
			/// Cancellation tokens will be delivered to the supplied task.
			/// </summary>
			public Job(Func<Task> work, CancellationToken token = default) : base(work, token) { }
		}

		public ExecThread(MyDomain domain)
		{
			_domain = domain;
			_manager = new AsyncJobManager(true, async (x, y) => new Job(() => _domain.Logging.WriteErrorClassedLog(x.GetType().Name, y, false)));
		}

		/// <summary>
		/// Returns a task that represent process of passed task, which on completion will return
		/// the completed task;
		/// </summary>
		/// <param name="runners"></param>
		/// <returns></returns>
		public async Task<Job> AddNew(Job job) => await _manager.AddNew(job) as Job;

		/// <summary>
		/// Returns a list of tasks that represent process of passed tasks, which on completion will
		/// return the completed tasks;
		/// </summary>
		/// <param name="runners"></param>
		/// <returns></returns>
		public async Task<IEnumerable<Job>> AddNew(params Job[] runners) => (await _manager.AddNew(runners)).Select(x => x as Job);

		/// <summary>
		/// Returns a list of tasks that represent process of passed tasks, which on completion will
		/// return the completed tasks;
		/// </summary>
		/// <param name="runners"></param>
		/// <returns></returns>
		public async Task<IEnumerable<Job>> AddNew(IEnumerable<Job> runners) => (await _manager.AddNew(runners)).Select(x => x as Job);

		public Task RunModule() => _manager.RunRunnable();
	}
}