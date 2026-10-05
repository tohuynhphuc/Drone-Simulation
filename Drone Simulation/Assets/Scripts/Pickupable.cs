using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Pickupable : MonoBehaviour {

    [SerializeField] public PackageType packageType;

    [SerializeField] private MeshRenderer meshRenderer;

    private void Start() {
        SetColor();
    }

    public void SetPackagingType(PackageType newPackageType) {
        packageType = newPackageType;
        SetColor();
    }

    public PackageType GetPackageType() {
        return packageType;
    }

    private void SetColor() {
        meshRenderer.material.color = GetColorFromPackageType(packageType);
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
                return new Color(1, 0f, 0.9f);
            case PackageType.YELLOW:
                return Color.yellow;
        }
        return Color.cyan;
    }
}
