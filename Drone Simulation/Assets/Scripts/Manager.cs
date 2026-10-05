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
        if (useROS) StartCoroutine(SortPackages());
    }

    private IEnumerator SortPackages() {
        moveToPointPublisher.ClearTarget();

        yield return new WaitUntil(() =>
            packagesParent != null &&
            packagesParent.transform.childCount >= packagesParent.GetComponent<PackageSpawner>().numberOfSpawns
        );

        while (packagesParent.transform.childCount > 0) {
            Transform targetPackage = packagesParent.transform.GetChild(0);
            yield return StartCoroutine(PickupAndDeliver(targetPackage));
        }

        moveToPointPublisher.ClearTarget();
        Debug.Log("Finished sorting all packages.");
    }

    private IEnumerator PickupAndDeliver(Transform targetPackage) {
        Debug.Log("Going toward package position: " + targetPackage.name);

        moveToPointPublisher.SetTarget(targetPackage);

        yield return new WaitUntil(() => {
            if (targetPackage == null) return true;

            Vector3 targetPosition = targetPackage.position + Vector3.up * pickupHoverHeight;
            return Vector3.Distance(drone.position, targetPosition) < arrivalDistance;
        });

        yield return new WaitForSeconds(settleTime);

        bool pickedUp = pickupZone.Pickup();

        if (!pickedUp || pickupZone.CarriedObject == null) {
            Debug.LogWarning("Failed to pick up a package.");
            moveToPointPublisher.ClearTarget();
            yield break;
        }

        GameObject actualPackage = pickupZone.CarriedObject.gameObject;
        Pickupable pickupable = pickupZone.CarriedObject;

        if (pickupable == null) {
            Debug.LogWarning("Picked up object " + actualPackage.name + " does not have Pickupable.");
            moveToPointPublisher.ClearTarget();
            pickupZone.Drop();
            yield break;
        }

        Debug.Log("Actually picked up: " + actualPackage.name + " (" + pickupable.packageType + ")");

        DropZone dropZone = FindDropZone(pickupable.packageType);

        if (dropZone == null) {
            Debug.LogWarning("No drop zone found for package type " + pickupable.packageType);
            moveToPointPublisher.ClearTarget();
            pickupZone.Drop();
            yield break;
        }

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

        Debug.Log("Dropped " + actualPackage.name + " at " + dropZone.name);

        yield return new WaitForSeconds(settleTime);
    }

    private DropZone FindDropZone(PackageType packageType) {
        foreach (Transform child in dropZonesParent.transform) {
            DropZone dropZone = child.GetComponent<DropZone>();

            if (dropZone != null && dropZone.packageType == packageType) return dropZone;
        }

        return null;
    }
}
