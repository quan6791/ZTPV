#!/usr/bin/env python3

import socket
import threading
import json
import time
import math
import os
import paramiko
from paramiko import ServerInterface, Transport, SFTPServer, SFTPServerInterface, SFTPHandle, SFTPAttributes
import sys
import io
import logging
import select
import traceback

# Setup logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class RobotSimulationServer:
    """Complete Robot Simulation Server with all rblib3.py functions"""
    
    def __init__(self, host='localhost', port=12345):
        self.host = host
        self.port = port
        self.sock = None
        self.running = False
        
        # Robot state simulation
        self.current_joint_position = [0.0, 0.0, 0.0, 0.0, 0.0, 0.0]
        self.current_cartesian_position = [0.0, 0.0, 500.0, 0.0, 0.0, 0.0]  # x,y,z,rz,ry,rx (mm, deg)
        self.current_posture = 0
        self.current_rb_coord = 1
        self.current_tool = 1
        self.servo_on = False
        self.motion_id = 1000
        self.is_moving = False
        self.permission_acquired = False
        self.current_speed = 50.0
        self.linear_joint_support = True
        
        # Motion queue simulation
        self.motion_queue = []
        self.motion_lock = threading.Lock()
        
        # IO simulation
        self.digital_outputs = [False] * 32
        self.digital_inputs = [False] * 32
        
        # Tool data simulation
        self.tools = {
            1: {'x': 0, 'y': 0, 'z': 100, 'rz': 0, 'ry': 0, 'rx': 0},
            2: {'x': 0, 'y': 0, 'z': 150, 'rz': 0, 'ry': 0, 'rx': 0},
        }
        
        # File management for FilesListView
        self.robot_files = [
            "__init__",
            "calib_teach", 
            "init",
            "init_util",
            "io_test",
            "main",
            "network",
            "network_test_client",
            "selftest",
            "tp_1",
            "twim_test"
        ]
        
        # Command codes (complete list from rblib3.py)
        self.NOP = 1
        self.SVSW = 2
        self.PLSMOVE = 3
        self.MTRMOVE = 4
        self.JNTMOVE = 5
        self.PTPMOVE = 6
        self.CPMOVE = 7
        self.SET_TOOL = 8
        self.CHANGE_TOOL = 9
        self.ASYNCM = 10
        self.PASSM = 11
        self.OVERLAP = 12
        self.MARK = 13
        self.JMARK = 14
        self.IOCTRL = 15
        self.ZONE = 16
        self.J2R = 17
        self.R2J = 18
        self.SYSSTS = 19
        self.TRMOVE = 20
        self.ABORTM = 21
        self.JOINM = 22
        self.SLSPEED = 23
        self.ACQ_PERMISSION = 24
        self.REL_PERMISSION = 25
        self.SET_MDO = 26
        self.ENABLE_MDO = 27
        self.DISABLE_MDO = 28
        self.PMARK = 29
        self.VERSION = 30
        self.ENCRST = 31
        self.SAVEPARAMS = 32
        self.CALCPLSOFFSET = 33
        self.SET_LOG_LEVEL = 34
        self.FSCTRL = 35
        self.PTPPLAN = 36
        self.CPPLAN = 37
        self.PTPPLAN_W_SP = 38
        self.CPPLAN_W_SP = 39
        self.SYSCTRL = 40
        self.OPTCPMOVE = 41
        self.OPTCPPLAN = 42
        self.PTPMOVE_MT = 43
        self.MARK_MT = 44
        self.J2R_MT = 45
        self.R2J_MT = 46
        self.PTPPLAN_MT = 47
        self.PTPPLAN_W_SP_MT = 48
        self.JNTRMOVE = 49
        self.JNTRMOVE_WO_CHK = 50
        self.CPRMOVE = 51
        self.CPRPLAN = 52
        self.CPRPLAN_W_SP = 53
        self.SUSPENDM = 54
        self.RESUMEM = 55
        self.GETMT = 56
        self.RELBRK = 57
        self.CLPBRK = 58
        self.ARCMOVE = 59
        self.CIRMOVE = 60
        self.MMARK = 61
        
        # Custom commands for FilesListView and MotionControlView
        self.GET_FILE_LIST = 1000
        self.GET_FILE_CONTENT = 1001
        self.EXECUTE_FILE = 1002
        self.UPLOAD_FILE = 1003
        self.DELETE_FILE = 1004
        
        # Jog commands for MotionControlView
        self.JOG_STOP = 2000
        self.JOG_RZ_POSITIVE = 2001
        self.JOG_RZ_NEGATIVE = 2002
        self.JOG_RY_POSITIVE = 2003
        self.JOG_RY_NEGATIVE = 2004
        self.JOG_RX_POSITIVE = 2005
        self.JOG_RX_NEGATIVE = 2006
        self.JOG_Z_POSITIVE = 2007
        self.JOG_Z_NEGATIVE = 2008
        self.JOG_Y_POSITIVE = 2009
        self.JOG_Y_NEGATIVE = 2010
        self.JOG_X_POSITIVE = 2011
        self.JOG_X_NEGATIVE = 2012

    def start(self):
        """Start the robot simulation server"""
        try:
            self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            self.sock.bind((self.host, self.port))
            self.sock.listen(10)
            self.running = True
            
            logger.info(f"🤖 Robot Simulation Server started on {self.host}:{self.port}")
            
            while self.running:
                try:
                    client_sock, client_addr = self.sock.accept()
                    logger.info(f"Robot client connected from {client_addr}")
                    client_thread = threading.Thread(target=self.handle_client, args=(client_sock, client_addr))
                    client_thread.daemon = True
                    client_thread.start()
                except Exception as e:
                    if self.running:
                        logger.error(f"Error accepting robot client: {e}")
                        
        except Exception as e:
            logger.error(f"Robot server startup error: {e}")

    def handle_client(self, client_sock, client_addr):
        """Handle robot client connection"""
        try:
            while self.running:
                # Set timeout for socket operations
                client_sock.settimeout(1.0)
                
                try:
                    data = client_sock.recv(4096)
                    if not data:
                        break
                    
                    request = data.decode('utf-8', errors='ignore')
                    logger.info(f"Robot RX from {client_addr}: {request[:100]}...")
                    
                    response = self.process_command(request)
                    if response:
                        client_sock.send(response.encode('utf-8'))
                        logger.info(f"Robot TX to {client_addr}: {response[:100]}...")
                        
                except socket.timeout:
                    continue  # Continue loop on timeout
                except Exception as e:
                    logger.error(f"Robot client communication error: {e}")
                    break
                    
        except Exception as e:
            logger.error(f"Robot client handler error: {e}")
        finally:
            try:
                client_sock.close()
            except:
                pass
            logger.info(f"Robot client {client_addr} disconnected")

    def process_command(self, request):
        """Process robot command and return response"""
        try:
            cmd_data = json.loads(request)
            cmd = cmd_data.get('cmd', 0)
            params = cmd_data.get('params', [])
            
            # Command dispatch
            command_handlers = {
                # Standard robot commands
                self.NOP: self.handle_nop,
                self.SVSW: self.handle_servo_control,
                self.PLSMOVE: self.handle_pulse_move,
                self.MTRMOVE: self.handle_motor_move,
                self.JNTMOVE: self.handle_joint_move,
                self.PTPMOVE: self.handle_ptp_move,
                self.CPMOVE: self.handle_cp_move,
                self.MARK: self.handle_mark,
                self.JMARK: self.handle_jmark,
                self.J2R: self.handle_j2r,
                self.R2J: self.handle_r2j,
                self.SYSSTS: self.handle_sys_status,
                self.ACQ_PERMISSION: self.handle_acq_permission,
                self.REL_PERMISSION: self.handle_rel_permission,
                self.VERSION: self.handle_version,
                self.SET_TOOL: self.handle_set_tool,
                self.CHANGE_TOOL: self.handle_change_tool,
                self.ABORTM: self.handle_abort_motion,
                self.JOINM: self.handle_join_motion,
                self.SLSPEED: self.handle_sl_speed,
                self.PMARK: self.handle_pmark,
                self.ENCRST: self.handle_encoder_reset,
                self.SAVEPARAMS: self.handle_save_params,
                self.IOCTRL: self.handle_io_control,
                self.TRMOVE: self.handle_tr_move,
                self.PTPPLAN: self.handle_ptp_plan,
                self.CPPLAN: self.handle_cp_plan,
                self.SUSPENDM: self.handle_suspend_motion,
                self.RESUMEM: self.handle_resume_motion,
                self.RELBRK: self.handle_release_brake,
                self.CLPBRK: self.handle_clamp_brake,
                self.ARCMOVE: self.handle_arc_move,
                self.CIRMOVE: self.handle_circular_move,
                self.MMARK: self.handle_multi_mark,
                
                # File management commands for FilesListView
                self.GET_FILE_LIST: self.handle_get_file_list,
                self.GET_FILE_CONTENT: self.handle_get_file_content,
                self.EXECUTE_FILE: self.handle_execute_file,
                self.UPLOAD_FILE: self.handle_upload_file,
                self.DELETE_FILE: self.handle_delete_file,
                
                # Jog commands for MotionControlView
                self.JOG_STOP: self.handle_jog_stop,
                self.JOG_RZ_POSITIVE: self.handle_jog_rz_positive,
                self.JOG_RZ_NEGATIVE: self.handle_jog_rz_negative,
                self.JOG_RY_POSITIVE: self.handle_jog_ry_positive,
                self.JOG_RY_NEGATIVE: self.handle_jog_ry_negative,
                self.JOG_RX_POSITIVE: self.handle_jog_rx_positive,
                self.JOG_RX_NEGATIVE: self.handle_jog_rx_negative,
                self.JOG_Z_POSITIVE: self.handle_jog_z_positive,
                self.JOG_Z_NEGATIVE: self.handle_jog_z_negative,
                self.JOG_Y_POSITIVE: self.handle_jog_y_positive,
                self.JOG_Y_NEGATIVE: self.handle_jog_y_negative,
                self.JOG_X_POSITIVE: self.handle_jog_x_positive,
                self.JOG_X_NEGATIVE: self.handle_jog_x_negative,
            }
            
            handler = command_handlers.get(cmd)
            if handler:
                return handler(params)
            else:
                logger.warning(f"Unknown command: {cmd}")
                return self.create_error_response(cmd, f"Unknown command: {cmd}")
                
        except json.JSONDecodeError as e:
            logger.error(f"JSON decode error: {e}")
            return self.create_error_response(0, f"JSON parse error: {str(e)}")
        except Exception as e:
            logger.error(f"Command processing error: {e}")
            logger.error(traceback.format_exc())
            return self.create_error_response(0, f"Processing error: {str(e)}")

    # ==================== FILE MANAGEMENT HANDLERS FOR FILESLISTVIEW ====================
    
    def handle_get_file_list(self, params=None):
        """Get list of robot program files"""
        try:
            # Format files with numbering like in the UI
            formatted_files = []
            for i, filename in enumerate(self.robot_files, 1):
                formatted_files.append(f"{i:02d}. {filename}")
            
            logger.info(f"Returning {len(formatted_files)} files")
            return self.create_success_response(self.GET_FILE_LIST, [True] + formatted_files)
        except Exception as e:
            logger.error(f"Error getting file list: {e}")
            return self.create_error_response(self.GET_FILE_LIST, f"Failed to get file list: {str(e)}")

    def handle_get_file_content(self, params):
        """Get content of a specific file"""
        try:
            if not params:
                return self.create_error_response(self.GET_FILE_CONTENT, "No filename provided")
            
            filename = params[0]
            
            # Remove numbering if present (e.g., "01. __init__" -> "__init__")
            if '. ' in filename:
                filename = filename.split('. ', 1)[1]
            
            # Simulate file content based on filename
            content = self.generate_file_content(filename)
            
            logger.info(f"Returning content for file: {filename}")
            return self.create_success_response(self.GET_FILE_CONTENT, [True, content])
            
        except Exception as e:
            logger.error(f"Error getting file content: {e}")
            return self.create_error_response(self.GET_FILE_CONTENT, f"Failed to get file content: {str(e)}")

    def handle_execute_file(self, params):
        """Execute a robot program file"""
        try:
            if not params:
                return self.create_error_response(self.EXECUTE_FILE, "No filename provided")
            
            filename = params[0]
            
            # Remove numbering if present
            if '. ' in filename:
                filename = filename.split('. ', 1)[1]
            
            logger.info(f"Executing file: {filename}")
            
            # Simulate file execution
            execution_time = 2.0  # Simulate 2 second execution
            self.simulate_file_execution(filename, execution_time)
            
            return self.create_success_response(self.EXECUTE_FILE, [True, f"Executing {filename}"])
            
        except Exception as e:
            logger.error(f"Error executing file: {e}")
            return self.create_error_response(self.EXECUTE_FILE, f"Failed to execute file: {str(e)}")

    def handle_upload_file(self, params):
        """Upload a new file to robot"""
        try:
            if len(params) < 2:
                return self.create_error_response(self.UPLOAD_FILE, "Filename and content required")
            
            filename = params[0]
            content = params[1]
            
            # Add to file list if not exists
            if filename not in self.robot_files:
                self.robot_files.append(filename)
            
            logger.info(f"File uploaded: {filename} ({len(content)} bytes)")
            return self.create_success_response(self.UPLOAD_FILE, [True, f"File {filename} uploaded successfully"])
            
        except Exception as e:
            logger.error(f"Error uploading file: {e}")
            return self.create_error_response(self.UPLOAD_FILE, f"Failed to upload file: {str(e)}")

    def handle_delete_file(self, params):
        """Delete a file from robot"""
        try:
            if not params:
                return self.create_error_response(self.DELETE_FILE, "No filename provided")
            
            filename = params[0]
            
            # Remove numbering if present
            if '. ' in filename:
                filename = filename.split('. ', 1)[1]
            
            if filename in self.robot_files:
                self.robot_files.remove(filename)
                logger.info(f"File deleted: {filename}")
                return self.create_success_response(self.DELETE_FILE, [True, f"File {filename} deleted successfully"])
            else:
                return self.create_error_response(self.DELETE_FILE, f"File {filename} not found")
            
        except Exception as e:
            logger.error(f"Error deleting file: {e}")
            return self.create_error_response(self.DELETE_FILE, f"Failed to delete file: {str(e)}")

    def generate_file_content(self, filename):
        """Generate simulated file content based on filename"""
        content_templates = {
            "__init__": """#!/usr/bin/env python3
# Robot initialization file
import sys
import time

def initialize_robot():
    print("Initializing robot system...")
    # Initialize servo motors
    # Set default positions
    # Check safety systems
    print("Robot initialization complete")

if __name__ == '__main__':
    initialize_robot()
""",
            "calib_teach": """#!/usr/bin/env python3
# Calibration and teaching routines
import rblib3 as rb

def calibration_routine():
    print("Starting calibration routine...")
    # Move to calibration positions
    # Record reference points
    # Calculate offsets
    print("Calibration complete")

def teaching_mode():
    print("Entering teaching mode...")
    # Enable manual positioning
    # Record waypoints
    # Save trajectory
    print("Teaching complete")

if __name__ == '__main__':
    calibration_routine()
    teaching_mode()
""",
            "main": """#!/usr/bin/env python3
# Main robot program
import rblib3 as rb

def main_program():
    print("Starting main robot program...")
    
    # Initialize robot
    rb.acquire_permission()
    rb.servo_on()
    
    # Main operation loop
    for i in range(10):
        print(f"Cycle {i+1}")
        # Add your robot operations here
        time.sleep(1)
    
    # Cleanup
    rb.servo_off()
    rb.release_permission()
    print("Main program complete")

if __name__ == '__main__':
    main_program()
""",
            "io_test": """#!/usr/bin/env python3
# I/O testing routines
import rblib3 as rb
import time

def test_digital_io():
    print("Testing digital I/O...")
    
    # Test outputs
    for port in range(8):
        rb.set_digital_output(port, True)
        time.sleep(0.5)
        rb.set_digital_output(port, False)
    
    # Read inputs
    for port in range(8):
        state = rb.get_digital_input(port)
        print(f"Input {port}: {state}")

if __name__ == '__main__':
    test_digital_io()
""",
            "network_test_client": """#!/usr/bin/env python3
# Network connectivity test
import socket
import time

def test_network_connection():
    print("Testing network connection...")
    
    try:
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.connect(('localhost', 12345))
        sock.send(b'test_message')
        response = sock.recv(1024)
        print(f"Network test successful: {response}")
        sock.close()
    except Exception as e:
        print(f"Network test failed: {e}")

if __name__ == '__main__':
    test_network_connection()
"""
        }
        
        return content_templates.get(filename, f"""#!/usr/bin/env python3
# Robot program: {filename}
# This is a simulated robot program file

import rblib3 as rb
import time

def main():
    print(f"Executing robot program: {filename}")
    
    # Add your robot operations here
    time.sleep(1)
    
    print(f"Program {filename} completed")

if __name__ == '__main__':
    main()
""")

    def simulate_file_execution(self, filename, duration):
        """Simulate file execution"""
        def execution_complete():
            time.sleep(duration)
            logger.info(f"File execution completed: {filename}")
        
        execution_thread = threading.Thread(target=execution_complete)
        execution_thread.daemon = True
        execution_thread.start()

    # ==================== JOG HANDLERS FOR MOTIONCONTROLVIEW ====================
    
    def handle_jog_stop(self, params):
        """Stop all jog motion"""
        self.is_moving = False
        logger.info("Jog motion stopped")
        return self.create_success_response(self.JOG_STOP, [True, "Jog stopped"])

    def handle_jog_rz_positive(self, params):
        """Jog Rz axis in positive direction"""
        logger.info("Jogging Rz positive")
        self.current_joint_position[5] += 1.0  # Simulate small increment
        self.current_cartesian_position[3] += 1.0  # Update Rz in cartesian
        self.simulate_jog_motion("Rz+")
        return self.create_success_response(self.JOG_RZ_POSITIVE, [True, "Jog Rz positive started"])

    def handle_jog_rz_negative(self, params):
        """Jog Rz axis in negative direction"""
        logger.info("Jogging Rz negative")
        self.current_joint_position[5] -= 1.0
        self.current_cartesian_position[3] -= 1.0
        self.simulate_jog_motion("Rz-")
        return self.create_success_response(self.JOG_RZ_NEGATIVE, [True, "Jog Rz negative started"])

    def handle_jog_ry_positive(self, params):
        """Jog Ry axis in positive direction"""
        logger.info("Jogging Ry positive")
        self.current_joint_position[4] += 1.0
        self.current_cartesian_position[4] += 1.0
        self.simulate_jog_motion("Ry+")
        return self.create_success_response(self.JOG_RY_POSITIVE, [True, "Jog Ry positive started"])

    def handle_jog_ry_negative(self, params):
        """Jog Ry axis in negative direction"""
        logger.info("Jogging Ry negative")
        self.current_joint_position[4] -= 1.0
        self.current_cartesian_position[4] -= 1.0
        self.simulate_jog_motion("Ry-")
        return self.create_success_response(self.JOG_RY_NEGATIVE, [True, "Jog Ry negative started"])

    def handle_jog_rx_positive(self, params):
        """Jog Rx axis in positive direction"""
        logger.info("Jogging Rx positive")
        self.current_joint_position[3] += 1.0
        self.current_cartesian_position[5] += 1.0
        self.simulate_jog_motion("Rx+")
        return self.create_success_response(self.JOG_RX_POSITIVE, [True, "Jog Rx positive started"])

    def handle_jog_rx_negative(self, params):
        """Jog Rx axis in negative direction"""
        logger.info("Jogging Rx negative")
        self.current_joint_position[3] -= 1.0
        self.current_cartesian_position[5] -= 1.0
        self.simulate_jog_motion("Rx-")
        return self.create_success_response(self.JOG_RX_NEGATIVE, [True, "Jog Rx negative started"])

    def handle_jog_z_positive(self, params):
        """Jog Z axis in positive direction"""
        logger.info("Jogging Z positive")
        self.current_cartesian_position[2] += 5.0  # 5mm increment
        self.simulate_jog_motion("Z+")
        return self.create_success_response(self.JOG_Z_POSITIVE, [True, "Jog Z positive started"])

    def handle_jog_z_negative(self, params):
        """Jog Z axis in negative direction"""
        logger.info("Jogging Z negative")
        self.current_cartesian_position[2] -= 5.0
        self.simulate_jog_motion("Z-")
        return self.create_success_response(self.JOG_Z_NEGATIVE, [True, "Jog Z negative started"])

    def handle_jog_y_positive(self, params):
        """Jog Y axis in positive direction"""
        logger.info("Jogging Y positive")
        self.current_cartesian_position[1] += 5.0
        self.simulate_jog_motion("Y+")
        return self.create_success_response(self.JOG_Y_POSITIVE, [True, "Jog Y positive started"])

    def handle_jog_y_negative(self, params):
        """Jog Y axis in negative direction"""
        logger.info("Jogging Y negative")
        self.current_cartesian_position[1] -= 5.0
        self.simulate_jog_motion("Y-")
        return self.create_success_response(self.JOG_Y_NEGATIVE, [True, "Jog Y negative started"])

    def handle_jog_x_positive(self, params):
        """Jog X axis in positive direction"""
        logger.info("Jogging X positive")
        self.current_cartesian_position[0] += 5.0
        self.simulate_jog_motion("X+")
        return self.create_success_response(self.JOG_X_POSITIVE, [True, "Jog X positive started"])

    def handle_jog_x_negative(self, params):
        """Jog X axis in negative direction"""
        logger.info("Jogging X negative")
        self.current_cartesian_position[0] -= 5.0
        self.simulate_jog_motion("X-")
        return self.create_success_response(self.JOG_X_NEGATIVE, [True, "Jog X negative started"])

    def simulate_jog_motion(self, direction):
        """Simulate jog motion for specified direction"""
        self.is_moving = True
        
        def jog_complete():
            time.sleep(0.1)  # Very short jog movement
            self.is_moving = False
            logger.debug(f"Jog motion {direction} completed")
        
        jog_thread = threading.Thread(target=jog_complete)
        jog_thread.daemon = True
        jog_thread.start()

    # ==================== STANDARD ROBOT COMMAND HANDLERS ====================
    
    def handle_nop(self, params):
        """No operation"""
        return self.create_success_response(self.NOP, [True])

    def handle_servo_control(self, params):
        """Servo control on/off"""
        sw = params[0] if params else 0
        self.servo_on = sw == 1
        logger.info(f"Servo control: {'ON' if self.servo_on else 'OFF'}")
        return self.create_success_response(self.SVSW, [True])

    def handle_pulse_move(self, params):
        """Pulse move command"""
        pulses = params[0]
        speed = params[1] if len(params) > 1 else 50.0
        acct = params[2] if len(params) > 2 else 1.0
        dacct = params[3] if len(params) > 3 else 1.0
        
        # Convert pulses to joint angles (simplified)
        for i in range(min(6, len(pulses))):
            self.current_joint_position[i] = pulses[i] / 1000.0  # Simplified conversion
        
        self.motion_id += 1
        self.simulate_motion(2.0)  # 2 second motion
        logger.info(f"Pulse move to: {pulses}, motion ID: {self.motion_id}")
        return self.create_success_response(self.PLSMOVE, [True, self.motion_id])

    def handle_motor_move(self, params):
        """Motor move command"""
        angles = params[0]
        speed = params[1] if len(params) > 1 else 50.0
        acct = params[2] if len(params) > 2 else 1.0
        dacct = params[3] if len(params) > 3 else 1.0
        
        # Convert radians to degrees
        for i in range(min(6, len(angles))):
            self.current_joint_position[i] = math.degrees(angles[i])
        
        self.motion_id += 1
        self.simulate_motion(2.0)
        logger.info(f"Motor move to: {[math.degrees(a) for a in angles]}, motion ID: {self.motion_id}")
        return self.create_success_response(self.MTRMOVE, [True, self.motion_id])

    def handle_joint_move(self, params):
        """Joint move command"""
        joints = params[0]
        speed = params[1] if len(params) > 1 else 50.0
        acct = params[2] if len(params) > 2 else 1.0
        dacct = params[3] if len(params) > 3 else 1.0
        
        # Update joint positions
        for i in range(min(6, len(joints))):
            if self.linear_joint_support:
                self.current_joint_position[i] = joints[i]  # Direct assignment for linear joints
            else:
                self.current_joint_position[i] = math.degrees(joints[i])  # Convert from radians
        
        self.motion_id += 1
        self.simulate_motion(2.0)
        logger.info(f"Joint move to: {joints}, motion ID: {self.motion_id}")
        return self.create_success_response(self.JNTMOVE, [True, self.motion_id])

    def handle_ptp_move(self, params):
        """Point-to-point move command"""
        position = params[0]
        speed = params[1] if len(params) > 1 else 50.0
        acct = params[2] if len(params) > 2 else 1.0
        dacct = params[3] if len(params) > 3 else 1.0
        
        # Update cartesian position
        self.current_cartesian_position[0] = position[0] * 1000  # m to mm
        self.current_cartesian_position[1] = position[1] * 1000
        self.current_cartesian_position[2] = position[2] * 1000
        self.current_cartesian_position[3] = math.degrees(position[3])  # rad to deg
        self.current_cartesian_position[4] = math.degrees(position[4])
        self.current_cartesian_position[5] = math.degrees(position[5])
        self.current_posture = position[6] if len(position) > 6 else 0
        self.current_rb_coord = position[7] if len(position) > 7 else 1
        
        self.motion_id += 1
        self.simulate_motion(3.0)  # 3 second motion for PTP
        logger.info(f"PTP move to: {position}, motion ID: {self.motion_id}")
        return self.create_success_response(self.PTPMOVE, [True, self.motion_id])

    def handle_cp_move(self, params):
        """Continuous path move command"""
        position = params[0]
        speed = params[1] if len(params) > 1 else 50.0
        acct = params[2] if len(params) > 2 else 1.0
        dacct = params[3] if len(params) > 3 else 1.0
        
        # Similar to PTP move but with continuous path
        self.current_cartesian_position[0] = position[0] * 1000
        self.current_cartesian_position[1] = position[1] * 1000
        self.current_cartesian_position[2] = position[2] * 1000
        self.current_cartesian_position[3] = math.degrees(position[3])
        self.current_cartesian_position[4] = math.degrees(position[4])
        self.current_cartesian_position[5] = math.degrees(position[5])
        
        self.motion_id += 1
        self.simulate_motion(2.5)
        logger.info(f"CP move to: {position}, motion ID: {self.motion_id}")
        return self.create_success_response(self.CPMOVE, [True, self.motion_id])

    def handle_mark(self, params=None):
        """Get current cartesian position"""
        return self.create_success_response(self.MARK, [
            True,
            self.current_cartesian_position[0] / 1000.0,  # mm to m
            self.current_cartesian_position[1] / 1000.0,
            self.current_cartesian_position[2] / 1000.0,
            math.radians(self.current_cartesian_position[3]),  # deg to rad
            math.radians(self.current_cartesian_position[4]),
            math.radians(self.current_cartesian_position[5]),
            self.current_posture,
            self.current_rb_coord
        ])

    def handle_jmark(self, params=None):
        """Get current joint position"""
        if self.linear_joint_support:
            joints = self.current_joint_position[:]
        else:
            joints = [math.radians(j) for j in self.current_joint_position]
        
        return self.create_success_response(self.JMARK, [True] + joints)

    def handle_j2r(self, params):
        """Joint to cartesian conversion (forward kinematics)"""
        joints = params[:6]
        
        # Simplified forward kinematics calculation
        # In real robot, this would be complex DH parameters calculation
        x = joints[0] * 0.01 + joints[1] * 0.3 * math.cos(math.radians(joints[1]))
        y = joints[0] * 0.01 + joints[2] * 0.25 * math.sin(math.radians(joints[2]))
        z = 0.5 + joints[1] * 0.2 + joints[2] * 0.15
        rz = math.radians(joints[3])
        ry = math.radians(joints[4])
        rx = math.radians(joints[5])
        
        return self.create_success_response(self.J2R, [
            True, x, y, z, rz, ry, rx, 0, 1
        ])

    def handle_r2j(self, params):
        """Cartesian to joint conversion (inverse kinematics)"""
        x, y, z, rz, ry, rx = params[:6]
        posture = params[6] if len(params) > 6 else 0
        rb_coord = params[7] if len(params) > 7 else 1
        
        # Simplified inverse kinematics calculation
        j1 = x * 100 + y * 50  # Simplified
        j2 = math.degrees(math.atan2(y, x))
        j3 = (z - 0.5) * 6.67  # Simplified
        j4 = math.degrees(rz)
        j5 = math.degrees(ry)
        j6 = math.degrees(rx)
        
        return self.create_success_response(self.R2J, [
            True, j1, j2, j3, j4, j5, j6
        ])

    def handle_sys_status(self, params):
        """System status query"""
        status_type = params[0] if params else 1
        
        status_responses = {
            1: [True, 0],  # EMS status: 0=normal
            2: [True, self.motion_id],  # Last motion ID
            3: [True, 0],  # INC mode axes bitfield
            4: [True, 0],  # ABS lost axes bitfield
            5: [True, len(self.motion_queue)],  # Motion request count
            6: [True, 1 if self.is_moving else 0],  # Motion state
            7: [True, 1 if self.servo_on else 0],  # Servo state
            8: [True, self.current_tool],  # Current tool
            9: [True, 1 if self.permission_acquired else 0],  # Permission state
        }
        
        response = status_responses.get(status_type, [True, 0])
        return self.create_success_response(self.SYSSTS, response)

    def handle_acq_permission(self, params=None):
        """Acquire robot permission"""
        self.permission_acquired = True
        logger.info("Robot permission acquired")
        return self.create_success_response(self.ACQ_PERMISSION, [True])

    def handle_rel_permission(self, params=None):
        """Release robot permission"""
        self.permission_acquired = False
        logger.info("Robot permission released")
        return self.create_success_response(self.REL_PERMISSION, [True])

    def handle_version(self, params=None):
        """Get robot controller version"""
        return self.create_success_response(self.VERSION, [
            True, 1, 8, 5, 20231128, "2023-11-28 10:30:00"
        ])

    def handle_set_tool(self, params):
        """Set tool offset data"""
        tool_id = params[0]
        if len(params) > 1:
            tool_data = params[1:7]  # x, y, z, rz, ry, rx
            self.tools[tool_id] = {
                'x': tool_data[0], 'y': tool_data[1], 'z': tool_data[2],
                'rz': tool_data[3], 'ry': tool_data[4], 'rx': tool_data[5]
            }
        
        logger.info(f"Tool {tool_id} offset set")
        return self.create_success_response(self.SET_TOOL, [True])

    def handle_change_tool(self, params):
        """Change current tool"""
        tool_id = params[0]
        self.current_tool = tool_id
        logger.info(f"Changed to tool {tool_id}")
        return self.create_success_response(self.CHANGE_TOOL, [True])

    def handle_abort_motion(self, params=None):
        """Abort current motion"""
        self.is_moving = False
        with self.motion_lock:
            self.motion_queue.clear()
        logger.info("All motions aborted")
        return self.create_success_response(self.ABORTM, [True, self.motion_id])

    def handle_join_motion(self, params=None):
        """Wait for motion completion"""
        # In simulation, always return completed
        return self.create_success_response(self.JOINM, [True, self.motion_id])

    def handle_sl_speed(self, params):
        """Set speed limit"""
        speed = params[0]
        self.current_speed = speed
        logger.info(f"Speed limit set to {speed}%")
        return self.create_success_response(self.SLSPEED, [True, speed])

    def handle_pmark(self, params):
        """Pulse mark command"""
        sw = params[0] if params else 0
        
        if sw == 0:
            # Return available pmark types
            return self.create_success_response(self.PMARK, [
                True, "pcurp,pcmdp,curpos.pls,lastgoal.pls"
            ])
        else:
            # Convert current joint positions to pulses
            pulses = [int(pos * 1000) for pos in self.current_joint_position]
            return self.create_success_response(self.PMARK, [True] + pulses)

    def handle_encoder_reset(self, params):
        """Reset encoder"""
        bitfield = params[0] if params else 0x3F  # All axes
        logger.info(f"Encoder reset, bitfield: 0x{bitfield:02X}")
        return self.create_success_response(self.ENCRST, [True])

    def handle_save_params(self, params=None):
        """Save parameters to robot controller"""
        logger.info("Parameters saved to robot controller")
        return self.create_success_response(self.SAVEPARAMS, [True])

    def handle_io_control(self, params):
        """IO control command"""
        io_type = params[0]  # 0=output, 1=input
        port = params[1] if len(params) > 1 else 0
        value = params[2] if len(params) > 2 else 0
        
        if io_type == 0:  # Digital output
            if 0 <= port < len(self.digital_outputs):
                self.digital_outputs[port] = bool(value)
                logger.info(f"Digital output {port} set to {value}")
        
        return self.create_success_response(self.IOCTRL, [True])

    def handle_tr_move(self, params):
        """TR move command"""
        direction = params[0]  # Move direction
        distance = params[1] if len(params) > 1 else 10.0
        speed = params[2] if len(params) > 2 else 50.0
        
        self.motion_id += 1
        self.simulate_motion(1.5)
        logger.info(f"TR move: direction={direction}, distance={distance}")
        return self.create_success_response(self.TRMOVE, [True, self.motion_id])

    def handle_ptp_plan(self, params):
        """PTP motion planning"""
        positions = params[0] if params else []
        self.motion_id += 1
        logger.info(f"PTP plan with {len(positions)} points")
        return self.create_success_response(self.PTPPLAN, [True, self.motion_id])

    def handle_cp_plan(self, params):
        """CP motion planning"""
        positions = params[0] if params else []
        self.motion_id += 1
        logger.info(f"CP plan with {len(positions)} points")
        return self.create_success_response(self.CPPLAN, [True, self.motion_id])

    def handle_suspend_motion(self, params=None):
        """Suspend motion"""
        logger.info("Motion suspended")
        return self.create_success_response(self.SUSPENDM, [True])

    def handle_resume_motion(self, params=None):
        """Resume motion"""
        logger.info("Motion resumed")
        return self.create_success_response(self.RESUMEM, [True])

    def handle_release_brake(self, params):
        """Release brake"""
        axis_bitfield = params[0] if params else 0x3F
        logger.info(f"Brake released, axes: 0x{axis_bitfield:02X}")
        return self.create_success_response(self.RELBRK, [True])

    def handle_clamp_brake(self, params):
        """Clamp brake"""
        axis_bitfield = params[0] if params else 0x3F
        logger.info(f"Brake clamped, axes: 0x{axis_bitfield:02X}")
        return self.create_success_response(self.CLPBRK, [True])

    def handle_arc_move(self, params):
        """Arc move command"""
        via_point = params[0]
        end_point = params[1]
        speed = params[2] if len(params) > 2 else 50.0
        
        self.motion_id += 1
        self.simulate_motion(4.0)  # Arc moves take longer
        logger.info(f"Arc move via {via_point} to {end_point}")
        return self.create_success_response(self.ARCMOVE, [True, self.motion_id])

    def handle_circular_move(self, params):
        """Circular move command"""
        center = params[0]
        end_point = params[1]
        speed = params[2] if len(params) > 2 else 50.0
        
        self.motion_id += 1
        self.simulate_motion(5.0)  # Circle moves take longer
        logger.info(f"Circular move around {center} to {end_point}")
        return self.create_success_response(self.CIRMOVE, [True, self.motion_id])

    def handle_multi_mark(self, params):
        """Multi-mark command"""
        mark_type = params[0] if params else 0
        
        # Return multiple position data
        multi_data = []
        for i in range(3):  # Return 3 sample positions
            multi_data.extend([
                self.current_cartesian_position[0] / 1000.0 + i * 0.1,
                self.current_cartesian_position[1] / 1000.0 + i * 0.1,
                self.current_cartesian_position[2] / 1000.0,
                math.radians(self.current_cartesian_position[3]),
                math.radians(self.current_cartesian_position[4]),
                math.radians(self.current_cartesian_position[5]),
                self.current_posture,
                self.current_rb_coord
            ])
        
        return self.create_success_response(self.MMARK, [True] + multi_data)

    # ==================== UTILITY METHODS ====================
    
    def simulate_motion(self, duration):
        """Simulate robot motion for specified duration"""
        self.is_moving = True
        
        def motion_complete():
            time.sleep(duration)
            self.is_moving = False
            logger.info(f"Motion {self.motion_id} completed after {duration}s")
        
        motion_thread = threading.Thread(target=motion_complete)
        motion_thread.daemon = True
        motion_thread.start()

    def create_success_response(self, cmd, results):
        """Create success response"""
        response = {'cmd': cmd, 'results': results}
        return json.dumps(response)

    def create_error_response(self, cmd, error):
        """Create error response"""
        response = {'cmd': cmd, 'results': [False, error]}
        return json.dumps(response)

    def stop(self):
        """Stop robot server"""
        self.running = False
        if self.sock:
            try:
                self.sock.close()
            except:
                pass
        logger.info("Robot Simulation Server stopped")


