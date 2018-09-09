using UnityEngine;
using System.Collections;

public class TestChar : MonoBehaviour {

	// Use this for initialization
	void Start () {
		char[] char1 = new char[]{ '1', '2', '3', '4', '5' };
		char[] char2 = new char[char1.Length];
		int i = 0;
		while (i < char1.Length && char1 [i] != '5') {
			char2 [i] = char1 [i];
			Debug.Log (char2[i]);
			Debug.Log (char1[i]);
			i++;
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
