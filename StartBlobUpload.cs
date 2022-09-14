using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System;
using Azure.Storage.Blobs;
using System.Diagnostics;
using Azure.Storage.Blobs.Specialized;
using Newtonsoft.Json;
using System.Threading;
using System.Net;



namespace gbelenky.blobupload
{
    public static class StartBlobUpload
    {
        [FunctionName("StartBlobUpload")]
        public static async Task<List<CopyFileReturn>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var outputs = new List<CopyFileReturn>();
            ArchiveTask archiveTask = context.GetInput<ArchiveTask>();
            List<string> fullFilesList = await context.CallActivityAsync<List<string>>("GetFileList", archiveTask.PathToArchive);


            var tasks = new Task<CopyFileReturn>[fullFilesList.Count];
            int i = 0;
            foreach (string fileName in fullFilesList)
            {
                tasks[i] = context.CallActivityAsync<CopyFileReturn>("CopyFile", fileName);
                i++;
            }

            await Task.WhenAll(tasks);
            
            long totalBytes = 0;
            long totalTime = 0;

            foreach (Task<CopyFileReturn> t in tasks)
            {
                outputs.Add(t.Result);
                totalBytes += t.Result.FileSize;
                totalTime += t.Result.Duration; 
            }
            
            log.LogInformation($"Copied {i} files in {totalTime/1000} seconds. Total size: {totalBytes} bytes");
            return outputs;
        }

        [FunctionName("GetFileList")]
        public static List<string> GetFileList([ActivityTrigger] string rootString, ILogger log)
        {
            DirectoryInfo root = new DirectoryInfo(rootString);
            List<string> fullFilesList = new List<string>();
            WalkDirectoryTree(root, ref fullFilesList, log);

            log.LogInformation($"File list prepared for {rootString}");
            return fullFilesList;
        }

        // from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/file-system/how-to-iterate-through-a-directory-tree
        static void WalkDirectoryTree(DirectoryInfo root, ref List<string> fullFilesList, ILogger log)
        {
            FileInfo[] files = null;
            DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try
            {
                files = root.GetFiles("*.*");
            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException e)
            {
                // This code just writes out the message and continues to recurse.
                // You may decide to do something different here. For example, you
                // can try to elevate your privileges and access the file again.
                log.LogError(e.Message);
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                log.LogError(e.Message);
            }

            if (files != null)
            {
                foreach (System.IO.FileInfo fi in files)
                {
                    // In this example, we only access the existing FileInfo object. If we
                    // want to open, delete or modify the file, then
                    // a try-catch block is required here to handle the case
                    // where the file has been deleted since the call to TraverseTree().
                    log.LogTrace(fi.FullName);
                    fullFilesList.Add(fi.FullName);
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Recursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo, ref fullFilesList, log);
                }
            }
        }


        [FunctionName("CopyFile")]
        [StorageAccount("DEST_BLOB_STORAGE")]
        public static async Task<CopyFileReturn> CopyFile([ActivityTrigger] string fileName,
                [Blob("archfiles")] BlobContainerClient destContainer, ILogger log)
        {
            BlobClient blobClient = destContainer.GetBlobClient(fileName);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            log.LogInformation($"copy {fileName}...");
            await blobClient.UploadAsync(fileName, true);
            stopwatch.Stop();
            var properties = await blobClient.GetPropertiesAsync();
            string statusMessage = $"copied {fileName} of {properties.Value.ContentLength} bytes in {stopwatch.ElapsedMilliseconds} milliseconds";
            log.LogInformation(statusMessage);
            return new CopyFileReturn{
                FileSize = properties.Value.ContentLength,
                Duration = stopwatch.ElapsedMilliseconds,
                LogMessage = statusMessage
            };
        }

        [FunctionName("InitBlobUpload")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string archiveTaskJson = await req.Content.ReadAsStringAsync();

            ArchiveTask archiveTask = JsonConvert.DeserializeObject<ArchiveTask>(archiveTaskJson);

            // Function input comes from the request content.
            var instanceId = await starter.StartNewAsync("StartBlobUpload", archiveTask);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            DurableOrchestrationStatus status = await starter.GetStatusAsync(instanceId);
            while (status.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
            {
                await Task.Delay(200);
                status = await starter.GetStatusAsync(instanceId);
            }
            return starter.CreateCheckStatusResponse(req, status.Output.ToString());
        }

        [FunctionName("GetStatus")]
        public static async Task<HttpResponseMessage> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient client, ILogger log)
        {
            var noFilter = new OrchestrationStatusQueryCondition();
            OrchestrationStatusQueryResult result = await client.ListInstancesAsync(
                noFilter,
                CancellationToken.None);

            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(result))
            };

            return httpResponseMessage;
        }
    }
}