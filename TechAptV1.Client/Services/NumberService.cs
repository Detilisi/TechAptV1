// Copyright © 2025 Always Active Technologies PTY Ltd

using System.Collections.Concurrent;
using System.Security.Cryptography;
using TechAptV1.Client.Models;

namespace TechAptV1.Client.Services;

/// <summary>
/// Number Service for generating and processing random numbers
/// </summary>
public static class NumberService
{
    public static int GenerateRandom() => Random.Shared.Next(1, 10_000_000);
    public static int GenerateEven() => Random.Shared.Next(0, 5_000_000) * 2;
    public static int GenerateOdd() => Random.Shared.Next(0, 5_000_000) * 2 + 1;
    public static int GenerateRandomPrime()
    {
        const int maxAttempts = 1000;
        for (int attempts = 0; attempts < maxAttempts; attempts++)
        {
            int randomNumber = GenerateRandom();
            if (IsPrime(randomNumber)) return randomNumber;
        }

        return 2; // Fallback to 2 if no prime found within attempts
    }

    public static bool IsPrime(int number)
    {
        if (number <= 1) return false;
        if (number <= 3) return true;
        if (number % 2 == 0 || number % 3 == 0) return false;
        for (int i = 5; i * i <= number; i = i + 6)
        {
            if (number % i == 0 || number % (i + 2) == 0) return false;
        }
        return true;
    }

    public static List<Number> ConvertToNumberList(List<int> source)
    {
        var result = new Number[source.Count];

        Parallel.ForEach(
            Partitioner.Create(0, source.Count),
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    int val = source[i];
                    result[i] = new Number
                    {
                        Value = val,
                        IsPrime = NumberService.IsPrime(val) ? 1 : 0
                    };
                }
            });

        return result.ToList();
    }
}

