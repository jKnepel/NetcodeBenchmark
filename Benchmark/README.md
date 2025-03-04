# Netcode Benchmark

## Python Benchmark Controller

This is the environment for controlling the netcode benchmark. To run the benchmarks the Windows OS and python 3.6+ are required. For the network traffic benchmark Wireshark needs to be installed on the system.

Before the benchmark can be run, the dependencies in requirements.txt must be installed. This can optionally be done by using a python environment eg. venv.

To do this optionally activate the python environment by using:

```
python -m venv venv
venv\Scripts\activate
```

Install the dependencies using:

```
pip install -r requirements.txt
```

Before the benchmarks can be started, compatible benchmark projects need to be built. For this the script in ../Projects/ProteusNet/Assets/Scripts/BenchmarkController.cs can be used. The visual benchmark requires a baseline benchmark for comparison. For this, the adjusted local controller in ../Projects/Local/Assets/Scripts/BenchmarkController.cs can be used.

Once compatible applications are built, either the benchmark_visual or benchmark_traffic can be run. For this, they need to be configured with the correct PROCESS_PATH and benchmark variables. The script can then be run in a terminal (e.g. by opening the directory with VSCode and Python extension).
