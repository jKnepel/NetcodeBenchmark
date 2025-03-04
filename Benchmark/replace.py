import csv
from collections import defaultdict

# Function to reshape the CSV data by 'Number of Objects' and processes
def reshape_csv(input_csv, output_csv):
    # Read the CSV file
    with open(input_csv, newline='', encoding='utf-8') as infile:
        reader = csv.DictReader(infile)

        # Create a dictionary to store data grouped by 'Number of Objects' and process name
        grouped_data = defaultdict(lambda: {
            'ProteusNet': None,
            'FishNet': None,
            'NGO': None,
            'Mirror': None
        })
        
        # Process each row and group by 'Number of Objects' and 'Process'
        for row in reader:
            num_objects = int(row['Number of Objects'])  # Convert 'Number of Objects' to int
            process = row['Process']
            mean_total_bytes = row['Mean Total Bytes']
            
            # Only store the 'Mean Total Bytes' for the specific process
            if process in grouped_data[num_objects]:
                grouped_data[num_objects][process] = mean_total_bytes

    # Prepare the output file to write the reshaped data
    with open(output_csv, mode='w', newline='', encoding='utf-8') as outfile:
        # Define the fieldnames for the output CSV file
        fieldnames = ['Number of Objects', 'ProteusNet', 'FishNet', 'NGO', 'Mirror']
        writer = csv.DictWriter(outfile, fieldnames=fieldnames)
        
        # Write the header row
        writer.writeheader()

        # Write rows sorted by 'Number of Objects' (numerically)
        for num_objects in sorted(grouped_data.keys()):
            row = {'Number of Objects': num_objects}
            # Fill in the Mean Total Bytes for each process
            for process in ['ProteusNet', 'FishNet', 'NGO', 'Mirror']:
                row[process] = grouped_data[num_objects].get(process, None)
            writer.writerow(row)

    print(f"Reshaped data saved to {output_csv}")

# Example usage:
input_csv = 'C:/Users/Julian/Desktop/traffic_benchmark_objects - 1.csv'  # Replace with your input CSV path
output_csv = 'C:/Users/Julian/Desktop/traffic_benchmark_objects - 2.csv'  # Output file path

reshape_csv(input_csv, output_csv)