class MockSSHServerInterface(ServerInterface):
    """SSH Server Interface for paramiko"""
    
    def __init__(self):
        self.event = threading.Event()

    def check_channel_request(self, kind, chanid):
        if kind == 'session':
            return paramiko.OPEN_SUCCEEDED
        return paramiko.OPEN_FAILED_ADMINISTRATIVELY_PROHIBITED

    def check_auth_password(self, username, password):
        if username == 'i611usr' and password == 'i611':
            logger.info(f"SSH: Authentication successful for {username}")
            return paramiko.AUTH_SUCCESSFUL
        logger.warning(f"SSH: Authentication failed for {username}")
        return paramiko.AUTH_FAILED

    def check_auth_publickey(self, username, key):
        return paramiko.AUTH_FAILED

    def get_allowed_auths(self, username):
        return 'password'

    def check_channel_shell_request(self, channel):
        self.event.set()
        return True

    def check_channel_pty_request(self, channel, term, width, height, pixelwidth, pixelheight, modes):
        return True

    def check_channel_exec_request(self, channel, command):
        """Handle SSH command execution"""
        try:
            cmd_str = command.decode('utf-8').strip()
            logger.info(f"SSH: Executing command: {cmd_str}")
            
            if cmd_str == 'ls' or cmd_str.startswith('ls '):
                # Simulate comprehensive file listing
                files = [
                    'robot_initialization.py',
                    'movement_control.py',
                    'position_teaching.py',
                    'gripper_operations.py',
                    'safety_procedures.py',
                    'calibration_routine.py',
                    'maintenance_check.py',
                    'test_sequence.py',
                    'demo_program.py',
                    'user_interface.py',
                    'data_logging.py',
                    'error_handling.py',
                    'communication_test.py',
                    'sensor_monitoring.py',
                    'trajectory_planning.py',
                    'pick_and_place.py',
                    'assembly_routine.py',
                    'quality_check.py',
                    'vision_processing.py',
                    'force_control.py',
                    'config.txt',
                    'readme.md',
                    'backup.pyc',  # This should be filtered out
                    'temp.pyc',    # This should be filtered out
                    '__pycache__', # This should be ignored
                ]
                
                # Filter for Python files if 'ls *.py' was requested
                if '*.py' in cmd_str:
                    files = [f for f in files if f.endswith('.py')]
                
                file_list = '\n'.join(files) + '\n'
                channel.send(file_list.encode('utf-8'))
                channel.send_exit_status(0)
            
            elif cmd_str.startswith('cat ') or cmd_str.startswith('head '):
                # Simulate file content
                filename = cmd_str.split(' ', 1)[1] if ' ' in cmd_str else 'unknown'
                content = f"""#!/usr/bin/env python3
# Simulated content for {filename}
# This is a mock file for teaching pendant simulation

import sys
import time

def main():
    print("This is a simulated robot program: {filename}")
    print("Robot operations would be implemented here")
    
if __name__ == '__main__':
    main()
"""
                channel.send(content.encode('utf-8'))
                channel.send_exit_status(0)
            
            elif cmd_str == 'pwd':
                channel.send('/home/i611usr\n'.encode('utf-8'))
                channel.send_exit_status(0)
            
            elif cmd_str == 'whoami':
                channel.send('i611usr\n'.encode('utf-8'))
                channel.send_exit_status(0)
            
            elif cmd_str.startswith('echo '):
                echo_text = cmd_str[5:] + '\n'
                channel.send(echo_text.encode('utf-8'))
                channel.send_exit_status(0)
            
            else:
                # Unknown command
                error_msg = f"bash: {cmd_str}: command not found\n"
                channel.send_stderr(error_msg.encode('utf-8'))
                channel.send_exit_status(127)
            
            channel.close()
            return True
            
        except Exception as e:
            logger.error(f"SSH command execution error: {e}")
            try:
                channel.send_stderr(f"Error: {str(e)}\n".encode('utf-8'))
                channel.send_exit_status(1)
                channel.close()
            except:
                pass
            return False


