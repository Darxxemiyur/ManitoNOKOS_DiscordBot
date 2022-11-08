using DisCatSharp;

using Name.Bayfaderix.Darxxemiyur.Common;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Manito.Discord
{
	public class DiscordEventProxy<T> : IDisposable
	{
		private FIFOFBACollection<(DiscordClient, T)> _facade;

		public DiscordEventProxy() => _facade = new();

		public Task Handle(DiscordClient client, T stuff) => _facade.Handle((client, stuff));

		public Task<bool> HasAny() => _facade.HasAny();

		public async Task Cancel() => await _facade.Cancel();

		public async Task<(DiscordClient, T)> GetData(CancellationToken token = default) => await _facade.GetData(token);

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
				}
			   ((IDisposable)_facade).Dispose();
				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged
		// resources ~TaskEventProxy() { // Do not change this code. Put cleanup code in
		// 'Dispose(bool disposing)' method Dispose(disposing: false); }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		~DiscordEventProxy()
		{
			// Do not re-create Dispose clean-up code here. Calling Dispose(false) is optimal in
			// terms of readability and maintainability.
			this.Dispose(false);
		}
	}
}