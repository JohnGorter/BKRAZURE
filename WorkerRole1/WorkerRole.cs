using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class Registration : TableEntity
    {
        public String Name { get; set; }
        public String Lastname { get; set; }
        public String Photourl { get; set; }
    }

    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            CloudStorageAccount csa = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=johngorterstorage2;AccountKey=wU8dAPycZjQLYhdAywnq56eWwehF8ICT7M51gFIIO/3rG/78rX/dFTLvl2eS+OekXbosi4oSm8FKpcerLZi6Rg==;EndpointSuffix=core.windows.net");
            var queueclient = csa.CreateCloudQueueClient();
            var tableclient = csa.CreateCloudTableClient();
            var blobClient = csa.CreateCloudBlobClient();
            var queueref = queueclient.GetQueueReference("registrations");
            var tableref = tableclient.GetTableReference("registrations");
            var containerref = blobClient.GetContainerReference("registrations");

            queueref.CreateIfNotExists();
            tableref.CreateIfNotExists();
            containerref.CreateIfNotExists();

            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");

                var message = queueref.GetMessage();
                if (message != null)
                {
                    var m = message.AsString;
                    var firstname = m.Split(new String[] { "-" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    var lastname = m.Split(new String[] { "-" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    Trace.TraceInformation($"working on registration for {m}");
                    queueref.DeleteMessage(message);

                    // image resizen
                    var blobref = containerref.GetBlockBlobReference(m + ".jpeg");
                    var blobref_thumb = containerref.GetBlockBlobReference(m + "_th.jpeg");

                    MemoryStream stream = new MemoryStream();
                    MemoryStream stream2 = new MemoryStream();
                    blobref.DownloadToStream(stream);
                    stream.Position = 0;
                    //Image i = Image.FromStream(stream);
                    //Image thumb = i.GetThumbnailImage(50, 50, null, IntPtr.Zero);
                    //thumb.Save(stream2, ImageFormat.Jpeg);
                    //stream2.Position = 0;
                    //blobref_thumb.UploadFromStream(stream2);


                    using (Image image = Image.Load(stream))
                    {
                        int width = 50;
                        int height = 50;
                        image.Mutate(x => x.Resize(width, height));
                        image.SaveAsJpeg(stream2);
                    }
                    stream2.Position = 0;
                    blobref_thumb.UploadFromStream(stream2);

                    var registration = new Registration()
                    {
                        RowKey = m,
                        PartitionKey = lastname[0].ToString(),
                        Name = firstname,
                        Lastname = lastname,
                        Photourl = m + "_th.jpeg"
                    };
                    tableref.Execute(TableOperation.Insert(registration));

                }
                await Task.Delay(1000);
            }
        }
    }
}
