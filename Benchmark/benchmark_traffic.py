import time
import sys
import os
import csv
import pyshark
import threading
import asyncio
import numpy as np
import math
from benchmark_harness import BenchmarkHarnessNetwork, BenchmarkHarnessBase

def main():
    PROCESS_PATHS = [
        #r"../Projects/ProteusNet/Builds/Benchmark.exe",
        #r"../Projects/NGO/Builds/Benchmark.exe",
        r"../Projects/FishNet/Builds/Benchmark.exe",
        #r"../Projects/Mirror/Builds/Benchmark.exe"
    ]
    WARMUPS = 0
    RUNS = 15
    NUM_CLIENTS = 3
    START_OBJECTS = 73
    END_OBJECTS = 100
    CONFIDENCE_LEVEL = 0.99
    UDP_PORT = 24856  # Replace with the port number used by the frameworks
    INTERFACE = r"\Device\NPF_Loopback"  # Replace with your loopback interface (e.g., "lo" for Linux, "\Device\NPF_Loopback" for Windows)

    if (RUNS < 2):
        print("Runs must be larger than 1 to compute meaningful means and CI!")
        return

    # Change working directory to current file
    script_directory = os.path.dirname(os.path.abspath(__file__)) 
    os.chdir(script_directory)

    with open(r"benchmark_traffic_results.csv", mode='a', newline='') as csv_file:
        csv_writer = csv.writer(csv_file)
        """
        csv_writer.writerow([
            "Process",
            "Number of Objects", 
            "Runs",
            "Mean Bytes To Server", 
            "Mean Bytes From Server", 
            "Mean Total Bytes", 
            "StdDev Total Bytes", 
            "Error Total Bytes", 
            "CI Total Bytes",
            "Mean Packets To Server", 
            "Mean Packets From Server", 
            "Mean Total Packets", 
            "StdDev Total Packets", 
            "Error Total Packets", 
            "CI Total Packets"
        ])
        """

        for path in PROCESS_PATHS:
            harness = BenchmarkHarnessNetwork(path, NUM_CLIENTS)

            for num_objects in range(START_OBJECTS, END_OBJECTS + 1, 1):
                bytes_to_server = []
                bytes_from_server = []
                total_bytes = []
                packets_to_server = []
                packets_from_server = []
                total_packets = []

                for i in range(1, WARMUPS + RUNS + 1, 1):
                    cancel_event = threading.Event()
                    capture_results = {}
                    capture_thread = threading.Thread(target=capture_traffic, args=(cancel_event, capture_results, UDP_PORT, INTERFACE))
                    capture_thread.start()
                    time.sleep(1)
                    
                    harness.start(num_objects)
                    benchmark(harness)
                    harness.stop()

                    cancel_event.set()
                    capture_thread.join()

                    if i > WARMUPS:
                        bytes_to_server.append(capture_results.get("bytes_to_server", 0))
                        bytes_from_server.append(capture_results.get("bytes_from_server", 0))
                        total_bytes.append(capture_results.get("total_bytes", 0))
                        packets_to_server.append(capture_results.get("packets_to_server", 0))
                        packets_from_server.append(capture_results.get("packets_from_server", 0))
                        total_packets.append(capture_results.get("total_packets", 0))

                # Compute statistics
                avg_bytes_to_server = np.mean(bytes_to_server)
                avg_bytes_from_server = np.mean(bytes_from_server)
                avg_total_bytes = np.mean(total_bytes)
                std_total_bytes = np.std(total_bytes, ddof=1)
                err_total_bytes = std_total_bytes / math.sqrt(RUNS)
                ci_total_bytes = compute_confidence_interval(avg_total_bytes, std_total_bytes, RUNS, CONFIDENCE_LEVEL)

                avg_packets_to_server = np.mean(packets_to_server)
                avg_packets_from_server = np.mean(packets_from_server)
                avg_total_packets = np.mean(total_packets)
                std_total_packets = np.std(total_packets, ddof=1)
                err_total_packets = std_total_packets / math.sqrt(RUNS)
                ci_total_packets = compute_confidence_interval(avg_total_packets, std_total_packets, RUNS, CONFIDENCE_LEVEL)

                csv_writer.writerow([
                    path,
                    num_objects,
                    RUNS,
                    avg_bytes_to_server,
                    avg_bytes_from_server,
                    avg_total_bytes,
                    std_total_bytes,
                    err_total_bytes,
                    f"{str(ci_total_bytes[0])}-{str(ci_total_bytes[1])}",
                    avg_packets_to_server,
                    avg_packets_from_server,
                    avg_total_packets,
                    std_total_packets,
                    err_total_packets,
                    f"{str(ci_total_packets[0])}-{str(ci_total_packets[1])}",
                ])

            del harness
            print(f"Completed benchmark.")
        print(f"Completed all benchmarks.")

def benchmark(harness: BenchmarkHarnessBase):
    time.sleep(1)
    #harness.directional_input(0, 0.0,  1.0,  3)
    #harness.directional_input(0, 1.0, -1.0,  1)
    #harness.directional_input(1, 0.0,  1.0,  5)

def capture_traffic(cancel_event, results, port, interface):
    # Create an event loop in this thread
    loop = asyncio.new_event_loop()
    asyncio.set_event_loop(loop)

    capture = pyshark.LiveCapture(interface=interface, bpf_filter=f"udp port {port}")
    bytes_to_server = 0
    bytes_from_server = 0
    packets_to_server = 0
    packets_from_server = 0

    print("Traffic capture started...")
    try:
        for packet in capture.sniff_continuously():
            if cancel_event.is_set():
                break

            if hasattr(packet, 'udp') and hasattr(packet, 'length'):
                packet_length = int(packet.length)

                # Check if the captured port is the destination or source
                if int(packet.udp.dstport) == port:
                    # Traffic to the server
                    bytes_to_server += packet_length
                    packets_to_server += 1
                elif int(packet.udp.srcport) == port:
                    # Traffic from the server
                    bytes_from_server += packet_length
                    packets_from_server += 1

    except Exception as e:
        print(f"Error during traffic capture: {e}")
    finally:
        capture.close()

    # Store results
    results["bytes_to_server"] = bytes_to_server
    results["bytes_from_server"] = bytes_from_server
    results["total_bytes"] = bytes_to_server + bytes_from_server
    results["packets_to_server"] = packets_to_server
    results["packets_from_server"] = packets_from_server
    results["total_packets"] = packets_to_server + packets_from_server

    print("Traffic capture stopped.")

def compute_confidence_interval(mean, std_dev, n, confidence=0.99):
    # Approximate t-critical value for df = 25 - 1 from https://people.richland.edu/james/lecture/m170/tbl-t.html
    t_critical = {
        0.90: 1.711,  # 90% confidence
        0.95: 2.064,  # 95% confidence
        0.99: 2.797   # 99% confidence
    }.get(confidence)

    margin_of_error = t_critical * (std_dev / math.sqrt(n))
    return (mean - margin_of_error, mean + margin_of_error)

if __name__ == "__main__":
    sys.exit(main())
