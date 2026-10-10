using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;

[RequireComponent(typeof(WarehouseMapGenerator))]
public class WarehouseMapPublisher : MonoBehaviour
{
    public string topicName = "/map";

    private ROSConnection ros;
    private WarehouseMapGenerator mapGenerator;

    void Start()
	{
	    mapGenerator = GetComponent<WarehouseMapGenerator>();

	    ros = ROSConnection.GetOrCreateInstance();

	    ros.RegisterPublisher<OccupancyGridMsg>(
		topicName,
		queue_size: 1,
		latch: true
	    );

	    mapGenerator.GenerateMap();

	    Invoke(nameof(PublishMap), 2.0f);
	}

    [ContextMenu("Publish Map")]
    public void PublishMap()
    {
        if (mapGenerator == null)
            mapGenerator = GetComponent<WarehouseMapGenerator>();

        if (mapGenerator.OccupiedGrid == null)
        {
            Debug.LogWarning("No occupancy grid exists.");
            return;
        }

        /*
         * Unity:
         *
         * X = right
         * Z = forward
         *
         * ROS:
         *
         * X = forward
         * Y = left
         *
         * Therefore:
         *
         * ROS X = Unity Z
         * ROS Y = -Unity X
         */

        int rosWidth = mapGenerator.GridHeight;
        int rosHeight = mapGenerator.GridWidth;

        sbyte[] data = new sbyte[rosWidth * rosHeight];

        for (int unityX = 0;
             unityX < mapGenerator.GridWidth;
             unityX++)
        {
            for (int unityZ = 0;
                 unityZ < mapGenerator.GridHeight;
                 unityZ++)
            {
                int rosX = unityZ;

                // Unity +X becomes ROS -Y
                int rosY =
                    mapGenerator.GridWidth - 1 - unityX;

                int rosIndex =
                    rosY * rosWidth + rosX;

                data[rosIndex] =
                    mapGenerator.OccupiedGrid[unityX, unityZ]
                    ? (sbyte)100
                    : (sbyte)0;
            }
        }

        OccupancyGridMsg mapMsg =
            new OccupancyGridMsg();

        mapMsg.header.frame_id = "map";

        mapMsg.info.resolution =
            mapGenerator.cellSize;

        mapMsg.info.width =
            (uint)rosWidth;

        mapMsg.info.height =
            (uint)rosHeight;

        /*
         * Determine the ROS position of the
         * bottom-left corner of the converted map.
         */

        Vector3 unityOrigin =
            mapGenerator.MapOrigin;

        float unityMaxX =
            unityOrigin.x +
            mapGenerator.GridWidth *
            mapGenerator.cellSize;

        double rosOriginX =
            unityOrigin.z;

        double rosOriginY =
            -unityMaxX;

        mapMsg.info.origin.position.x =
            rosOriginX;

        mapMsg.info.origin.position.y =
            rosOriginY;

        mapMsg.info.origin.position.z =
            0.0;

        mapMsg.info.origin.orientation.x = 0.0;
        mapMsg.info.origin.orientation.y = 0.0;
        mapMsg.info.origin.orientation.z = 0.0;
        mapMsg.info.origin.orientation.w = 1.0;

        mapMsg.data = data;

        ros.Publish(topicName, mapMsg);

        Debug.Log(
            $"Published ROS map: " +
            $"{rosWidth} x {rosHeight}, " +
            $"resolution = {mapGenerator.cellSize}"
        );
    }
}
