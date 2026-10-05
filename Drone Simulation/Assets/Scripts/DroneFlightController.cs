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
    [SerializeField] private float verticalStrength = 4f; // inverse of time for velocity change

    [Header("Physics")]
    [SerializeField] private float linearDamping = 0.5f;
    [SerializeField] private float angularDamping = 5f;

    [Header("Cargo")]
    [SerializeField] private DronePickupZone pickupZone;
    [SerializeField] private float cargoGroundClearance = 0.05f;

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

        calculateTotalMass();

        body.linearDamping = linearDamping;
        body.angularDamping = angularDamping;

        targetYaw = transform.eulerAngles.y;

        if (Manager.Instance.UseROS) {
            ros = ROSConnection.GetOrCreateInstance();
            ros.Subscribe<TwistMsg>(topicName, ReceiveCommand);
        }

        command = new TwistMsg();
        lastCommandTime = -999f;
    }

    private void calculateTotalMass() {
        ArticulationBody[] bodies = transform.root.GetComponentsInChildren<ArticulationBody>();

        totalMass = 0f;

        foreach (ArticulationBody part in bodies) {
            totalMass += part.mass;
        }
    }

    public void ReceiveCommand(TwistMsg message) {
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

        float targetPitch = Mathf.Clamp(forwardError * tiltStrength, -maxTiltAngle, maxTiltAngle);
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

        if (targetVerticalSpeed < 0f && CargoIsNearGround()) {
            targetVerticalSpeed = 0f;

            Vector3 velocity = body.velocity;

            if (velocity.y < 0f) {
                velocity.y = 0f;
                body.velocity = velocity;
            }
        }

        float verticalError = targetVerticalSpeed - body.velocity.y;
        float weight = totalMass * Physics.gravity.magnitude;

        /*
         * When the drone tilts, transform.up is no longer completely
         * vertical, so increase total thrust to compensate.
         */
        // 1 when they match (upright), 0 when orthogonal (sideway)
        float verticalRatio = Vector3.Dot(transform.up, Vector3.up); 
        // drone can only tilt by 60deg
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

    private bool CargoIsNearGround() {
        if (pickupZone == null || !pickupZone.IsCarrying) {
            return false;
        }

        BoxCollider proxy = pickupZone.CarriedCollider;

        if (proxy == null) {
            return false;
        }

        Vector3 center = proxy.transform.TransformPoint(proxy.center);

        Vector3 halfExtents = Vector3.Scale(
            proxy.size * 0.5f,
            AbsVector(proxy.transform.lossyScale)
        );

        Collider[] overlaps = Physics.OverlapBox(
            center,
            halfExtents,
            proxy.transform.rotation,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider collider in overlaps) {
            if (ShouldIgnoreCargoCollision(collider)) {
                continue;
            }

            return true;
        }

        RaycastHit[] hits = Physics.BoxCastAll(
            center,
            halfExtents,
            Vector3.down,
            proxy.transform.rotation,
            cargoGroundClearance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits) {
            if (ShouldIgnoreCargoCollision(hit.collider)) {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool ShouldIgnoreCargoCollision(Collider collider) {
        if (collider == null) {
            return true;
        }

        if (collider.transform.IsChildOf(transform.root)) {
            return true;
        }

        if (
            pickupZone.CarriedObject != null &&
            collider.transform.IsChildOf(pickupZone.CarriedObject.transform)
        ) {
            return true;
        }

        return false;
    }

    private Vector3 AbsVector(Vector3 value) {
        return new Vector3(
            Mathf.Abs(value.x),
            Mathf.Abs(value.y),
            Mathf.Abs(value.z)
        );
    }
}