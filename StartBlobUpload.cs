using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;
using System;

namespace gbelenky.blobupload
{
    public static class StartBlobUpload
    {
        [FunctionName("StartBlobUpload")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            ArchiveTask archiveTask = context.GetInput<ArchiveTask>();
            

            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            /*
            outputs.Add(await context.CallActivityAsync<string>("StartBlobUpload_Hello", "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>("StartBlobUpload_Hello", "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>("StartBlobUpload_Hello", "London"));
            */
             outputs.Add(await context.CallActivityAsync<string>("GetFileList", archiveTask.PathToArchive));

            return outputs;
        }

        [FunctionName("GetFileList")]
        public static string GetFileList([ActivityTrigger] string rootString, ILogger log)
        {           
            DirectoryInfo root = new DirectoryInfo(rootString);
            List<FileInfo> fullFilesList = new List<FileInfo>();
            WalkDirectoryTree(root, ref fullFilesList, log);

            log.LogInformation($"Saying hello to .");
            return $"Hello!";
        }


        static void WalkDirectoryTree(DirectoryInfo root, ref List<FileInfo> fullFilesList, ILogger log)
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
                    fullFilesList.Add(fi);
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo, ref fullFilesList, log);
                }
            }
        }



        [FunctionName("InitBlobUpload")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string archiveTaskJson = await req.Content.ReadAsStringAsync();

            ArchiveTask archiveTask = JsonSerializer.Deserialize<ArchiveTask>(archiveTaskJson);


            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("StartBlobUpload", archiveTask);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}