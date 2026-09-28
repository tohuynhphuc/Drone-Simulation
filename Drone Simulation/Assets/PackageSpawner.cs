using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PackageSpawner : MonoBehaviour {

    [SerializeField] private float halfWidth = 5f;
    [SerializeField] private float halfLength = 5f;
    [SerializeField] private float halfHeight = 1f;

    [SerializeField] private int numberOfSpawns = 40;

    [SerializeField] private Pickupable packagePrefab;

    private void Start() {
        StartCoroutine(SpawnPackages());
    }

    private IEnumerator SpawnPackages() {
        Vector3 originalPosition = transform.position;

        for (int i = 0; i < numberOfSpawns; i++) {
            GameObject package = Instantiate(packagePrefab.gameObject);

            Vector3 randomOffset = new Vector3(
                (Random.value * 2 - 1) * halfWidth,
                (Random.value * 2 - 1) * halfHeight,
                (Random.value * 2 - 1) * halfWidth
            );

            package.transform.position = originalPosition + randomOffset;

            PackageType randomType = (PackageType) Random.Range(
                0,
                System.Enum.GetValues(typeof(PackageType)).Length
            );

            package.GetComponent<Pickupable>().SetPackagingType(randomType);

            yield return new WaitForSeconds(0.2f);
        }
    }

}
