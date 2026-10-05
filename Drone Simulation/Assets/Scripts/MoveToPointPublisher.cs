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
    [SerializeField] private Transform target;

    [Header("Pose Publishing")]
    [SerializeField] private float posePublishRate = 20f;

    private ROSConnection ros;
    private float nextPosePublishTime;

    private void Start() {
        if (!Manager.Instance.UseROS) {
            return;
        }

        ros = ROSConnection.GetOrCreateInstance();

        ros.RegisterPublisher<PointMsg>(goalTopic);
        ros.RegisterPublisher<PoseStampedMsg>(poseTopic);
    }

    private void Update() {
        if (!Manager.Instance.UseROS) {
            return;
        }

        if (Time.time >= nextPosePublishTime) {
            PublishPose();

            nextPosePublishTime = Time.time + 1f / posePublishRate;
        }

        if (Input.GetKeyDown(KeyCode.G)) {
            PublishGoal();
        }
    }

    private void PublishPose() {
        PoseStampedMsg message = new PoseStampedMsg();

        message.header.frame_id = "map";
        message.pose.position = drone.position.To<FLU>();
        message.pose.orientation = drone.rotation.To<FLU>();

        ros.Publish(poseTopic, message);
    }

    private void PublishGoal() {
        PointMsg message = target.position.To<FLU>();
        ros.Publish(goalTopic, message);
        Debug.Log($"Published goal: {target.position}");
    }
}