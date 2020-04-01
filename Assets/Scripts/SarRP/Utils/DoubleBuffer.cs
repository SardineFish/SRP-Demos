using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarRP
{
    public class DoubleBuffer<T>
    {
        public int Count { get; private set; }
        T[] buffer;
        int currentIdx = 0;

        public T Current
        {
            get=> buffer[currentIdx];
            set => buffer[currentIdx] = value;
        }
        public T Next
        {
            get => buffer[(currentIdx + 1) % Count];
            set => buffer[(currentIdx + 1) % Count] = value;
        }
        public DoubleBuffer(int capacity = 2)
        {
            buffer = new T[capacity];
            this.Count = capacity;
        }
        public DoubleBuffer(Func<int, T> initFunc, int capacity = 2) : this(capacity)
        {
            for (int i = 0; i < Count; i++)
            {
                buffer[i] = initFunc(i);
            }
        }

        public T Flip()
        {
            currentIdx = (currentIdx + 1) % Count;
            return buffer[currentIdx];
        }
    }
}