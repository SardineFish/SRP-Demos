using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

[Serializable]
public struct AngularRange
{
    [SerializeField]
    public float low;
    [SerializeField]
    public float high;

    public float length
    {
        get
        {
            if (low > high)
            {
                return (180 - low) + (high + 180);
            }
            else
            {
                return high - low;
            }
        }
    }

    public AngularRange(float low, float high)
    {
        // Map any angle to [-180,180]
        low -= ((int)low / 360) * 360;
        high -= ((int)low / 360) * 360;
        if (low > 180)
            low -= 360;
        if (high > 180)
            high -= 360;
        if (low < -180)
            low += 360;
        if (high < -180)
            high += 360;

        this.low = low;
        this.high = high;
    }

    public float Limit(float ang)
    {
        // Map any angle to [-180,180]
        ang -= ((int)low / 360) * 360;
        if (ang > 180)
            ang -= 360;
        else if (ang < -180)
            ang += 360;

        // The range cross 180
        if (low > high)
        {
            // Out of range
            if (ang < low && ang > high)
            {
                if (Mathf.Abs(ang - high) < Mathf.Abs(ang - low))
                    return high;
                return low;

            }
        }
        else
        {
            if (ang > high)
                return high;
            else if (ang < low)
                return low;
        }
        return ang;
    }

    public override string ToString()
    {
        return "[" + low.ToString() + "," + high.ToString() + "]";
    }
}