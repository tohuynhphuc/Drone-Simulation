using RosMessageTypes.Geometry;
using System.Collections;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class DroneFlightController : MonoBehaviour {

    [SerializeField] private string TopicName = "/drone/cmd_vel";
    [SerializeField] private float movementForce = 5f;
    [SerializeField] private float verticalForce = 5f;
    [SerializeField] private float rotationTorque = 2f;

    private ROSConnection ros;
    private ArticulationBody body;
    private TwistMsg command;

    private void Start() {
        body = GetComponent<ArticulationBody>();
        ros = ROSConnection.GetOrCreateInstance();

        ros.Subscribe<TwistMsg>(TopicName, ReceiveCommand);
        command = new TwistMsg();
    }

    private void ReceiveCommand(TwistMsg message) {
        command = message;
    }

    private void FixedUpdate() {
        Vector3 force = transform.forward * (float) command.linear.x * movementForce - transform.right * (float) command.linear.y * movementForce + transform.up * (float) command.linear.z * verticalForce;

        body.AddForce(force);

        Vector3 torque = transform.up * -(float) command.angular.z * rotationTorque;
        body.AddTorque(torque);
    }
}
