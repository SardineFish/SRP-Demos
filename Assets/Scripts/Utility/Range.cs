using System;
using UnityEngine;

[Serializable]
public struct Range
{
    [SerializeField]
    public float min;
    [SerializeField]
    public float max;
    public float length => max - min;
    public Range(float min,float max)
    {
        this.min = min;
        this.max = max;
    }
}