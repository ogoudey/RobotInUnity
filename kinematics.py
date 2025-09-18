import threading

import placo










class Kinematics():
    def __init__(self):
        self.robot =  placo.RobotWrapper("Assets/so101_new_calib.urdf")
        self.joint_names = ["gripper_frame_link", "gripper_link", "wrist_link", "lower_arm_link", "upper_arm_link", "shoulder_link"]
        
        self.solver = placo.KinematicsSolver(robot)
        self.solver.mask_fbase(True)
        self.effector_task = solver.add_frame_task("effector", np.eye(4))
        self.effector_task.configure("effector", "soft", 1.0, 1.0)

    def forward(self, present_joints):
        for i in range(0, len(present_joints)):
            self.robot.set_joint(self.joint_names[i], present_joints[i])
        self.robot.update_kinematics()

        self.solver.solve(True)
        return self.effector_task.T_world_frame
