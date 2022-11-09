using System;
using System.Threading;
using System.Threading.Tasks;

namespace g3
{
    /// <summary>
    /// The code is copied from https://github.com/StephenCleary/AsyncEx
    /// </summary>
    public static class TaskExtension
    {
        public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, CancellationToken cancellationToken)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (!cancellationToken.CanBeCanceled)
                return task;
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            return DoWaitAsync(task, cancellationToken);
        }

        private static async Task<TResult> DoWaitAsync<TResult>(Task<TResult> task, CancellationToken cancellationToken)
        {
            using (var cancelTaskSource = new CancellationTokenTaskSource<TResult>(cancellationToken))
                return await (await Task.WhenAny(task, cancelTaskSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Holds the task for a cancellation token, as well as the token registration. The registration is disposed when this instance is disposed.
        /// </summary>
        private sealed class CancellationTokenTaskSource<T> : IDisposable
        {
            /// <summary>
            /// The cancellation token registration, if any. This is <c>null</c> if the registration was not necessary.
            /// </summary>
            private readonly IDisposable _registration;

            /// <summary>
            /// Creates a task for the specified cancellation token, registering with the token if necessary.
            /// </summary>
            /// <param name="cancellationToken">The cancellation token to observe.</param>
            public CancellationTokenTaskSource(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Task = System.Threading.Tasks.Task.FromCanceled<T>(cancellationToken);
                    return;
                }
                var tcs = new TaskCompletionSource<T>();
                _registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false);
                Task = tcs.Task;
            }

            /// <summary>
            /// Gets the task for the source cancellation token.
            /// </summary>
            public Task<T> Task { get; private set; }

            /// <summary>
            /// Disposes the cancellation token registration, if any. Note that this may cause <see cref="Task"/> to never complete.
            /// </summary>
            public void Dispose()
            {
                _registration?.Dispose();
            }
        }
    }
}