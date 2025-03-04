import sys
import os
import csv
import cv2
import numpy as np
from benchmark_harness import BenchmarkHarnessNetwork, BenchmarkHarnessLocal, BenchmarkHarnessBase

def main():
    WARMUPS = 0
    RUNS = 2 # must be atleast 2 for mean computation
    NUM_CLIENTS = 3
    START_OBJECTS = 49
    END_OBJECTS = 49

    # Change working directory to current file
    script_directory = os.path.dirname(os.path.abspath(__file__)) 
    os.chdir(script_directory)

    with open(r"benchmark_visuals_results.csv", mode='a', newline='') as csv_file:
        csv_writer = csv.writer(csv_file)
        '''
        csv_writer.writerow([
            "Number of Objects", 
            "Runs",
            "Mean Server Diff", 
            "StdDev Server Diff", 
            "Error Server Diff", 
            "Mean Clients Diff", 
            "StdDev Clients Diff", 
            "Error Clients Diff",
        ])
        '''

        local = BenchmarkHarnessLocal(r"../Projects/Local/Builds/Benchmark.exe", NUM_CLIENTS)
        network = BenchmarkHarnessNetwork(r"../Projects/ProteusNet/Builds/Benchmark.exe", NUM_CLIENTS)

        LOCAL_VIDEO_PATH = "local.avi"
        SERVER_VIDEO_PATH = "network_server.avi"
        CLIENTS_VIDEO_PATH = [f"network_client_{i}.avi" for i in range(NUM_CLIENTS)]

        for num_objects in range(START_OBJECTS, END_OBJECTS + 1, 1):

            # Run the local baseline benchmark
            local.start(num_objects)
            local.process.start_capture(LOCAL_VIDEO_PATH)
            benchmark(local)
            local.process.stop_capture()
            local.stop()
            
            server_diffs = []
            client_diffs = []

            for i in range(1, WARMUPS + RUNS + 1, 1): 

                # Run the network benchmark
                network.start(num_objects)
                network.server.start_capture(SERVER_VIDEO_PATH)
                for j, client in enumerate(network.clients):
                    client.start_capture(CLIENTS_VIDEO_PATH[j])
                benchmark(network)
                for client in network.clients:
                    client.stop_capture()
                network.server.stop_capture()
                network.stop()

                # Only include results if warmups are done
                if i > WARMUPS:
                    # Compute and save differences between videos
                    server_diffs.append(compute_difference(SERVER_VIDEO_PATH, LOCAL_VIDEO_PATH))
                    client_diffs.append(compute_difference(compute_average_videos(CLIENTS_VIDEO_PATH), LOCAL_VIDEO_PATH))

            # Compute averages
            avg_server_diff = np.mean(server_diffs)
            std_server_diff = np.std(server_diffs, ddof=1)
            err_server_diff = std_server_diff / np.sqrt(RUNS)
            avg_client_diff = np.mean(client_diffs)
            std_client_diff = np.std(client_diffs, ddof=1)
            err_client_diff = std_client_diff / np.sqrt(RUNS)

            csv_writer.writerow([
                num_objects,
                RUNS,
                avg_server_diff,
                std_server_diff,
                err_server_diff,
                avg_client_diff,
                std_client_diff,
                err_client_diff
            ])

        del local
        del network

    print(f"Completed benchmark.")

def benchmark(harness: BenchmarkHarnessBase):
    harness.directional_input(0, 0.0, 1.0, 2)
    harness.directional_input(0, 0.5, 0.0, 2)
    harness.directional_input(0, -0.5, 0.0, 4)

def compute_difference(video1_path: str, video2_path: str):
    capture1 = cv2.VideoCapture(video1_path)
    capture2 = cv2.VideoCapture(video2_path)

    if not capture1.isOpened() or not capture2.isOpened():
        print(f"Error opening videos: {video1_path}, {video2_path}")
        return None

    frame_diffs = []

    while capture1.isOpened() and capture2.isOpened():
        ret1, frame1 = capture1.read()
        ret2, frame2 = capture2.read()

        if not ret1 or not ret2:
            break  # End of video

        # Convert to grayscale
        gray1 = cv2.cvtColor(frame1, cv2.COLOR_BGR2GRAY)
        gray2 = cv2.cvtColor(frame2, cv2.COLOR_BGR2GRAY)

        # Compute absolute difference
        diff = cv2.absdiff(gray1, gray2)
        frame_diffs.append(np.mean(diff))

    capture1.release()
    capture2.release()

    if frame_diffs:
        return np.mean(frame_diffs)
    return None

def compute_average_videos(videos: list[str]):
    captures = [cv2.VideoCapture(video) for video in videos]

    if any(not capture.isOpened() for capture in captures):
        print("Error opening one or more client videos.")
        return None

    avg_frames = []
    while all(capture.isOpened() for capture in captures):
        frames = [capture.read()[1] for capture in captures]
        
        if any(frame is None for frame in frames):
            break  # End of video

        # Convert to grayscale
        gray_frames = [cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY) for frame in frames]

        # Compute average frame
        avg_frame = np.mean(gray_frames, axis=0).astype(np.uint8)
        avg_frames.append(avg_frame)

    for capture in captures:
        capture.release()

    if avg_frames is not None:
        # Write the average video to disk
        avg_video_path = "avg_videos.avi"
        frame_height, frame_width = avg_frames[0].shape
        out = cv2.VideoWriter(avg_video_path, cv2.VideoWriter_fourcc(*'XVID'), 30, (frame_width, frame_height), False)

        for frame in avg_frames:
            out.write(frame)
        out.release()

        return avg_video_path
    else:
        print("Failed to compute average videos.")

if __name__ == "__main__":
    sys.exit(main())
