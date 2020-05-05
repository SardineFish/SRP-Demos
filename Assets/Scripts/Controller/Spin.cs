using UnityEngine;
using System.Collections;

public class Spin : MonoBehaviour
{
    public Transform Center;
    public Vector3 Axis = Vector3.up;
    public float AngularSpeed;
    void Reset()
    {
        Center = transform;    
    }
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(Center.position, Axis, AngularSpeed * Time.deltaTime);
    }
}
