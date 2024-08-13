# Blob Upload

Durable Functions demo on reliably uploading a big number of large files to Azure Blob Storage.

## Description

This project is a demonstration of using Azure Durable Functions to reliably upload a large number of large files to Azure Blob Storage. The solution ensures that the file upload process is resilient and can handle interruptions gracefully.

## Features

- **Durable Functions**: Utilizes Azure Durable Functions to manage and orchestrate the upload process.
- **Resilience**: Ensures that uploads are reliable and can recover from interruptions.
- **Scalability**: Designed to handle a large number of large files efficiently.

## Language

- **C#**: The entire project is written in C#.

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azure Storage Account](https://azure.microsoft.com/en-us/services/storage/)

### Installation

1. Clone the repository:
   ```sh
   git clone https://github.com/gbelenky/blob-upload.git
   cd blob-upload
   ```
### Steps to Set Up
Create local.settings.json:

Here is an example of what a local.settings.json file might look like for this project:

{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "DEST_BLOB_STORAGE": "your-destination-blob-storage-connection-string"
  }
}

Add the above configuration to your local.settings.json file in the root of the project.
Update Connection Strings:

Replace YourAzureWebJobsStorageConnectionString and YourDestinationBlobStorageConnectionString with your actual Azure Storage connection strings.
Configure Durable Functions:

Ensure that your Azure Durable Functions are properly set up and configured to use the settings from local.settings.json.
By following these steps, you should be able to configure and run the project effectively.

### Running the Function Locally
To run the Azure Function locally and start the file upload process using a curl request, follow these steps:

Ensure Prerequisites:

Make sure you have Azure Functions Core Tools installed.
Ensure the project dependencies are installed (dotnet restore).
Start the Azure Function Locally:

Open a terminal in the project root directory.
Run the following command to start the Azure Function locally:
func start
Send the Curl Request:

Open another terminal window.
Run the following curl command to initiate the file upload process:
curl -X POST \
  http://localhost:7071/api/HttpStart \
  -H "Content-Type: application/json" \
  -d '{
        "PathToArchive": "/path/to/your/local/directory",
        "InstanceId": "your-instance-id"
      }'
Replace the placeholders:
/path/to/your/local/directory: Path to the local directory containing the files.
your-instance-id: A unique instance ID for tracking the upload process.
Monitor the Logs:

Check the terminal where the Azure Function is running to monitor the logs and verify that the files are being uploaded correctly.
By following these steps, you can run the Azure Function locally and initiate file uploads using a curl request.
