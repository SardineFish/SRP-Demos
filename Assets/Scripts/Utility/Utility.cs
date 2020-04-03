using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public static class Utility
{
    public static void ForEach<T>(this IEnumerable<T> ts, Action<T> callback)
    {
        foreach (var item in ts)
            callback(item);
    }
    public static IEnumerable<T> WeightedRandomTake<T>(this IEnumerable<T> list, int count) where T : IWeightedObject
    {
        var source = list.ToArray();
        var idxMap = source.Select((item, idx) => idx).ToArray();
        var weightSum = source.Sum(item => item.Weight);

        for(var i = 0; i < count; i++)
        {
            var totalWeight = weightSum;
            for (var j = 0; j < source.Length - i; j++)
            {
                if (UnityEngine.Random.value < (float)source[idxMap[j]].Weight / (float)totalWeight)
                {
                    yield return source[idxMap[j]];

                    // Update the total weight of all rest items.
                    weightSum -= source[idxMap[j]].Weight;
                    // Put the last item to the position where the item was taken.
                    idxMap[j] = idxMap[source.Length - i - 1];
                    break;
                }
                else
                    totalWeight -= source[idxMap[j]].Weight;
            }
        }
    }

    public static IEnumerable<T> RandomTake<T>(this IEnumerable<T> list,int count)
    {
        var source = list.ToArray();

        for (var i = 0; i < count; i++)
        {
            var idx = UnityEngine.Random.Range(0, source.Length - i);
            yield return source[idx];
            source[idx] = source[count - i - 1];
        }
    }

    public static IEnumerable<GameObject> GetChildren(this GameObject gameObject)
    {
        for(var i = 0; i < gameObject.transform.childCount; i++)
        {
            yield return gameObject.transform.GetChild(i).gameObject;
        }
    }


    public static IEnumerable<U> Map<T, U>(this IEnumerable<T> collection, Func<T, U> callback) => collection.Select(callback);

    public static TResult MinOf<T, TCompare, TResult>(this IEnumerable<T> collection, Func<T, TCompare> comparerSelector, Func<T, TResult> resultSelector)
        where TCompare: IComparable
    {
        bool hasFirstValue = false;
        var minValue = default(TCompare);
        TResult result = default(TResult);
        foreach(var element in collection)
        {
            var value = comparerSelector(element);
            if(!hasFirstValue)
            {
                minValue = value;
                result = resultSelector(element);
                hasFirstValue = true;
            }
            else if(minValue.CompareTo(value) > 0)
            {
                minValue = value;
                result = resultSelector(element);
            }
        }
        return result;
    }

    public static TResult MaxOf<T, TCompare, TResult>(this IEnumerable<T> collection, Func<T, TCompare> comparerSelector, Func<T, TResult> resultSelector)
        where TCompare : IComparable
    {
        bool hasFirstValue = false;
        var minValue = default(TCompare);
        TResult result = default(TResult);
        foreach (var element in collection)
        {
            var value = comparerSelector(element);
            if (!hasFirstValue)
            {
                minValue = value;
                result = resultSelector(element);
                hasFirstValue = true;
            }
            else if (minValue.CompareTo(value) < 0)
            {
                minValue = value;
                result = resultSelector(element);
            }
        }
        return result;
    }

    public static IEnumerable<T> NotNull<T>(this IEnumerable<T> source)
    {
        foreach(var item in source)
        {
            if (item != null)
                yield return item;
        }
    }
    
    public static TResult Merge<TResult,Tkey,TElement>(this IGrouping<Tkey,TElement> group, TResult mergeTarget, Func<TElement,TResult,TResult> mergeFunc)
    {
        foreach(var element in group)
        {
            mergeTarget = mergeFunc(element, mergeTarget);
        }
        return mergeTarget;
    }

    public static GameObject Instantiate(this UnityEngine.Object self, GameObject original, Scene scene)
    {
        var obj = UnityEngine.Object.Instantiate(original);
        SceneManager.MoveGameObjectToScene(obj, scene);
        return obj;
    }

    public static GameObject Instantiate(GameObject original, Scene scene)
    {
        var obj = UnityEngine.Object.Instantiate(original);
        SceneManager.MoveGameObjectToScene(obj, scene);
        return obj;
    }
    public static GameObject Instantiate(GameObject original, GameObject parent)
    {
        var obj = Instantiate(original, parent.scene);
        obj.transform.parent = parent.transform;
        return obj;
    }

    public static GameObject Instantiate(GameObject original, GameObject parent, Vector3 relativePosition,Quaternion relativeRotation)
    {
        var obj = Instantiate(original, parent);
        obj.transform.localPosition = relativePosition;
        obj.transform.localRotation = relativeRotation;
        return obj;
    }

    public static void ClearChildren(this GameObject self)
    {
        self.GetChildren().ForEach((child)=>
        {
            child.ClearChildren();
            GameObject.Destroy(child);
        });
    }

    public static void ClearChildImmediate(this GameObject self)
    {
        while (self.transform.childCount > 0)
        {
            var obj = self.transform.GetChild(0).gameObject;
            obj.ClearChildImmediate();
            GameObject.DestroyImmediate(obj);
        }
    }

    public static void SetLayerRecursive(this GameObject gameObject,int layer)
    {
        gameObject.layer = layer;
        gameObject.GetChildren().ForEach(child => child.SetLayerRecursive(layer));
    }

    public static void NextFrame(this MonoBehaviour context, Action callback)
    {
        context.StartCoroutine(NextFrameCoroutine(callback));
    }

    public static IEnumerator NextFrameCoroutine(Action callback)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        callback?.Invoke();
    }

    public static Coroutine NumericAnimate(this MonoBehaviour context, float time, Action<float> tick, Action complete = null)
    {
        return context.StartCoroutine(NumericAnimateEnumerator(time, tick, complete));
    }

    public static IEnumerator NumericAnimateEnumerator(float time, Action<float> callback, Action complete)
    {
        var startTime = Time.time;
        for (float t = 0; t < time; t = Time.time - startTime)
        {
            callback?.Invoke(t / time);
            yield return new WaitForEndOfFrame();
        }
        callback?.Invoke(1);
        complete?.Invoke();
    }

    public static Coroutine WaitForSecond(this MonoBehaviour context, Action callback, float seconds = 0)
    {
        return context.StartCoroutine(WaitForSecondEnumerator(callback, seconds));
    }

    public static Coroutine SetInterval(this MonoBehaviour context, Action callback, float seconds = 0)
    {
        return context.StartCoroutine(IntervalCoroutine(callback, seconds));
    }

    public static IEnumerator IntervalCoroutine(Action callback, float seconds)
    {
        while (true)
        {
            yield return new WaitForSeconds(seconds);
            callback?.Invoke();
        }
    }

    public static IEnumerator WaitForSecondEnumerator(Action callback,float seconds = 0)
    {
        yield return new WaitForSeconds(seconds);
        callback?.Invoke();
    }

    public static IEnumerable<float> Timer(float time)
    {
        for (var startTime = Time.time; Time.time < startTime + time;)
        {
            yield return Time.time - startTime;
        }
        yield return time;
    }
    public static IEnumerable<float> TimerNormalized(float time)
    {
        foreach (var t in Timer(time))
        {
            yield return t / time;
        }
    }

    public class CallbackYieldInstruction : CustomYieldInstruction
    {
        Func<bool> callback;
        public override bool keepWaiting => callback?.Invoke() ?? true;

        public CallbackYieldInstruction(Func<bool> callback)
        {
            this.callback = callback;
        }
    }

    public static IEnumerator ShowUI(UnityEngine.UI.Graphic ui, float time, float targetAlpha = 1)
    {
        var color = ui.color;
        color.a = 0;
        ui.gameObject.SetActive(true);
        foreach(var t in TimerNormalized(time))
        {
            color.a = t * targetAlpha;
            ui.color = color;
            yield return null;
        }
    }
    public static IEnumerator HideUI(UnityEngine.UI.Graphic ui, float time, bool deactive = false)
    {
        var color = ui.color;
        color.a = 1;
        foreach (var t in TimerNormalized(time))
        {
            color.a = 1 - t;
            ui.color = color;
            yield return null;
        }
        if (deactive)
            ui.gameObject.SetActive(false);
    }
    public static Matrix4x4 ProjectionToWorldMatrix(Camera camera)
    {
        return (camera.projectionMatrix * camera.worldToCameraMatrix).inverse;
    }

    public static Mesh GenerateFullScreenQuad()
    {
        var mesh = new Mesh();
        mesh.Clear();
        mesh.vertices = new Vector3[]
        {
                    new Vector3(-1,-1,0),
                    new Vector3(1,-1,0),
                    new Vector3(1,1,0),
                    new Vector3(-1,1,0),
        };
        mesh.triangles = new int[]
        {
                    0, 2, 1,
                    0, 3, 2
        };
        mesh.uv = new Vector2[]
        {
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                    new Vector2(1, 0),
                    new Vector2(0, 0),
        };
        return mesh;
    }
}