class MockSSHServer:
    """Complete SSH Server simulation"""
    
    def __init__(self, host='192.168.0.23', port=22):
        self.host = host
        self.port = port
        self.sock = None
        self.running = False
        self.host_key = None
        self.client_count = 0
        self.setup_host_key()

    def setup_host_key(self):
        """Generate host key for SSH server"""
        try:
            # Generate RSA host key
            self.host_key = paramiko.RSAKey.generate(2048)
            logger.info("Generated RSA host key for SSH server")
        except Exception as e:
            logger.error(f"Error generating SSH host key: {e}")
            # Fallback to smaller key
            try:
                self.host_key = paramiko.RSAKey.generate(1024)
                logger.info("Generated fallback RSA host key")
            except Exception as e2:
                logger.error(f"Critical: Cannot generate SSH host key: {e2}")
                raise

    def start(self):
        """Start SSH server"""
        try:
            self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            self.sock.bind((self.host, self.port))
            self.sock.listen(10)
            self.running = True
            
            logger.info(f"🔐 Mock SSH Server started on {self.host}:{self.port}")
            logger.info("SSH Credentials: username='i611usr', password='i611'")
            
            while self.running:
                try:
                    client_sock, client_addr = self.sock.accept()
                    self.client_count += 1
                    logger.info(f"SSH client #{self.client_count} connected from {client_addr}")
                    
                    # Handle each SSH connection in separate thread
                    ssh_thread = threading.Thread(
                        target=self.handle_ssh_connection,
                        args=(client_sock, client_addr, self.client_count)
                    )
                    ssh_thread.daemon = True
                    ssh_thread.start()
                    
                except Exception as e:
                    if self.running:
                        logger.error(f"SSH accept error: {e}")
                        
        except Exception as e:
            logger.error(f"SSH server startup error: {e}")

    def handle_ssh_connection(self, client_sock, client_addr, client_id):
        """Handle individual SSH connection"""
        transport = None
        try:
            # Create SSH transport
            transport = Transport(client_sock)
            transport.add_server_key(self.host_key)
            transport.set_subsystem_handler('sftp', SFTPServer)
            
            # Create server interface
            server_interface = MockSSHServerInterface()
            
            # Start SSH server
            transport.start_server(server=server_interface)
            
            # Accept channel
            channel = transport.accept(30)  # 30 second timeout
            if channel is None:
                logger.warning(f"SSH client #{client_id}: No channel established")
                return
            
            logger.info(f"SSH client #{client_id}: Channel established")
            
            # Keep connection alive and wait for commands
            try:
                while transport.is_active() and not transport.is_closing():
                    time.sleep(0.1)
                    if not self.running:
                        break
            except Exception as e:
                logger.debug(f"SSH client #{client_id} connection loop error: {e}")
                
        except Exception as e:
            logger.error(f"SSH client #{client_id} connection error: {e}")
        finally:
            try:
                if transport:
                    transport.close()
            except:
                pass
            try:
                client_sock.close()
            except:
                pass
            logger.info(f"SSH client #{client_id} ({client_addr}) disconnected")

    def stop(self):
        """Stop SSH server"""
        self.running = False
        if self.sock:
            try:
                self.sock.close()
            except:
                pass
        logger.info("Mock SSH Server stopped")


