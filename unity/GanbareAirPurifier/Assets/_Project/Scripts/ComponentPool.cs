using System;
using System.Collections.Generic;
using UnityEngine;

public class ComponentPool<T> where T : Component
{
    private readonly Stack<T> inactiveItems = new Stack<T>();
    private readonly HashSet<T> pooledItems = new HashSet<T>();
    private readonly HashSet<T> activeItems = new HashSet<T>();
    private readonly Func<T> factory;
    private readonly Action<T> onGet;
    private readonly Action<T> onRelease;

    public ComponentPool(Func<T> factory, int initialSize, Action<T> onGet = null, Action<T> onRelease = null)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.onGet = onGet;
        this.onRelease = onRelease;
        Prewarm(initialSize);
    }

    public int InactiveCount => inactiveItems.Count;
    public int ActiveCount => activeItems.Count;
    public int TotalCount => pooledItems.Count;

    public void Prewarm(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Release(CreateItem());
        }
    }

    public T Get()
    {
        var item = inactiveItems.Count > 0 ? inactiveItems.Pop() : CreateItem();
        activeItems.Add(item);
        onGet?.Invoke(item);
        return item;
    }

    public void Release(T item)
    {
        if (item == null)
        {
            return;
        }

        if (pooledItems.Contains(item) && !activeItems.Remove(item) && inactiveItems.Contains(item))
        {
            return;
        }

        if (!pooledItems.Contains(item))
        {
            pooledItems.Add(item);
        }

        onRelease?.Invoke(item);
        inactiveItems.Push(item);
    }

    public void ReleaseAllActive()
    {
        var items = new List<T>(activeItems);
        for (var i = 0; i < items.Count; i++)
        {
            Release(items[i]);
        }
    }

    private T CreateItem()
    {
        var item = factory();
        pooledItems.Add(item);
        return item;
    }
}
