using UnityEngine;

public class DronePickupZone : MonoBehaviour {

    [SerializeField] private BoxCollider pickupZone;
    [SerializeField] private ArticulationBody droneBody;

    private KeyCode pickupKey = KeyCode.F;

    private Pickupable nearbyObject;
    private Pickupable carriedObject;
    private BoxCollider carriedCollider;

    public bool IsCarrying {
        get {
            return carriedObject != null;
        }
    }

    public Pickupable CarriedObject {
        get {
            return carriedObject;
        }
    }

    public BoxCollider CarriedCollider {
        get {
            return carriedCollider;
        }
    }

    private void Update() {
        if (Input.GetKeyDown(pickupKey)) {
            if (carriedObject == null) {
                Pickup();
            } else {
                Drop();
            }
        }
    }

    public bool Pickup() {
        if (nearbyObject == null) {
            Debug.Log("No object close enough to pick up.");
            return false;
        }

        carriedObject = nearbyObject;

        Rigidbody objectBody = carriedObject.GetComponent<Rigidbody>();
        carriedCollider = carriedObject.GetComponent<BoxCollider>();

        if (objectBody != null) {
            objectBody.velocity = Vector3.zero;
            objectBody.angularVelocity = Vector3.zero;

            objectBody.useGravity = false;
            objectBody.isKinematic = true;
        }

        if (carriedCollider != null) {
            carriedCollider.enabled = false;
        }

        carriedObject.transform.SetParent(transform);
        carriedObject.transform.localPosition = Vector3.zero;
        carriedObject.transform.localRotation = Quaternion.identity;

        Debug.Log("Picked up: " + carriedObject.name);
        return carriedObject != null;
    }

    public void Drop() {
        if (carriedObject == null) {
            return;
        }

        Rigidbody objectBody = carriedObject.GetComponent<Rigidbody>();

        carriedObject.transform.SetParent(null);

        if (carriedCollider != null) {
            carriedCollider.enabled = true;
        }

        if (objectBody != null) {
            objectBody.isKinematic = false;
            objectBody.useGravity = true;

            if (droneBody != null) {
                objectBody.velocity = droneBody.velocity;
            }
        }

        Debug.Log("Dropped: " + carriedObject.name);

        carriedObject = null;
        carriedCollider = null;
    }

    private void OnTriggerEnter(Collider other) {
        Pickupable pickupObject = other.GetComponent<Pickupable>();

        if (pickupObject != null && carriedObject == null) {
            nearbyObject = pickupObject;

            Debug.Log(
                "Object in pickup range: " +
                pickupObject.name
            );
        }
    }

    private void OnTriggerExit(Collider other) {
        Pickupable pickupObject = other.GetComponent<Pickupable>();

        if (pickupObject == nearbyObject) {
            nearbyObject = null;
        }
    }
}
