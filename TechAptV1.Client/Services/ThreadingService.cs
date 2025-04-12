// Copyright © 2025 Always Active Technologies PTY Ltd

using System.Collections.Concurrent;
using TechAptV1.Client.Models;

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
    private readonly object _listLock = new(); //To synchronize access to shared list
    private CancellationTokenSource _cts = new(); //To control thread execution

    private readonly List<int> _sharedGlobalList = new(capacity: TotalLimit);

    //Properties
    private int _oddNumbers = 0;
    private int _evenNumbers = 0;
    private int _primeNumbers = 0;
    private int _totalNumbers = 0;
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
        logger.LogInformation("Starting number generation process...");

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

        lock (_listLock)
        {
            _sharedGlobalList.Sort(); // Sort the list in ascending order
        }

        _totalNumbers = _sharedGlobalList.Count;

        logger.LogInformation("Number generation Complete.");
        logger.LogInformation($"Summary: Total = {_totalNumbers}, Odd = {_oddNumbers}, Even = {_evenNumbers}, Prime = {_primeNumbers}");
    }

    /// <summary>
    /// Persist the results to the SQLite database
    /// </summary>
    public async Task Save()
    {
        logger.LogInformation("Initiating save operation...");

        List<Number> numberListCopy;

        try
        {
            lock (_listLock)
            {
                if (_sharedGlobalList == null || _sharedGlobalList.Count == 0)
                {
                    logger.LogWarning("Save operation skipped: No numbers generated or list is empty.");
                    return;
                }
                numberListCopy = NumberService.ConvertToNumberList(_sharedGlobalList);
            }

            logger.LogDebug("Calling dataService.Save...");
            await dataService.Save(numberListCopy);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to copy or convert the shared list for saving.");
            throw; // Re-throw as saving cannot proceed
        }

        logger.LogInformation("Save completed");
    }

    //Helper methods
    private void GenerateOddNumbers(CancellationToken token)
    {
        logger.LogInformation("Odd number generator started.");

        while (!token.IsCancellationRequested)
        {
            bool addedSuccessfully = TryAddNumberToSharedList(() => NumberService.GenerateOdd(), ref _oddNumbers);
            if (!addedSuccessfully) break;
        }

        logger.LogInformation("Odd number generator stopped.");
    }

    private void GenerateNegatedPrimes(CancellationToken token)
    {
        logger.LogInformation("Prime number generator started.");

        while (!token.IsCancellationRequested)
        {

            int negatedPrime = -NumberService.GenerateRandomPrime();
            bool addedSuccessfully = TryAddNumberToSharedList(() => negatedPrime, ref _primeNumbers);
            if (!addedSuccessfully) break;
        }

        logger.LogInformation("Prime number generator stopped.");
    }

    private async Task MonitorAndStartEvenGenerator(CancellationToken token)
    {
        while (_sharedGlobalList.Count < ThresholdForEvenGenerator)
        {
            await Task.Delay(100, token); // Avoid tight loop
        }

        logger.LogInformation("2.5 million numbers reached. Launching even number generator...");

        await Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                bool addedSuccessfully = TryAddNumberToSharedList(() => NumberService.GenerateEven(), ref _evenNumbers);
                if (!addedSuccessfully) break;
            }

            logger.LogInformation("Even number generator finished.");
        }, token);

        _cts.Cancel();
    }

    private bool TryAddNumberToSharedList(Func<int> generator, ref int counter)
    {
        lock (_listLock)
        {
            if (_sharedGlobalList.Count >= TotalLimit) return false;

            int number = generator();
            _sharedGlobalList.Add(number);
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
        _sharedGlobalList.Clear();
    }
}
