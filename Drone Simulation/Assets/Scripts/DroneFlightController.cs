using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class DroneFlightController : MonoBehaviour {
    [SerializeField] private string topicName = "/drone/cmd_vel";

    [Header("Horizontal Movement")]
    [SerializeField] private float maxHorizontalSpeed = 3f;
    [SerializeField] private float tiltStrength = 8f;
    [SerializeField] private float maxTiltAngle = 20f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float maxYawRate = 90f;

    [Header("Vertical Movement")]
    [SerializeField] private float maxVerticalSpeed = 2f;
    [SerializeField] private float verticalStrength = 4f;

    [Header("Physics")]
    [SerializeField] private float linearDamping = 0.5f;
    [SerializeField] private float angularDamping = 5f;

    [Header("ROS")]
    [SerializeField] private float commandTimeout = 0.5f;

    private ROSConnection ros;
    private ArticulationBody body;
    private TwistMsg command;

    private float totalMass;
    private float targetYaw;
    private float lastCommandTime;

    private void Start() {
        body = GetComponent<ArticulationBody>();

        if (!body.isRoot) {
            Debug.LogError("DroneFlightController must be attached to the root ArticulationBody!");
        }

        ArticulationBody[] bodies = transform.root.GetComponentsInChildren<ArticulationBody>();

        totalMass = 0f;

        foreach (ArticulationBody part in bodies) {
            totalMass += part.mass;
        }

        body.linearDamping = linearDamping;
        body.angularDamping = angularDamping;

        targetYaw = transform.eulerAngles.y;

        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<TwistMsg>(topicName, ReceiveCommand);

        command = new TwistMsg();
        lastCommandTime = -999f;

        Debug.Log("Total drone mass: " + totalMass);
    }

    private void ReceiveCommand(TwistMsg message) {
        command = message;
        lastCommandTime = Time.time;
    }

    private void FixedUpdate() {
        float forwardCommand = 0f;
        float sidewaysCommand = 0f;
        float verticalCommand = 0f;
        float yawCommand = 0f;

        if (Time.time - lastCommandTime <= commandTimeout) {
            forwardCommand = Mathf.Clamp((float)command.linear.x, -1f, 1f);
            sidewaysCommand = Mathf.Clamp((float)command.linear.y, -1f, 1f);
            verticalCommand = Mathf.Clamp((float)command.linear.z, -1f, 1f);
            yawCommand = Mathf.Clamp((float)command.angular.z, -1f, 1f);
        }

        ControlRotation(forwardCommand, sidewaysCommand, yawCommand);
        ControlThrust(verticalCommand);
    }

    private void ControlRotation(float forwardCommand, float sidewaysCommand, float yawCommand) {
        float targetForwardSpeed = forwardCommand * maxHorizontalSpeed;
        float targetLeftSpeed = sidewaysCommand * maxHorizontalSpeed;

        Vector3 localVelocity = transform.InverseTransformDirection(body.velocity);

        float currentForwardSpeed = localVelocity.z;
        float currentLeftSpeed = -localVelocity.x;

        float forwardError = targetForwardSpeed - currentForwardSpeed;
        float leftError = targetLeftSpeed - currentLeftSpeed;

        /*
         * Forward velocity error -> pitch.
         *
         * Positive pitch tilts the drone forward.
         */
        float targetPitch = Mathf.Clamp(forwardError * tiltStrength, -maxTiltAngle, maxTiltAngle);

        /*
         * Positive roll tilts the drone toward its left side,
         * matching ROS +Y = left.
         */
        float targetRoll = Mathf.Clamp(leftError * tiltStrength, -maxTiltAngle, maxTiltAngle);

        targetYaw += yawCommand * maxYawRate * Time.fixedDeltaTime;

        Quaternion targetRotation = Quaternion.Euler(targetPitch, targetYaw, targetRoll);

        Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

        body.TeleportRoot(transform.position, newRotation);

        /*
         * We are directly controlling attitude, so don't allow
         * leftover angular physics to fight against us.
         */
        body.angularVelocity = Vector3.zero;
    }

    private void ControlThrust(float verticalCommand) {
        float targetVerticalSpeed = verticalCommand * maxVerticalSpeed;

        float verticalError = targetVerticalSpeed - body.velocity.y;

        float weight = totalMass * Physics.gravity.magnitude;

        /*
         * When the drone tilts, transform.up is no longer completely
         * vertical, so increase total thrust to compensate.
         */
        float verticalRatio = Vector3.Dot(transform.up, Vector3.up);

        verticalRatio = Mathf.Clamp(verticalRatio, 0.4f, 1f);

        float hoverForce = weight / verticalRatio;

        float verticalCorrection = verticalError * verticalStrength * totalMass;

        float totalThrust = hoverForce + verticalCorrection;

        totalThrust = Mathf.Max(0f, totalThrust);

        /*
         * Thrust always points out the top of the drone.
         *
         * When the drone tilts, this naturally creates horizontal
         * movement.
         */
        body.AddForce(transform.up * totalThrust);
    }
}
