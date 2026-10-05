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
        self.z = 0.0
        self.yaw = 0.0

        self.cmd_pub = rospy.Publisher(
            "/drone/cmd_vel",
            Twist,
            queue_size=10
        )

        rospy.Subscriber(
            "/drone/goal",
            Point,
            self.goal_callback
        )

        rospy.Subscriber(
            "/drone/pose",
            PoseStamped,
            self.pose_callback
        )

    def goal_callback(self, msg):
        self.goal = msg

    def pose_callback(self, msg):
        self.x = msg.pose.position.x
        self.y = msg.pose.position.y
        self.z = msg.pose.position.z

        q = msg.pose.orientation

        quaternion = (
            q.x,
            q.y,
            q.z,
            q.w
        )

        _, _, self.yaw = euler_from_quaternion(
            quaternion
        )

        self.update_controller()

    def update_controller(self):
        if self.goal is None:
            return

        # --------------------------------
        # Distance to target
        # --------------------------------

        dx = self.goal.x - self.x
        dy = self.goal.y - self.y
        dz = self.goal.z - self.z

        horizontal_distance = math.sqrt(
            dx * dx + dy * dy
        )

        distance = math.sqrt(
            dx * dx +
            dy * dy +
            dz * dz
        )

        # --------------------------------
        # Horizontal direction / yaw
        # --------------------------------

        if horizontal_distance > 0.01:
            desired_yaw = math.atan2(dy, dx)

            yaw_error = desired_yaw - self.yaw

            # Normalize to [-pi, pi]
            yaw_error = math.atan2(
                math.sin(yaw_error),
                math.cos(yaw_error)
            )
        else:
            # Target is basically directly above/below
            yaw_error = 0.0

        cmd = Twist()

        STOP_DISTANCE = 0.4
        SLOW_DISTANCE = 2.0

        # --------------------------------
        # 1. Goal reached
        # --------------------------------

        if distance < STOP_DISTANCE:
            cmd.linear.x = 0.0
            cmd.linear.z = 0.0
            cmd.angular.z = 0.0

            self.goal = None

        # --------------------------------
        # 2. Rotate toward target first
        # --------------------------------

        elif abs(yaw_error) > math.radians(5):
            cmd.linear.x = 0.0
            cmd.linear.z = 0.0

            cmd.angular.z = 0.8 * yaw_error

        # --------------------------------
        # 3. Fly directly toward target
        # --------------------------------

        else:
            cmd.angular.z = 0.0

            # Slow down near target
            if distance < SLOW_DISTANCE:
                speed = max(
                    0.05,
                    (distance - STOP_DISTANCE)
                    / (SLOW_DISTANCE - STOP_DISTANCE)
                )
            else:
                speed = 1.0

            # Direction of travel in 3D
            if distance > 0.001:
                horizontal_ratio = (
                    horizontal_distance / distance
                )

                vertical_ratio = (
                    dz / distance
                )

                cmd.linear.x = (
                    speed * horizontal_ratio
                )

                cmd.linear.z = (
                    speed * vertical_ratio
                )

        self.cmd_pub.publish(cmd)


if __name__ == "__main__":
    rospy.init_node("go_to_point")

    controller = GoToPoint()

    rospy.spin()
