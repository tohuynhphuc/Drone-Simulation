using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropZone : MonoBehaviour {

    [SerializeField] private PackageType packageType;
    [SerializeField] private MeshRenderer meshRenderer;


    private void Start() {
        SetColor();
    }

    private void OnTriggerEnter(Collider other) {
        Pickupable box = other.GetComponent<Pickupable>();

        if (box == null) {
            return;
        }

        if (box.GetPackageType() == packageType) {
            Debug.Log("Correct box!");
        } else {
            Debug.Log("Wrong zone!");
        }
    }

    public void SetPackagingType(PackageType newPackageType) {
        packageType = newPackageType;
        SetColor();
    }

    private void SetColor() {
        Color color = GetColorFromPackageType(packageType);
        meshRenderer.material.color = new Color(color.r, color.g, color.b, 0.2f);
    }

    private Color GetColorFromPackageType(PackageType type) {
        switch (type) {
            case PackageType.RED:
                return Color.red;
            case PackageType.GREEN:
                return Color.green;
            case PackageType.BLUE:
                return Color.blue;
            case PackageType.PINK:
                return new Color(1, 0, 0.9f);
            case PackageType.YELLOW:
                return Color.yellow;
        }
        return Color.cyan;
    }

    public PackageType GetPackageType() {
        return packageType;
    }

}
