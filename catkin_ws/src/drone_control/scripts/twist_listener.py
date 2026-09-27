#!/usr/bin/env python3

import rospy
from geometry_msgs.msg import Twist

def callback(msg):
    rospy.loginfo(
        "Received Twist: linear=(%.2f, %.2f, %.2f), angular=(%.2f, %.2f, %.2f)",
        msg.linear.x,
        msg.linear.y,
        msg.linear.z,
        msg.angular.x,
        msg.angular.y,
        msg.angular.z
    )

rospy.init_node("twist_listener")

rospy.Subscriber("/drone/cmd_vel", Twist, callback)

rospy.spin()
