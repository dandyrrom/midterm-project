using System;
using UnityEngine;

public class CandleInventory : MonoBehaviour
{
    [Tooltip("MC can hold only one candle at a time.")]
    public int maxCount = 1;

    public int count;

    public event Action<int> OnCountChanged;

    public bool IsFull => count >= maxCount;
    public bool HasCandle => count > 0;

    public bool TryAddCandle()
    {
        if (IsFull)
            return false;

        count++;
        OnCountChanged?.Invoke(count);
        return true;
    }

    public bool TrySpendCandle()
    {
        if (count <= 0)
            return false;

        count--;
        OnCountChanged?.Invoke(count);
        return true;
    }
}
