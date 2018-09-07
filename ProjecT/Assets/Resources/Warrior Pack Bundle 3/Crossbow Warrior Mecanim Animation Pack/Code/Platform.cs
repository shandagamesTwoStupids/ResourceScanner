using UnityEngine;
using System.Collections;

public class Platform : MonoBehaviour {
	Vector3 previous;
	[HideInInspector]
	public Vector3 velocity;
	public float platformSpeed = 20f;
	public float plaformRotateSpeed = 30f;
	public float minHeight = 1.567745f;
	public float maxHeight = 5f;
	public bool animated = false;
	bool raising = true;
	
	void Update(){
		if(animated){
			velocity = ((transform.position - previous)) / Time.deltaTime;
			previous = transform.position;
		}
		else{
			GetComponent<Rigidbody>().velocity += transform.forward * platformSpeed * Time.deltaTime;
			Vector3 eulerAngleVelocity = new Vector3(0, plaformRotateSpeed, 0);
			Quaternion rotate = Quaternion.Euler(eulerAngleVelocity * Time.deltaTime);
			GetComponent<Rigidbody>().MoveRotation(GetComponent<Rigidbody>().rotation * rotate);
			if(transform.position.y <= maxHeight && raising){
				GetComponent<Rigidbody>().velocity += transform.up * platformSpeed * Time.deltaTime;
			}
			if(transform.position.y >= maxHeight){
				raising = false;
			}
			if(!raising){
				GetComponent<Rigidbody>().velocity += -transform.up * platformSpeed * Time.deltaTime;
			}
			if(transform.position.y <= minHeight){
				GetComponent<Rigidbody>().velocity += transform.up * platformSpeed * Time.deltaTime;
				raising = true;
			}
		}
	}
}
