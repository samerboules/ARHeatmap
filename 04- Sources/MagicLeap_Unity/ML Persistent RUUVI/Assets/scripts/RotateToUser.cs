using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotate : MonoBehaviour {

    private Camera _camera;
    // Use this for initialization
    void Start () {
        _camera = Camera.main;
    }
	
	// Update is called once per frame
	void Update () {
        // Rotate the object to face the main camera.
        float rotSpeed = Time.deltaTime * 5f;
        Quaternion rotTo = Quaternion.LookRotation(transform.position - _camera.transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotTo, rotSpeed);
    }
}
