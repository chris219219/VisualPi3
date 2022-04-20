using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace VisualPi3
{
    public class HexPi
    {
        private static readonly int THREADS = Environment.ProcessorCount;
        private static readonly string FILE_HEX = "hex.txt";
        private static readonly string FILE_ITR = "itr.txt";
        private static readonly string FILE_GRP = "grp.txt";
        private static readonly string FILE_THR = "thr.txt";

        public static readonly StorageFolder LOCAL_FOLDER = ApplicationData.Current.LocalFolder;

        public HexPi()
        {
            PiHex = string.Empty;
            Iteration = 0;
            Threads = THREADS;
            GroupAmount = THREADS * 500;

            Paused = true;
            Stopped = false;

            _pause = true;
            _stop = false;

            _calcTask = new Task(CalcTask);
        }

        public StorageFile HexFile { get; private set; }
        public StorageFile ItrFile { get; private set; }
        public StorageFile GrpFile { get; private set; }
        public StorageFile ThrFile { get; private set; }

        public string PiHex { get; private set; }
        public long Iteration { get; private set; }
        public int GroupAmount { get; private set; }
        public int Threads { get; private set; }

        public bool Paused { get; private set; }
        public bool Stopped { get; private set; }

        private bool _pause;
        private bool _stop;
        private readonly Task _calcTask;

        public async void Initialize()
        {
            HexFile = await LOCAL_FOLDER.CreateFileAsync(FILE_HEX, CreationCollisionOption.OpenIfExists);
            ItrFile = await LOCAL_FOLDER.CreateFileAsync(FILE_ITR, CreationCollisionOption.OpenIfExists);
            GrpFile = await LOCAL_FOLDER.CreateFileAsync(FILE_GRP, CreationCollisionOption.OpenIfExists);
            ThrFile = await LOCAL_FOLDER.CreateFileAsync(FILE_THR, CreationCollisionOption.OpenIfExists);

            _calcTask.Start();
        }

        public async Task<bool> ReadSavedState()
        {
            try
            {
                string tempPiHex = await FileIO.ReadTextAsync(HexFile);
                if (tempPiHex is null || tempPiHex.Length == 0) return false;

                long tempIteration = long.Parse(await FileIO.ReadTextAsync(ItrFile));
                if (tempIteration <= 0) return false;

                int tempGroupAmount = int.Parse(await FileIO.ReadTextAsync(GrpFile));
                if (tempGroupAmount <= 0) return false;

                int tempThreads = int.Parse(await FileIO.ReadTextAsync(ThrFile));
                if (tempThreads <= 0) return false;

                PiHex = tempPiHex;
                Iteration = tempIteration;
                GroupAmount = tempGroupAmount;
                Threads = tempThreads;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public async void SaveState()
        {
            await FileIO.WriteTextAsync(HexFile, PiHex);
            await FileIO.WriteTextAsync(ItrFile, Iteration.ToString());
            await FileIO.WriteTextAsync(GrpFile, GroupAmount.ToString());
            await FileIO.WriteTextAsync(ThrFile, Threads.ToString());
        }

        public async void ClearState()
        {
            await FileIO.WriteTextAsync(HexFile, string.Empty);
            await FileIO.WriteTextAsync(ItrFile, string.Empty);
            await FileIO.WriteTextAsync(GrpFile, string.Empty);
            await FileIO.WriteTextAsync(ThrFile, string.Empty);
        }

        public bool SetGroupAmountAndThreads(int amount, int threads)
        {
            if (!Paused || amount % threads != 0)
                return false;

            GroupAmount = amount;
            Threads = threads;
            return true;
        }

        public void Start()
        {
            if (!Paused)
                throw new InvalidOperationException("Cannot call Start if not Paused.");

            _pause = false;
            while (Paused)
                Thread.Sleep(100);
        }

        public void Pause()
        {
            if (Paused)
                throw new InvalidOperationException("Cannot call Pause if Paused.");

            _pause = true;
            while (!Paused)
                Thread.Sleep(100);
        }

        public void Stop()
        {
            _pause = true;
            _stop = true;

            while (!Paused && !Stopped)
                Thread.Sleep(100);
        }

        private void CalcTask()
        {
            while (!_stop)
            {
                if (Stopped) Stopped = false;
                while (!_pause)
                {
                    if (Paused) Paused = false;

                    string hex = HexPiMath.PiGroup(Iteration, GroupAmount, Threads);
                    PiHex += hex;
                    Iteration += GroupAmount;
                }
                if (!Paused) Paused = true;
                Thread.Sleep(250);
            }
            Stopped = true;
        }
    }
}
