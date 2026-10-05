#!/usr/bin/env python3

import math
import rospy

from geometry_msgs.msg import Twist, Point, PoseStamped
from tf.transformations import euler_from_quaternion

STOP_HORIZONTAL = 0.25
STOP_VERTICAL = 0.04
SLOW_DISTANCE = 2.0

MAX_HORIZONTAL_SPEED = 1.0
MAX_VERTICAL_SPEED = 0.6

YAW_GAIN = 0.8
VERTICAL_GAIN = 0.8


class GoToPoint:
    def __init__(self):
        self.goal = None

        self.x = 0.0
        self.y = 0.0
        self.z = 0.0
        self.yaw = 0.0

        self.cmd_pub = rospy.Publisher("/drone/cmd_vel", Twist, queue_size=10)

        rospy.Subscriber("/drone/goal", Point, self.goal_callback)
        rospy.Subscriber("/drone/pose", PoseStamped, self.pose_callback)

    def goal_callback(self, msg):
        self.goal = msg

    def pose_callback(self, msg):
        self.x = msg.pose.position.x
        self.y = msg.pose.position.y
        self.z = msg.pose.position.z

        q = msg.pose.orientation
        quaternion = (q.x, q.y, q.z, q.w)
        _, _, self.yaw = euler_from_quaternion(quaternion)

        self.update_controller()

    def update_controller(self):
        if self.goal is None:
            return

        dx = self.goal.x - self.x
        dy = self.goal.y - self.y
        dz = self.goal.z - self.z

        horizontal_distance = math.sqrt(dx * dx + dy * dy)

        if horizontal_distance > 0.01:
            desired_yaw = math.atan2(dy, dx)
            yaw_error = desired_yaw - self.yaw
            yaw_error = math.atan2(math.sin(yaw_error), math.cos(yaw_error))
        else:
            yaw_error = 0.0

        cmd = Twist()

        # Goal reached
        if horizontal_distance < STOP_HORIZONTAL and abs(dz) < STOP_VERTICAL:
            cmd.linear.x = 0.0
            cmd.linear.z = 0.0
            cmd.angular.z = 0.0

        # Rotate toward the target first
        elif abs(yaw_error) > math.radians(5):
            cmd.linear.x = 0.0
            cmd.linear.z = 0.0
            cmd.angular.z = YAW_GAIN * yaw_error

        # Fly toward the target
        else:
            cmd.angular.z = 0.0

            # Horizontal speed
            if horizontal_distance < STOP_HORIZONTAL:
                horizontal_speed = 0.0
            elif horizontal_distance < SLOW_DISTANCE:
                horizontal_speed = (horizontal_distance - STOP_HORIZONTAL) / (
                    SLOW_DISTANCE - STOP_HORIZONTAL
                )
                horizontal_speed *= MAX_HORIZONTAL_SPEED
            else:
                horizontal_speed = MAX_HORIZONTAL_SPEED

            # Vertical speed
            vertical_speed = VERTICAL_GAIN * dz
            vertical_speed = max(
                -MAX_VERTICAL_SPEED, min(MAX_VERTICAL_SPEED, vertical_speed)
            )

            if abs(dz) < STOP_VERTICAL:
                vertical_speed = 0.0

            cmd.linear.x = horizontal_speed
            cmd.linear.z = vertical_speed

        self.cmd_pub.publish(cmd)


if __name__ == "__main__":
    rospy.init_node("go_to_point")
    controller = GoToPoint()
    rospy.spin()
