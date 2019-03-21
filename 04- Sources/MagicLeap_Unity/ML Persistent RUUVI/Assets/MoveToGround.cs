using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToGround : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        GameObject _go = GameObject.Find("/Plane");
        //Plane _groundPlane = _go.GetComponent<Plane>();
        transform.position = new Vector3(transform.position.x, _go.transform.position.y, transform.position.x);
    }
}
