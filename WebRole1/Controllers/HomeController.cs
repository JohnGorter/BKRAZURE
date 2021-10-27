using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebRole1.Controllers
{
    public class Registration : TableEntity
    {
        public String Name { get; set; }
        public String Lastname { get; set; }
        public String Photourl { get; set; }
    }

    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            CloudStorageAccount csa = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=johngorterstorage2;AccountKey=wU8dAPycZjQLYhdAywnq56eWwehF8ICT7M51gFIIO/3rG/78rX/dFTLvl2eS+OekXbosi4oSm8FKpcerLZi6Rg==;EndpointSuffix=core.windows.net");
            var tableclient = csa.CreateCloudTableClient();
            var tableref = tableclient.GetTableReference("registrations");
            tableref.CreateIfNotExists();

            var r = tableref.ExecuteQuery(new TableQuery<Registration>()).ToList();
            ViewBag.registrations = r; 

            return View();
        }

        [HttpPost]
        public ActionResult Index(String name, String lastname, HttpPostedFileBase photo)
        {
            Debug.WriteLine(photo.FileName);


            CloudStorageAccount csa = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=johngorterstorage2;AccountKey=wU8dAPycZjQLYhdAywnq56eWwehF8ICT7M51gFIIO/3rG/78rX/dFTLvl2eS+OekXbosi4oSm8FKpcerLZi6Rg==;EndpointSuffix=core.windows.net");
            var queueclient = csa.CreateCloudQueueClient();
            var blobclient = csa.CreateCloudBlobClient();

            var queueref = queueclient.GetQueueReference("registrations");
            var containerref = blobclient.GetContainerReference("registrations");

            containerref.CreateIfNotExists();
            queueref.CreateIfNotExists();


            var blobref = containerref.GetBlockBlobReference($"{name}-{lastname}.jpeg");
            blobref.UploadFromStream(photo.InputStream);

            queueref.AddMessage(new CloudQueueMessage($"{name}-{lastname}"));




            return Redirect("/");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}