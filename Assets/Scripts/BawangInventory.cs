using System;
using UnityEngine;

public class BawangInventory : MonoBehaviour
{
    public int maxCount = 5;

    public int count;

    public event Action<int> OnCountChanged;

    public bool IsFull => count >= maxCount;

    public bool TryAddBawang()
    {
        if (IsFull)
            return false;

        count++;
        OnCountChanged?.Invoke(count);
        return true;
    }

    public bool TrySpendBawang()
    {
        if (count <= 0)
            return false;

        count--;
        OnCountChanged?.Invoke(count);
        return true;
    }
}
