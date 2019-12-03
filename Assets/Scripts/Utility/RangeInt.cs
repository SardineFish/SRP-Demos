using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public struct RangeInt
{
    [SerializeField]
    public int start;
    [SerializeField]
    public int end;
    public int length => end - start;
    public RangeInt(int start, int end)
    {
        this.start = start;
        this.end = end;
    }
}