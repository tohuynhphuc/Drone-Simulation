using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

public class TwistPublisher : MonoBehaviour
{
    [SerializeField] private string topicName = "/drone/cmd_vel";

    private ROSConnection rosConnection;

    private void Start()
    {

        Debug.Log("TwistPublisher START: " + gameObject.name);
        rosConnection = ROSConnection.GetOrCreateInstance();
        rosConnection.RegisterPublisher<TwistMsg>(topicName);
    }

    public void Publish()
    {
        var message = new TwistMsg();

        message.linear.x = 1.0;
        message.linear.y = 0.0;
        message.linear.z = 0.0;

        message.angular.x = 0.0;
        message.angular.y = 0.0;
        message.angular.z = 0.0;

        rosConnection.Publish(topicName, message);

        Debug.Log("Published");
    }
}