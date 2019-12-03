using UnityEngine;
using System.Collections;
using System.Linq;
using System;
using System.Collections.Generic;

[Serializable]
public class WeightedList : IList<WeightedItem>
{
    [HideInInspector]
    [SerializeField]
    private List<WeightedItem> internalList = new List<WeightedItem>();

    public WeightedItem this[int index] { get { return ((IList<WeightedItem>)internalList)[index]; } set { ((IList<WeightedItem>)internalList)[index] = value; } }

    public int Count => ((IList<WeightedItem>)internalList).Count;

    public bool IsReadOnly => ((IList<WeightedItem>)internalList).IsReadOnly;

    public void Add(UnityEngine.Object obj,float weight)
    {
        Add(new WeightedItem(obj, weight));
    }

    public void Add(WeightedItem item)
    {
        ((IList<WeightedItem>)internalList).Add(item);
    }

    public void Clear()
    {
        ((IList<WeightedItem>)internalList).Clear();
    }

    public bool Contains(WeightedItem item)
    {
        return ((IList<WeightedItem>)internalList).Contains(item);
    }

    public void CopyTo(WeightedItem[] array, int arrayIndex)
    {
        ((IList<WeightedItem>)internalList).CopyTo(array, arrayIndex);
    }

    public IEnumerator<WeightedItem> GetEnumerator()
    {
        return ((IList<WeightedItem>)internalList).GetEnumerator();
    }

    public int IndexOf(WeightedItem item)
    {
        return ((IList<WeightedItem>)internalList).IndexOf(item);
    }

    public void Insert(int index, WeightedItem item)
    {
        ((IList<WeightedItem>)internalList).Insert(index, item);
    }

    public IEnumerable<UnityEngine.Object> RandomTake(int count)
    {
        return internalList.WeightedRandomTake(count).Select(item => item.Object);
    }

    public bool Remove(WeightedItem item)
    {
        return ((IList<WeightedItem>)internalList).Remove(item);
    }

    public void RemoveAt(int index)
    {
        ((IList<WeightedItem>)internalList).RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IList<WeightedItem>)internalList).GetEnumerator();
    }
}
