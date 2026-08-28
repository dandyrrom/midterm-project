using System;
using UnityEngine;

public class BawangInventory : MonoBehaviour
{
    public int count;

    public event Action<int> OnCountChanged;

    public void AddBawang()
    {
        count++;
        OnCountChanged?.Invoke(count);
    }
}