using System;
using System.Threading;
using System.Threading.Tasks;

namespace Myria.Wpf.Services
{
    /// <summary>
    /// Coordinates the startup update check across multiple Myria.Wpf.exe instances via a
    /// session-local named Mutex, so two overlapping launches never race the same download
    /// (see Data/Misc/update.log history - this exact race threw IOException on 0.2.7 and
    /// again on 0.2.11). Only one instance ("the leader") performs the real check; any other
    /// instance ("a follower") waits for the leader to finish and then just proceeds - it never
    /// performs its own redundant check/download.
    /// </summary>
    public static class UpdateCoordinator
    {
        private const string MutexName = "MyriaRPG_UpdateCheck";
        private static readonly TimeSpan FollowerTimeout = TimeSpan.FromSeconds(90);

        private static Mutex? _mutex;

        /// <summary>Call once at startup. Returns true if this instance should perform the real
        /// update check (and must call Release() when done); false if another instance already
        /// owns the check - the caller should then call WaitForLeader() instead.</summary>
        public static bool TryBecomeLeader()
        {
            _mutex = new Mutex(initiallyOwned: false, MutexName);
            try
            {
                return _mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                // A previous leader crashed while holding it - we still successfully acquired
                // it (that's how AbandonedMutexException works), so we become the new leader
                // rather than permanently wedging every future launch.
                return true;
            }
        }

        /// <summary>Called by a follower (TryBecomeLeader() returned false). Waits until the
        /// leader releases the mutex or FollowerTimeout elapses, whichever comes first, then
        /// returns without performing any check of its own. Runs the blocking Mutex.WaitOne on a
        /// background thread so the caller's UI thread (and the update window's indeterminate
        /// progress animation, which needs the dispatcher loop running) never freezes.</summary>
        public static Task WaitForLeaderAsync()
        {
            if (_mutex == null) return Task.CompletedTask;
            return Task.Run(() =>
            {
                try
                {
                    if (_mutex.WaitOne(FollowerTimeout))
                        _mutex.ReleaseMutex();
                }
                catch (AbandonedMutexException)
                {
                    // Acquired via an abandoned mutex - still release it immediately, we're not
                    // the leader and have no check to perform.
                    _mutex.ReleaseMutex();
                }
            });
        }

        /// <summary>Called by the leader once its check is fully complete (success, no-update,
        /// or failure).</summary>
        public static void Release()
        {
            try { _mutex?.ReleaseMutex(); } catch { /* never let cleanup crash startup */ }
        }
    }
}
