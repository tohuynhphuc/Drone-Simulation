using System.Collections;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

public class MoveToPointPublisher : MonoBehaviour {

    [Header("ROS Topics")]
    [SerializeField] private string goalTopic = "/drone/goal";
    [SerializeField] private string poseTopic = "/drone/pose";

    [Header("Objects")]
    [SerializeField] private Transform drone;

    [Header("Pose Publishing")]
    [SerializeField] private float posePublishRate = 20f;

    private ROSConnection ros;
    private float nextPosePublishTime;
    private bool initialized = false;
    private Coroutine goalPublishCoroutine;
    
    [SerializeField] private Transform target;
[SerializeField] private float goalPublishRate = 10f;

[SerializeField] private float targetHeightOffset = 0.8f;

private float nextGoalPublishTime;

    private void Start() {
        InitializeROS();
    }

    private void InitializeROS() {
        if (initialized || Manager.Instance == null || !Manager.Instance.UseROS) return;

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PointMsg>(goalTopic);
        ros.RegisterPublisher<PoseStampedMsg>(poseTopic);

        initialized = true;
        Debug.Log("MoveToPointPublisher initialized");
    }

    private void Update() {
	    if (Manager.Instance == null || !Manager.Instance.UseROS) return;

	    InitializeROS();
	    if (!initialized) return;

	    if (Time.time >= nextPosePublishTime) {
		PublishPose();
		nextPosePublishTime = Time.time + 1f / posePublishRate;
	    }

	    if (target != null && Time.time >= nextGoalPublishTime) {
		Vector3 goalPosition = target.position + Vector3.up * targetHeightOffset;
PublishGoal(goalPosition);
		nextGoalPublishTime = Time.time + 1f / goalPublishRate;
	    }
	}

    private void PublishPose() {
        PoseStampedMsg message = new PoseStampedMsg();
        message.header.frame_id = "map";
        message.pose.position = drone.position.To<FLU>();
        message.pose.orientation = drone.rotation.To<FLU>();

        ros.Publish(poseTopic, message);
    }

    public void PublishGoal(Vector3 targetPosition) {
    InitializeROS();
    if (!initialized) return;

    PointMsg message = targetPosition.To<FLU>();
    ros.Publish(goalTopic, message);
}

public void SetTarget(Transform newTarget) {
    target = newTarget;
}

    private IEnumerator PublishGoalRoutine(Vector3 targetPosition) {
        PointMsg message = targetPosition.To<FLU>();

        for (int i = 0; i < 10; i++) {
            ros.Publish(goalTopic, message);
            Debug.Log($"Published ROS goal #{i + 1}: ({message.x:F2}, {message.y:F2}, {message.z:F2})");
            yield return new WaitForSeconds(0.2f);
        }

        goalPublishCoroutine = null;
    }
}
