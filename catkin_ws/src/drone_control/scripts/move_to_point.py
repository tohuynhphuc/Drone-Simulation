#!/usr/bin/env python3

import rospy
import math

from geometry_msgs.msg import Twist, Point, PoseStamped
from tf.transformations import euler_from_quaternion


class GoToPoint:
    def __init__(self):
        self.goal = None

        self.x = 0.0
        self.y = 0.0
        self.yaw = 0.0

        self.cmd_pub = rospy.Publisher("/drone/cmd_vel", Twist, queue_size=10)

        rospy.Subscriber("/drone/goal", Point, self.goal_callback)

        rospy.Subscriber("/drone/pose", PoseStamped, self.pose_callback)

    def goal_callback(self, msg):
        self.goal = msg

    def pose_callback(self, msg):
        self.x = msg.pose.position.x
        self.y = msg.pose.position.y

        q = msg.pose.orientation

        quaternion = (q.x, q.y, q.z, q.w)

        _, _, self.yaw = euler_from_quaternion(quaternion)

        self.update_controller()

    def update_controller(self):

        if self.goal is None:
            return

        dx = self.goal.x - self.x
        dy = self.goal.y - self.y

        distance = math.sqrt(dx * dx + dy * dy)

        desired_yaw = math.atan2(dy, dx)

        yaw_error = desired_yaw - self.yaw

        # Normalize to [-pi, pi]
        yaw_error = math.atan2(math.sin(yaw_error), math.cos(yaw_error))

        cmd = Twist()

        # Goal reached
        if distance < 0.2:
            cmd.linear.x = 0
            cmd.angular.z = 0

        # Face target first
        elif abs(yaw_error) > math.radians(5):
            cmd.linear.x = 0
            cmd.angular.z = 0.8 * yaw_error

        # Then fly forward
        else:
            cmd.linear.x = min(1.0, distance)
            cmd.angular.z = 0

        self.cmd_pub.publish(cmd)


if __name__ == "__main__":
    rospy.init_node("go_to_point")

    controller = GoToPoint()

    rospy.spin()
