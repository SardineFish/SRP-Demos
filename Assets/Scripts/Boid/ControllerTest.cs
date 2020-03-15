using UnityEngine;
using System.Collections;

public class ControllerTest : MonoBehaviour
{
    public Vector3 MaxAcceleration = new Vector3(1, 1, 1);
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var pitch = Input.GetAxis("Vertical");
        var steer = Input.GetAxis("Horizontal");
        var quatSteer = Quaternion.AngleAxis(MaxAcceleration.z * Mathf.Rad2Deg * steer, transform.forward);
        var right = quatSteer * transform.right;
        var quatPitch = Quaternion.AngleAxis(MaxAcceleration.x * Mathf.Rad2Deg * pitch, right);
        transform.rotation = quatPitch * quatSteer * transform.rotation;
        
    }
}
