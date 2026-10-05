using System.Collections;
using UnityEngine;

public class Manager : MonoBehaviour {
    public static Manager Instance { get; private set; }

    [SerializeField] private GameObject packagesParent;
    [SerializeField] private GameObject dropZonesParent;

    [SerializeField] private bool useROS = true;

    [Header("Drone")]
    [SerializeField] private Transform drone;
    [SerializeField] private MoveToPointPublisher moveToPointPublisher;
    [SerializeField] private DronePickupZone pickupZone;

    [Header("Pickup Test")]
    [SerializeField] private float pickupHoverHeight = 0.8f;
    [SerializeField] private float arrivalDistance = 0.5f;
    [SerializeField] private float settleTime = 0.5f;

    public bool UseROS {
        get { return useROS; }
        set { useROS = value; }
    }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        if (useROS) {
            StartCoroutine(PickupFirstPackageTest());
        }
    }

    private IEnumerator PickupFirstPackageTest() {
        // Wait until packages exist
        yield return new WaitUntil(() => packagesParent != null && packagesParent.transform.childCount > 0);

        Transform package = packagesParent.transform.GetChild(0);
        Debug.Log("Going to package: " + package.name);

        Vector3 hoverPosition = package.position + Vector3.up * pickupHoverHeight;
        moveToPointPublisher.SetTarget(package);

        yield return new WaitUntil(() => Vector3.Distance(drone.position, hoverPosition) < arrivalDistance);
        Debug.Log("Drone reached package.");
        yield return new WaitForSeconds(settleTime);

        bool pickedUp = pickupZone.Pickup();

        if (pickedUp) {
            Debug.Log("SUCCESS: Picked up " +package.name);
        } else {
            Debug.LogWarning("FAILED: Drone reached package, " +"but package was not inside pickup zone.");
        }

        // STOP HERE FOR NOW
        yield break;
    }
}
