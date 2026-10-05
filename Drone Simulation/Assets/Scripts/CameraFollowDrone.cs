using UnityEngine;

public class CameraFollowDrone : MonoBehaviour {

    [SerializeField] private Vector3 offset;
    [SerializeField] private Vector3 rotationOffset;
    [SerializeField] private Transform drone;

    private void LateUpdate() {
        Quaternion yawRotation = Quaternion.Euler(0f, drone.eulerAngles.y, 0f);

        Vector3 targetPosition = drone.position + yawRotation * offset;
        Quaternion targetRotation = yawRotation * Quaternion.Euler(rotationOffset);

        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

}