using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class DroneKeyboardPublisher : MonoBehaviour {

    [SerializeField] private string cmdKeyboardTopic = "/drone/cmd_keyboard_vel";
    [SerializeField] private DroneFlightController controller;

    private ROSConnection ros;

    private void Start() {
        if (Manager.Instance.UseROS) {
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<TwistMsg>(cmdKeyboardTopic);
        }
    }

    private void FixedUpdate() {
        TwistMsg message = new TwistMsg();

        // Forward / backward
        if (Input.GetKey(KeyCode.W)) {
            message.linear.x = 1.0;
        }

        if (Input.GetKey(KeyCode.S)) {
            message.linear.x = -1.0;
        }

        // Left / right
        if (Input.GetKey(KeyCode.A)) {
            message.linear.y = 1.0;
        }

        if (Input.GetKey(KeyCode.D)) {
            message.linear.y = -1.0;
        }

        // Up / down
        if (Input.GetKey(KeyCode.Space)) {
            message.linear.z = 1.0;
        }

        if (Input.GetKey(KeyCode.LeftControl)) {
            message.linear.z = -1.0;
        }

        // Yaw left / right
        if (Input.GetKey(KeyCode.Q)) {
            message.angular.z = -1.0;
        }

        if (Input.GetKey(KeyCode.E)) {
            message.angular.z = 1.0;
        }

        if (Manager.Instance.UseROS) {
            ros.Publish(cmdKeyboardTopic, message);
        } else {
            controller.ReceiveCommand(message);
        }
    }
}
