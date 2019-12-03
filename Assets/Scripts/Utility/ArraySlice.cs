using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ArraySlice<T>: IEnumerable<T>
{
    T[] array;
    int from;
    int to;
    public ArraySlice(int size)
    {
        array = new T[size];
        from = 0;
        to = size;
    }
    public ArraySlice(T[] array, int from, int to)
    {
        this.array = array;
        this.from = from;
        this.to = to;
    }
    public T this[int idx]
    {
        get
        {
            if (idx < from || idx >= to)
                throw new IndexOutOfRangeException();
            return array[from + idx];
        }
        set
        {
            if (idx < from || idx >= to)
                throw new IndexOutOfRangeException();
            array[from + idx] = value;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = from; i < to; i++)
        {
            yield return array[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}

public class ArrayPool<T>
{
    T[] array;
    public ArrayPool(int size)
    {
        array = new T[size];
    }
    public ArrayPool(T[] array)
    {
        this.array = array;
    }
}


public static class ArraySliceHelper
{
    public static ArraySlice<T> GetSlice<T>(this T[] array, int from, int to)
    {
        return new ArraySlice<T>(array, from, to);
    }
}