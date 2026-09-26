using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class TwistSubscriber : MonoBehaviour
{
    [SerializeField] private string topicName = "/drone/cmd_vel";
    [SerializeField] private float speedMultiplier = 1.0f;

    private ROSConnection rosConnection;
    private Vector3 velocity;

    private void Start()
    {
        rosConnection = ROSConnection.GetOrCreateInstance();
        rosConnection.Subscribe<TwistMsg>(topicName, ReceiveTwist);
    }

    private void ReceiveTwist(TwistMsg message)
    {
        velocity = new Vector3(
            (float)message.linear.x,
            (float)message.linear.y,
            (float)message.linear.z
        );
    }

    private void Update()
    {
        transform.position += velocity * speedMultiplier * Time.deltaTime;
    }
}