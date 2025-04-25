using System;
using System.Collections.Generic;
using System.Threading;

namespace MRDX.Base.ExtractDataBin.Interface;

public interface IExtractDataBin
{
    // static readonly object LockMr1 = new();
    // static readonly object LockMr2 = new();

    static string? ExtractedPath { get; set; } = string.Empty;

    // As of version 1.1.0 ExtractDataBin automatically extracts the zip.
    // You can add an action to this event to watch for unzip completion,
    // or you can check ExtractedPath to see if its already unzipped
    // public event OnExtractComplete ExtractComplete;

    public OneTimeEvent<string> ExtractComplete { get; }
    string? ExtractMr1();
    string? ExtractMr2();
}

public class OneTimeEvent<T>
{
    private readonly Lock _lock = new();
    private readonly List<Action<T?>> _subscribers = [];
    private T? _eventArgs;
    private bool _hasFired;

    public void Fire(T? args)
    {
        List<Action<T?>> toInvoke = null;

        lock (_lock)
        {
            if (_hasFired)
                return;

            _hasFired = true;
            _eventArgs = args;
            toInvoke = new List<Action<T?>>(_subscribers);
            _subscribers.Clear(); // Optional: allow only one-time notification
        }

        foreach (var subscriber in toInvoke) subscriber?.Invoke(args);
    }

    public void Subscribe(Action<T?> callback)
    {
        bool alreadyFired;
        T? argsCopy = default;

        lock (_lock)
        {
            alreadyFired = _hasFired;
            if (!alreadyFired)
                _subscribers.Add(callback);
            else
                argsCopy = _eventArgs;
        }

        if (alreadyFired) callback?.Invoke(argsCopy);
    }

    public void Unsubscribe(Action<T?> callback)
    {
        lock (_lock)
        {
            _subscribers.Remove(callback);
        }
    }
}