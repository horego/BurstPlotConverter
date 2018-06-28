using System;
using System.Threading;
using System.Threading.Tasks;

namespace Horego.BurstPlotConverter.Core
{
    internal class PauseAndResumeTask
    {
        bool m_IsPaused;
        TaskCompletionSource<bool> m_Resume;
        CancellationTokenSource m_CancellationTokenSource;
        readonly object m_Lock = new object();

        public PauseAndResumeTask(bool initialPause = false)
        {
            lock (m_Lock)
            {
                m_IsPaused = initialPause;
                m_Resume = new TaskCompletionSource<bool>();
                if (!initialPause)
                {
                    m_Resume.SetResult(true);
                }
                m_CancellationTokenSource = new CancellationTokenSource();
            }
        }

        public async Task WaitForResume()
        {
            Task task;
            lock (m_Lock)
            {
                task = Task.Run<bool>(async () => await m_Resume.Task, m_CancellationTokenSource.Token);
            }

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            { }
        }

        public bool Pause()
        {
            lock (m_Lock)
            {
                if (m_IsPaused)
                {
                    return false;
                }
                m_CancellationTokenSource.Cancel();
                m_CancellationTokenSource = new CancellationTokenSource();
                m_Resume = new TaskCompletionSource<bool>();
                m_IsPaused = true;
                return true;
            }
        }

        public bool IsPausedUnsafe()
        {
            return m_IsPaused;
        }

        public bool Resume()
        {
            lock (m_Lock)
            {
                if (!m_IsPaused)
                {
                    return false;
                }
                m_Resume.SetResult(true);
                m_IsPaused = false;
                return true;
            }
        }
    }
}