class SimulationServerManager:
    """Manager for all simulation servers"""
    
    def __init__(self):
        self.robot_server = None
        self.ssh_server = None
        self.running = False

    def start_all(self):
        """Start all simulation servers"""
        try:
            logger.info("=" * 60)
            logger.info("🚀 STARTING ROBOT SIMULATION ENVIRONMENT")
            logger.info("=" * 60)
            
            # Start Robot Simulation Server
            self.robot_server = RobotSimulationServer('localhost', 12345)
            robot_thread = threading.Thread(target=self.robot_server.start)
            robot_thread.daemon = True
            robot_thread.start()
            
            # Small delay to ensure robot server starts
            time.sleep(0.5)
            
            # Start SSH Server
            self.ssh_server = MockSSHServer('192.168.0.23', 22)
            ssh_thread = threading.Thread(target=self.ssh_server.start)
            ssh_thread.daemon = True
            ssh_thread.start()
            
            # Small delay to ensure SSH server starts
            time.sleep(0.5)
            
            self.running = True
            
            logger.info("=" * 60)
            logger.info("✅ ALL SIMULATION SERVERS STARTED SUCCESSFULLY")
            logger.info("=" * 60)
            logger.info(f"🤖 Robot Server: localhost:12345")
            logger.info(f"🔐 SSH Server: 192.168.0.23:22")
            logger.info(f"👤 SSH Login: username='i611usr', password='i611'")
            logger.info("=" * 60)
            logger.info("📝 Available robot commands:")
            logger.info("   - File management: GET_FILE_LIST, EXECUTE_FILE, etc.")
            logger.info("   - Jog control: JOG_*_POSITIVE/NEGATIVE, JOG_STOP")
            logger.info("   - Robot control: SERVO, MOVE, MARK, etc.")
            logger.info("=" * 60)
            logger.info("📝 Available SSH commands:")
            logger.info("   - ls          : List files")
            logger.info("   - ls *.py     : List Python files only")
            logger.info("   - cat <file>  : Show file content")
            logger.info("   - pwd         : Show current directory")
            logger.info("   - whoami      : Show current user")
            logger.info("=" * 60)
            logger.info("Press Ctrl+C to stop all servers")
            logger.info("=" * 60)
            
            return True
            
        except Exception as e:
            logger.error(f"Failed to start simulation servers: {e}")
            return False

    def stop_all(self):
        """Stop all simulation servers"""
        logger.info("=" * 60)
        logger.info("🛑 STOPPING ALL SIMULATION SERVERS...")
        logger.info("=" * 60)
        
        self.running = False
        
        if self.robot_server:
            self.robot_server.stop()
            
        if self.ssh_server:
            self.ssh_server.stop()
            
        logger.info("✅ All simulation servers stopped successfully")
        logger.info("=" * 60)

    def run_forever(self):
        """Run servers until interrupted"""
        try:
            while self.running:
                time.sleep(1)
        except KeyboardInterrupt:
            logger.info("\n🔔 Received interrupt signal")
        finally:
            self.stop_all()

