// Copyright © 2025 Always Active Technologies PTY Ltd

namespace TechAptV1.Client.Services;

/// <summary>
/// Number Service for generating and processing random numbers
/// </summary>
public static class NumberService
{
    private static readonly Random _random = new();

    public static int GenerateOdd()
    {
        return _random.Next(1, int.MaxValue) | 1; // force odd
    }

    public static int GenerateEven()
    {
        return _random.Next(1, int.MaxValue) & ~1; // force even
    }

    public static int GenerateRandom()
    {
        return _random.Next(1, int.MaxValue);
    }

    public static bool IsPrime(int number)
    {
        if (number < 2) return false;
        for (int i = 2; i <= Math.Sqrt(number); i++)
            if (number % i == 0) return false;
        return true;
    }
}

