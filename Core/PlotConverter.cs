using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Horego.BurstPlotConverter.Core
{
    internal class ProgressEventArgs
    {
        public TimeSpan ElapsedTime { get; }
        public TimeSpan RemainingTime { get; }
        public double PercentComplete { get; }
        public bool IsPaused { get; }

        public ProgressEventArgs(TimeSpan elapsedTime, TimeSpan remainingTime, double percentComplete, bool isPaused)
        {
            ElapsedTime = elapsedTime;
            RemainingTime = remainingTime;
            PercentComplete = percentComplete;
            IsPaused = isPaused;
        }
    }

    internal class PlotConverterCheckpoint
    {
        public long Position { get; }

        public PlotConverterCheckpoint(long position)
        {
            Position = position;
        }
    }

    internal class PlotConverter
    {
        readonly FileInfo m_InputFile;
        readonly PlotFile m_InputPlotFile;
        readonly TimeSpan m_ProgressIntervall = TimeSpan.FromSeconds(10);
        readonly int m_Partitions;
        readonly PauseAndResumeTask m_PauseAndResumeTask = new PauseAndResumeTask();
        readonly CancellationTokenSource m_CancellationTokenSource = new CancellationTokenSource();

        public PlotConverterCheckpoint Checkpoint { get; private set; }
        public int UsedMemoryInMb { get; }
        public ISubject<ProgressEventArgs> Progress { get; }

        public PlotConverter(FileInfo inputFile, int usedMemoryInMb)
        {
            m_InputFile = inputFile;
            m_InputPlotFile = new PlotFile(inputFile);
            m_Partitions = GetPartitionFactor(m_InputPlotFile.Nonces, usedMemoryInMb);
            UsedMemoryInMb = ToMegaByte(GetUsedMemory(m_InputPlotFile.Nonces, m_Partitions));
            Progress = new Subject<ProgressEventArgs>();
        }

        public void Abort()
        {
            m_CancellationTokenSource.Cancel();
        }

        public async Task RunOutline(FileInfo outputFile, PlotConverterCheckpoint checkpoint)
        {
            var blockSize = m_InputPlotFile.Nonces * Constants.SCOOP_SIZE;
            
            m_InputPlotFile.Validate();

            var outputStream = new FileStream(outputFile.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);
            var inputStream = new FileStream(m_InputFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);

            outputStream.SetLength(m_InputPlotFile.RealPlotSize); //Allocate memory
            await ShufflePoc1To2(inputStream, outputStream, blockSize, m_InputPlotFile.Nonces, m_Partitions, checkpoint);

            inputStream.Close();
            outputStream.Close();
        }

        public async Task RunInline(PlotConverterCheckpoint checkpoint)
        {
            var blockSize = m_InputPlotFile.Nonces * Constants.SCOOP_SIZE;
            m_InputPlotFile.Validate();

            var handle = new FileStream(m_InputFile.FullName, FileMode.Open, FileAccess.Read | FileAccess.Write, FileShare.None, 4096, true);

            var done = await ShufflePoc1To2(handle, handle, blockSize, m_InputPlotFile.Nonces, m_Partitions, checkpoint);

            handle.Close();
            if (done)
                m_InputPlotFile.Rename(m_InputPlotFile.Poc2FileName);
        }

        async Task<bool> ShufflePoc1To2(FileStream sourceStream, FileStream destinationStream, long blockSize, long numnonces, int partitions, PlotConverterCheckpoint checkpoint)
        {
            var totalIterations = Constants.SCOOPS_IN_NONCE / 2 * numnonces;
            var iterationPosition = 0L;

            var stopwatch = Stopwatch.StartNew();
            var timer = Observable.Timer(TimeSpan.Zero, m_ProgressIntervall);
            var timerSubscription = timer.Subscribe(ticks =>
            {
                var elapsedTime = stopwatch.Elapsed;
                var iterationsRemaining = totalIterations - iterationPosition;
                var percentage = (double)iterationPosition / totalIterations * 100.0;
                var remainingTime = iterationPosition == 0
                    ? TimeSpan.MaxValue
                    : TimeSpan.FromTicks(Convert.ToInt64((double)elapsedTime.Ticks / iterationPosition *
                                                         iterationsRemaining));
                Progress.OnNext(new ProgressEventArgs(elapsedTime, remainingTime, percentage, this.IsPausedUnsafe()));
            });

            var adjustedBlockSize = blockSize * partitions;
            if (checkpoint.Position % adjustedBlockSize != 0)
            {
                throw new PlotConverterException($"Can not resume plot conversion with checkpoint {checkpoint.Position} and {partitions} partitions.");
            }
            var resumeScoopIndex = checkpoint.Position / adjustedBlockSize;
            var buffer1 = new byte[adjustedBlockSize];
            var buffer2 = new byte[adjustedBlockSize];
            for (var scoopIndex = resumeScoopIndex; scoopIndex < Constants.SCOOPS_IN_NONCE / 2 / partitions; scoopIndex++)
            {
                var pos = scoopIndex * adjustedBlockSize;

                await m_PauseAndResumeTask.WaitForResume().ConfigureAwait(false);

                sourceStream.Seek(pos, SeekOrigin.Begin);
                var numread = await sourceStream.ReadAsync(buffer1, 0, buffer1.Length).ConfigureAwait(false);
                if (numread != adjustedBlockSize)
                    throw new PlotConverterException($"read {numread} bytes instead of {adjustedBlockSize}.");

                await m_PauseAndResumeTask.WaitForResume().ConfigureAwait(false);

                sourceStream.Seek(-(pos + adjustedBlockSize), SeekOrigin.End);
                numread = await sourceStream.ReadAsync(buffer2, 0, buffer2.Length).ConfigureAwait(false);
                if (numread != adjustedBlockSize)
                    throw new PlotConverterException($"read {numread} bytes instead of {adjustedBlockSize}.");

                if (partitions == 1)
                {
                    var hash1 = new byte[Constants.SHABAL256_HASH_SIZE];
                    var off = 32;
                    for (var nonceIndex = 0; nonceIndex < numnonces; nonceIndex++)
                    {
                        iterationPosition++;
                        Buffer.BlockCopy(buffer1, off, hash1, 0, Constants.SHABAL256_HASH_SIZE);
                        Buffer.BlockCopy(buffer2, off, buffer1, off, Constants.SHABAL256_HASH_SIZE);
                        Buffer.BlockCopy(hash1, 0, buffer2, off, Constants.SHABAL256_HASH_SIZE);
                        off += Constants.SCOOP_SIZE;
                    }
                }
                else
                {
                    Parallel.For(0, partitions, partitionIndex =>
                    {
                        var hash1 = new byte[Constants.SHABAL256_HASH_SIZE];
                        for (var nonceIndex = 0; nonceIndex < numnonces; nonceIndex++)
                        {
                            var off = nonceIndex * Constants.SCOOP_SIZE + 32;
                            Buffer.BlockCopy(buffer1, partitionIndex * (int)numnonces * Constants.SCOOP_SIZE + off, hash1,
                                0, Constants.SHABAL256_HASH_SIZE);
                            Buffer.BlockCopy(buffer2,
                                (partitions - partitionIndex - 1) * (int)numnonces * Constants.SCOOP_SIZE + off, buffer1,
                                partitionIndex * (int)numnonces * Constants.SCOOP_SIZE + off,
                                Constants.SHABAL256_HASH_SIZE);
                            Buffer.BlockCopy(hash1, 0, buffer2,
                                (partitions - partitionIndex - 1) * (int)numnonces * Constants.SCOOP_SIZE + off,
                                Constants.SHABAL256_HASH_SIZE);
                        }
                        Interlocked.Add(ref iterationPosition, numnonces);
                    });
                }

                if (m_CancellationTokenSource.IsCancellationRequested)
                {
                    Checkpoint = new PlotConverterCheckpoint(pos);
                    break;
                }

                await m_PauseAndResumeTask.WaitForResume().ConfigureAwait(false);

                destinationStream.Seek(-(pos + adjustedBlockSize), SeekOrigin.End); //seek from EOF
                await destinationStream.WriteAsync(buffer2, 0, buffer2.Length).ConfigureAwait(false);

                await m_PauseAndResumeTask.WaitForResume().ConfigureAwait(false);

                destinationStream.Seek(pos, SeekOrigin.Begin);
                await destinationStream.WriteAsync(buffer1, 0, buffer1.Length).ConfigureAwait(false);
            }

            stopwatch.Stop();
            timerSubscription.Dispose();
            if (!m_CancellationTokenSource.IsCancellationRequested)
                Progress.OnNext(new ProgressEventArgs(stopwatch.Elapsed, TimeSpan.Zero, 100, false));

            return !m_CancellationTokenSource.IsCancellationRequested;
        }

        int GetPartitionFactor(long nonces, int useMemoryInMb)
        {
            if (useMemoryInMb == 0)
            {
                return 1;
            }

            var useMemory = ToByte(useMemoryInMb);
            var currentMemoryUsage = GetUsedMemory(nonces, 1);
            var partitions = 1;
            var lastValidParitions = 1;
            // Check int.maxvalue array problem
            while (currentMemoryUsage < useMemory && partitions < Constants.SCOOPS_IN_NONCE / 2 && currentMemoryUsage < Int32.MaxValue)
            {
                partitions++;
                currentMemoryUsage = GetUsedMemory(nonces, partitions);
                if ((Constants.SCOOPS_IN_NONCE / 2) % partitions == 0)
                {
                    lastValidParitions = partitions;
                }
            }
            return lastValidParitions;
        }

        int ToMegaByte(long bytes)
        {
            return (int)(bytes / 524288);
        }

        long ToByte(int megaBytes)
        {
            return megaBytes * 524288L;
        }

        long GetUsedMemory(long nonces, int partitions)
        {
            var blockSize = nonces * Constants.SCOOP_SIZE * partitions; // how big is a 'scoop block' - we have 4096 of these
            return blockSize;
        }

        public string Info()
        {
            var plotFile = m_InputPlotFile;
            return $"Input file: {m_InputFile.FullName}" + Environment.NewLine +
                   $"Used memory: {UsedMemoryInMb} MByte." + Environment.NewLine +
                   $"Used paritions: {m_Partitions}" + Environment.NewLine +
                   $"Plot account: {plotFile.Id}" + Environment.NewLine +
                   $"Plot nonce start: {plotFile.Offset}" + Environment.NewLine +
                   $"Plot nonces: {plotFile.Nonces}" + Environment.NewLine +
                   $"Expeced plot size: {plotFile.ExpectedPlotSize}" + Environment.NewLine +
                   $"Real plot size: {plotFile.RealPlotSize}";
        }

        public bool IsPausedUnsafe()
        {
            return m_PauseAndResumeTask.IsPausedUnsafe();
        }

        public bool Resume()
        {
            return m_PauseAndResumeTask.Resume();
        }

        public bool Pause()
        {
            return m_PauseAndResumeTask.Pause();
        }
    }
}