import subprocess
import socket
import struct
import time
import cv2
import numpy as np
import threading
import win32gui
import win32ui
import win32process
from ctypes import windll
from PIL import Image
from abc import ABC, abstractmethod
from enum import Enum

class BenchmarkCommands(Enum):
    StartServer = b'\x01'
    StartClient = b'\x02'
    StopServer = b'\x03'
    StopClient = b'\x04'
    DirectionalInput = b'\x05'
    SetObjectNumber = b'\x06'

class BenchmarkHarnessBase(ABC):
    def __init__(self, process_path,  startup='', host='127.0.0.1'):
        self.process_path = process_path
        self.startup = startup
        self.host = host

    @abstractmethod
    def start(self, num_clients, num_objects):
        pass

    @abstractmethod
    def stop(self):
        pass

    @abstractmethod
    def directional_input(self, client_idx: int, right: float, up: float, duration: float):
        pass

class BenchmarkHarnessNetwork(BenchmarkHarnessBase):
    def __init__(self, process_path,  num_clients, startup='', host='127.0.0.1'):
        super().__init__(process_path, startup, host)
        self.server = BenchmarkConnection(self.process_path, self.startup, self.host)
        self.clients = [BenchmarkConnection(self.process_path, self.startup, self.host) for _ in range(num_clients)]

    def __del__(self):
        for client in self.clients:
            del client
        del self.server

    def start(self, num_objects):
        self.server.start_server(num_objects)
        time.sleep(1)
        for client in self.clients:
            client.start_client()
            time.sleep(1)

    def stop(self):
        for client in self.clients:
            client.stop_client()
        self.server.stop_server()

    def directional_input(self, client_idx: int, right: float, up: float, duration: float):
        self.clients[client_idx].directional_input(right, up, duration)

class BenchmarkHarnessLocal(BenchmarkHarnessBase):
    def __init__(self, process_path,  num_clients, startup='', host='127.0.0.1'):
        super().__init__(process_path, startup, host)
        self.process = BenchmarkConnection(self.process_path, self.startup, self.host)
        self.num_clients = num_clients

    def __del__(self):
        del self.process

    def start(self, num_objects):
        self.process.start_server(num_objects)
        for _ in range(self.num_clients):
            self.process.start_client()
        time.sleep(1)

    def stop(self):
        for _ in range(self.num_clients):
            self.process.stop_client()
        self.process.stop_server()

    def directional_input(self, client_idx: int, right: float, up: float, duration: float):
        self.process.send_data(BenchmarkCommands.DirectionalInput.value + struct.pack('i', client_idx) + struct.pack('f', right) + struct.pack('f', up))
        time.sleep(duration)
        self.process.send_data(BenchmarkCommands.DirectionalInput.value + struct.pack('i', client_idx) + struct.pack('f', 0) + struct.pack('f', 0))

class BenchmarkConnection:
    def __init__(self, process_path, startup='', host='127.0.0.1', fps=30):
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.bind((host, 0))
        self.socket.listen(1)
        self.port = self.socket.getsockname()[1]

        self.process = subprocess.Popen([process_path, str(self.port), startup])
        self.connection, addr = self.socket.accept()
        print(f"Benchmark connection established on {addr}")

        # Setup for window capturing
        self.capture_active = False
        self.capture_video_writer = None
        self.capture_thread = None
        self.fps = fps
        self.hwnd = self.find_window_by_pid(self.process.pid)
        if self.hwnd is None:
            print(f"Window for process with PID {self.process.pid} not found.")
        else:
            rect = win32gui.GetWindowRect(self.hwnd)
            left, top, right, bottom = rect
            self.width = right - left
            self.height = bottom - top

            windll.user32.SetProcessDPIAware()
            self.hwnd_dc = win32gui.GetWindowDC(self.hwnd)
            self.mfc_dc = win32ui.CreateDCFromHandle(self.hwnd_dc)
            self.save_dc = self.mfc_dc.CreateCompatibleDC()

            self.bitmap = win32ui.CreateBitmap()
            self.bitmap.CreateCompatibleBitmap(self.mfc_dc, self.width, self.height)

    def __del__(self):
        if self.process and self.process.poll() is None:
            self.process.terminate()
            self.process.wait()
        if self.connection:
            self.connection.close()
            self.connection = None
        if self.socket:
            self.socket.close()
            self.socket = None

    def start_server(self, num_objects: int):
        self.send_data(BenchmarkCommands.SetObjectNumber.value + struct.pack('I', num_objects))
        self.send_data(BenchmarkCommands.StartServer.value)
    def stop_server(self):
        self.send_data(BenchmarkCommands.StopServer.value)
    def start_client(self):
        self.send_data(BenchmarkCommands.StartClient.value)
    def stop_client(self):
        self.send_data(BenchmarkCommands.StopClient.value)
    def directional_input(self, right: float, up: float, duration: float):
        self.send_data(BenchmarkCommands.DirectionalInput.value + struct.pack('f', right) + struct.pack('f', up))
        time.sleep(duration)
        self.send_data(BenchmarkCommands.DirectionalInput.value + struct.pack('f', 0) + struct.pack('f', 0))

    def send_data(self, data: bytes):
        if self.connection:
            try:
                self.connection.sendall(data)
            except Exception as e:
                print(f"Error sending data: {e}")
        else:
            print("No client connection available to send data.")

    def start_capture(self, output_video_path):
        def capture():
            self.capture_active = True
            capture_video_writer = cv2.VideoWriter(output_video_path, cv2.VideoWriter_fourcc(*'XVID'), self.fps, (self.width, self.height))

            print("Capture started.")
            while self.capture_active:
                frame = self.capture_window()
                frame = cv2.cvtColor(frame, cv2.COLOR_RGB2BGR)
                capture_video_writer.write(frame)

            print("Capture stopped.")
            capture_video_writer.release()
            capture_video_writer = None

        # Create and start the capture thread
        self.capture_thread = threading.Thread(target=capture)
        self.capture_thread.start()

    def stop_capture(self):
        if self.capture_thread:
            self.capture_active = False
            self.capture_thread.join()

    def find_window_by_pid(self, pid):
        # Adapted from https://stackoverflow.com/questions/70618975/python-get-windowtitle-from-process-id-or-process-name
        def enum_window_callback(hwnd, lParam):
            _, found_pid = win32process.GetWindowThreadProcessId(hwnd)
            if found_pid == pid:
                lParam.append(hwnd)
            return True
        
        hwnd_list = []
        win32gui.EnumWindows(enum_window_callback, hwnd_list)
        return hwnd_list[0] if hwnd_list else None

    def capture_window(self):
        # Adapted from https://github.com/BoboTiG/python-mss/issues/180
        self.save_dc.SelectObject(self.bitmap)

        result = windll.user32.PrintWindow(self.hwnd, self.save_dc.GetSafeHdc(), 3)

        bmpinfo = self.bitmap.GetInfo()
        bmpstr = self.bitmap.GetBitmapBits(True)

        im = Image.frombuffer("RGB", (bmpinfo["bmWidth"], bmpinfo["bmHeight"]), bmpstr, "raw", "BGRX", 0, 1)

        if result != 1:
            win32gui.DeleteObject(self.bitmap.GetHandle())
            self.save_dc.DeleteDC()
            self.mfc_dc.DeleteDC()
            win32gui.ReleaseDC(self.hwnd, self.hwnd_dc)
            raise RuntimeError(f"Unable to acquire screenshot! Result: {result}")

        open_cv_image = np.array(im)[:, :, ::-1].copy()
        return open_cv_image
