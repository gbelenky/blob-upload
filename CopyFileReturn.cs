using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace gbelenky.blobupload
{
    public class CopyFileReturn
    {
        public string LogMessage { get; set; }
        public long Duration { get; set; }
        public long FileSize  { get; set; }      
    }
}

