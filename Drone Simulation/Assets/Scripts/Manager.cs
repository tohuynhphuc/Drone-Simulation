using System.Collections;
using System.Collections.Generic;
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

    [Header("Navigation")]
    [SerializeField] public float pickupHoverHeight = 0.6f;
    [SerializeField] private float arrivalDistance = 0.2f;
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
            StartCoroutine(SortPackages());
        }
    }

    private IEnumerator SortPackages() {
    moveToPointPublisher.SetTarget(drone.transform);
        yield return new WaitUntil(() => packagesParent != null && packagesParent.transform.childCount >= packagesParent.GetComponent<PackageSpawner>().numberOfSpawns);

        List<Transform> packages = new List<Transform>();

        foreach (Transform package in packagesParent.transform) {
            packages.Add(package);
        }

        foreach (Transform package in packages) {
            if (package == null) continue;

            Pickupable pickupable = package.GetComponent<Pickupable>();

            if (pickupable == null) {
                Debug.LogWarning(package.name + " does not have Pickupable.");
                continue;
            }

            DropZone dropZone = FindDropZone(pickupable.packageType);

            if (dropZone == null) {
                Debug.LogWarning("No drop zone found for package type " + pickupable.packageType);
                continue;
            }

            yield return StartCoroutine(PickupAndDeliver(package, dropZone));
        }

        moveToPointPublisher.ClearTarget();
        Debug.Log("Finished sorting all packages.");
    }

    private IEnumerator PickupAndDeliver(Transform package, DropZone dropZone) {
        Debug.Log("Going to package: " + package.name);

        moveToPointPublisher.SetTarget(package);

        yield return new WaitUntil(() => {
            Vector3 targetPosition = package.position + Vector3.up * pickupHoverHeight;
            return Vector3.Distance(drone.position, targetPosition) < arrivalDistance;
        });

        Debug.Log("Reached package: " + package.name);

        yield return new WaitForSeconds(settleTime);

        bool pickedUp = pickupZone.Pickup();

        if (!pickedUp) {
            Debug.LogWarning("Failed to pick up " + package.name);
            moveToPointPublisher.ClearTarget();
            yield break;
        }

        Debug.Log("Picked up: " + package.name);

        moveToPointPublisher.SetTarget(dropZone.transform);

        Debug.Log("Going to drop zone: " + dropZone.name);

        yield return new WaitUntil(() => {
            Vector3 targetPosition = dropZone.transform.position + Vector3.up * pickupHoverHeight;
            return Vector3.Distance(drone.position, targetPosition) < arrivalDistance;
        });

        Debug.Log("Reached drop zone: " + dropZone.name);

        yield return new WaitForSeconds(settleTime);

        moveToPointPublisher.ClearTarget();

        pickupZone.Drop();

        Debug.Log("Dropped " + package.name + " at " + dropZone.name);

        yield return new WaitForSeconds(settleTime);
    }

    private DropZone FindDropZone(PackageType packageType) {
        foreach (Transform child in dropZonesParent.transform) {
            DropZone dropZone = child.GetComponent<DropZone>();

            if (dropZone != null && dropZone.packageType == packageType) {
                return dropZone;
            }
        }

        return null;
    }
}
