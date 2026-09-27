using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowDrone : MonoBehaviour
{

	[SerializeField] private Vector3 offset;
	[SerializeField] private Transform drone;

    // Start is called before the first frame update
private    void Start() {
        
    }

    // Update is called once per frame
private    void Update() {
        transform.position = drone.position + offset;
    }
}
