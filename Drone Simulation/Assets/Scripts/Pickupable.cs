using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickupable : MonoBehaviour {

    [SerializeField] private MeshRenderer renderer;
    [SerializeField] private Material color;

    // Start is called before the first frame update
    private void Start() {
	renderer.material = color;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
