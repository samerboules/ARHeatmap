using UnityEngine;


public class CopyPosition : MonoBehaviour {

	public Transform target;


	void Update () {

		this.transform.position = this.target.position;
	
	}
}