def main():
    """Main function to run simulation environment"""
    print("\n" + "=" * 60)
    print("🤖 ROBOT TEACHING PENDANT SIMULATION ENVIRONMENT")
    print("=" * 60)
    print("🔧 Initializing simulation servers...")
    print("=" * 60)
    
    manager = SimulationServerManager()
    
    try:
        if manager.start_all():
            print("🎯 Simulation environment ready!")
            print("🔗 Waiting for connections...")
            print("=" * 60)
            
            # Display connection information
            print("📋 CONNECTION DETAILS:")
            print("   🤖 Robot TCP Server:")
            print("      - Host: localhost")
            print("      - Port: 12345")
            print("      - Protocol: JSON over TCP")
            print("")
            print("   🔐 SSH Server:")
            print("      - Host: 192.168.0.23")
            print("      - Port: 22")
            print("      - Username: i611usr")
            print("      - Password: i611")
            print("")
            print("📊 SUPPORTED FEATURES:")
            print("   ✅ File Management (FilesListView)")
            print("   ✅ Jog Control (MotionControlView)")
            print("   ✅ Robot Commands (All rblib3 functions)")
            print("   ✅ SSH Terminal Access")
            print("   ✅ Position Monitoring")
            print("   ✅ I/O Control")
            print("=" * 60)
            
            try:
                # Run the main loop
                manager.run_forever()
                
            except KeyboardInterrupt:
                print("\n" + "=" * 60)
                print("🔔 SHUTDOWN SIGNAL RECEIVED")
                print("=" * 60)
                
            except Exception as e:
                print(f"\n❌ Simulation environment error: {e}")
                logger.error(f"Main loop error: {e}")
                logger.error(traceback.format_exc())
                
        else:
            print("❌ Failed to start simulation environment")
            print("🔍 Check the logs above for detailed error information")
            print("💡 Common issues:")
            print("   - Port already in use (try closing other applications)")
            print("   - Permission denied (try running as administrator)")
            print("   - Missing dependencies (install paramiko: pip install paramiko)")
            return 1
            
    except Exception as e:
        print(f"\n💥 CRITICAL ERROR: {e}")
        logger.critical(f"Critical startup error: {e}")
        logger.critical(traceback.format_exc())
        return 1
        
    finally:
        # Ensure cleanup
        try:
            if manager:
                manager.stop_all()
        except:
            pass
            
        print("\n" + "=" * 60)
        print("👋 ROBOT SIMULATION ENVIRONMENT STOPPED")
        print("   Thank you for using the simulation!")
        print("=" * 60)
        
    return 0


if __name__ == '__main__':
    try:
        exit_code = main()
        sys.exit(exit_code)
    except Exception as e:
        print(f"\n💀 FATAL ERROR: {e}")
        logging.critical(f"Fatal error in main: {e}")
        logging.critical(traceback.format_exc())
        sys.exit(1)