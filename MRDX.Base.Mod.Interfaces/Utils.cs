using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MRDX.Base.Mod.Interfaces;

public static class Utils
{
    public static void Shuffle<T>(Random rng, T[] array)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        for (var i = 0; i < array.Length - 1; ++i)
        {
            var r = rng.Next(i, array.Length);
            (array[r], array[i]) = (array[i], array[r]);
        }
    }

    public static void Shuffle<T>(Random rng, T[,] array)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        var lengthRow = array.GetLength(1);

        for (var i = array.Length - 1; i > 0; i--)
        {
            var i0 = i / lengthRow;
            var i1 = i % lengthRow;

            var j = rng.Next(i + 1);
            var j0 = j / lengthRow;
            var j1 = j % lengthRow;

            (array[i0, i1], array[j0, j1]) = (array[j0, j1], array[i0, i1]);
        }
    }

    public static void Shuffle<T>(Random rng, IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    /// <summary>
    ///     Use reflection to find the appropriate signature for this memory location
    /// </summary>
    /// <param name="game"></param>
    /// <param name="region"></param>
    /// <typeparam name="T">Delegate or the type that you want to get the hookdef from</typeparam>
    /// <returns>Signature string for this location</returns>
    /// <exception cref="Exception">If no hookdef attribute can be found.</exception>
    public static string GetSignature<T>(BaseGame game, Region region)
    {
        foreach (var attr in typeof(T).GetCustomAttributes().OfType<HookDefAttribute>())
            if ((attr.Game & game) != 0 && (attr.Region & region) != 0)
                return attr.Signature;
        throw new Exception(
            $"Unable to find signature for Hook func {typeof(T)} with Game {game} and Region {region}\n   Make sure you define a hook signature!");
    }
}