import socket
import threading

HOST = '127.0.0.1'  # Standard loopback interface address (localhost)
PORT = 5005

class ThroughThread(threading.Thread):
    def __init__(self, shared):
        super().__init__()
        self.lock = threading.Lock()
        self.shared = shared
    """
    def convert_args(self, shared):
        args = shared["joints"]
        print(type(args))
        if type(args) == list:
            print(type(args[0]))
            if type(args[0]) == float:
                return {"joints": str(args)}
            else:
                new_list = []
                for e in args:
                    new_list.append(float(e))
                return {"joints": str(new_list)}
        else:
            try:
                return self.convert_args(list(args))
            except Exception:
                raise ValueError("Cannot make sense of input array")
    """ 
    def run(self):
        while True:
            try:
                while True:
                    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
                        s.connect((HOST, PORT))
                        with self.lock:
                            joints = str(self.shared["joints"])
                            print("Sending", joints)
                            s.sendall(bytes(joints, 'utf-8'))
                        data = s.recv(1024)
                    print(f"Received {data!r}")
            except KeyboardInterrupt:
                joints = input("Joints:\n") + "\n" 
            except ConnectionRefusedError:
                print(f"Socket {HOST}:{PORT} is down.")


import time
import math
def main():
    shared = {"joints": [45.0, 45.0, 45.0, 45.0, 45.0, 45.0, 45.0, 45.0]}
    through_thread = ThroughThread(shared)
    through_thread.start()
    
    s = 0
    while True:
        for i in range(1, 7):
            for t in range(0, 33):
                time.sleep(0.1)
                with through_thread.lock:
                    new_joints = [0.0, 45.0, 45.0, 45.0, 45.0, 45.0, 45.0, 45.0]
                    new_joints[i] = math.degrees(math.sin(s))
                    shared["joints"] = new_joints
                s += 1
if __name__ == "__main__":
    main()
