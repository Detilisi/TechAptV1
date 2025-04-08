// Copyright © 2025 Always Active Technologies PTY Ltd

namespace TechAptV1.Client.Services;

/// <summary>
/// Default constructor providing DI Logger and Data Service
/// </summary>
/// <param name="logger"></param>
/// <param name="dataService"></param>
public sealed class ThreadingService(ILogger<ThreadingService> logger, DataService dataService)
{
    //Constants
    private const int TotalLimit = 10_000_000;
    private const int ThresholdForEvenGenerator = 2_500_000;

    //Fields
    private int _oddNumbers = 0;
    private int _evenNumbers = 0;
    private int _primeNumbers = 0;
    private int _totalNumbers = 0;

    private readonly object _lock = new();
    private CancellationTokenSource _cts = new();
    private readonly List<int> _globalNumberList = new(capacity: TotalLimit);

    //Getters
    public int GetOddNumbers() => _oddNumbers;
    public int GetEvenNumbers() => _evenNumbers;
    public int GetPrimeNumbers() => _primeNumbers;
    public int GetTotalNumbers() => _totalNumbers;

    //Main methods
    /// <summary>
    /// Start the random number generation process
    /// </summary>
    public async Task Start()
    {
        logger.LogInformation("Start");

        ResetState();
        _cts = new CancellationTokenSource();

        var tasks = new List<Task>
        {
            Task.Run(() => GenerateOddNumbers(_cts.Token)),
            Task.Run(() => GenerateNegatedPrimes(_cts.Token)),
            Task.Run(() => MonitorAndStartEvenGenerator(_cts.Token))
        };

        await Task.WhenAll(tasks);

        logger.LogInformation("All threads have finished. Sorting the final list...");

        lock (_lock)
        {
            _globalNumberList.Sort();
        }

        _totalNumbers = _globalNumberList.Count;

        logger.LogInformation($"Summary: Total = {_totalNumbers}, Odd = {_oddNumbers}, Even = {_evenNumbers}, Prime = {_primeNumbers}");
    }

    /// <summary>
    /// Persist the results to the SQLite database
    /// </summary>
    public async Task Save()
    {
        logger.LogInformation("Save");
        throw new NotImplementedException();
    }

    //Helper methods
    private void GenerateOddNumbers(CancellationToken token)
    {
        logger.LogInformation("Odd number generator started.");

        while (!token.IsCancellationRequested)
        {
            bool addedSuccessfully = TryAddNumber(() => NumberService.GenerateOdd(), ref _oddNumbers);
            if (!addedSuccessfully) break;
        }

        logger.LogInformation("Odd number generator stopped.");
    }

    private void GenerateNegatedPrimes(CancellationToken token)
    {
        logger.LogInformation("Prime number generator started.");

        while (!token.IsCancellationRequested)
        {
            int candidate = NumberService.GenerateRandom();
            if (NumberService.IsPrime(candidate))
            {
                int negatedPrime = -candidate;
                bool addedSuccessfully = TryAddNumber(() => negatedPrime, ref _primeNumbers);
                if (!addedSuccessfully) break;
            }
        }

        logger.LogInformation("Prime number generator stopped.");
    }

    private async Task MonitorAndStartEvenGenerator(CancellationToken token)
    {
        while (_globalNumberList.Count < ThresholdForEvenGenerator)
        {
            await Task.Delay(100);
        }

        logger.LogInformation("2.5 million numbers reached. Launching even number generator...");

        await Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                bool addedSuccessfully = TryAddNumber(() => NumberService.GenerateEven(), ref _evenNumbers);
                if (!addedSuccessfully) break;
            }

            logger.LogInformation("Even number generator finished.");
        });

        _cts.Cancel();
    }

    private bool TryAddNumber(Func<int> generator, ref int counter)
    {
        lock (_lock)
        {
            if (_globalNumberList.Count >= TotalLimit) return false;

            int number = generator();
            _globalNumberList.Add(number);
            Interlocked.Increment(ref counter);
            return true;
        }
    }

    private void ResetState()
    {
        _oddNumbers = 0;
        _evenNumbers = 0;
        _primeNumbers = 0;
        _totalNumbers = 0;
        _globalNumberList.Clear();
    }
}
