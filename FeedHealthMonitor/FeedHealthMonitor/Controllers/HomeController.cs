using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FeedHealthMonitor.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            try
            {
                var logPath = _configuration["Monitoring:Path"];
                DirectoryInfo directory = Directory.CreateDirectory(logPath);
                IOrderedEnumerable<FileInfo> files = directory.GetFilesWithSubfolders()
                    .OrderByDescending(f => f.LastWriteTime);
                var lastFlightUpdate = files.FirstOrDefault(f => f.Name.StartsWith("FlightUpdate"));
                var lastFlightInsert = files.FirstOrDefault(f => f.Name.StartsWith("FlightInsert"));
                var lastLoadPlanInsert = files.FirstOrDefault(f => f.Name.StartsWith("LoadPlan"));
                var lastAcarsInsert = files.FirstOrDefault(f => f.Name.StartsWith("Acars"));
                var error = files.FirstOrDefault(f => f.Name.Contains("Exception") || f.Name.Contains("Fail"));
                var lastErrorFile = error;
                ViewBag.LastErrorFile = error?.FullName;
                ViewBag.LastFlightUpdateTime = (lastFlightUpdate?.LastWriteTime == null)
                    ? "Never"
                    : lastFlightUpdate.LastWriteTime.ToString();
                ViewBag.LastFlightInsertTime = (lastFlightInsert?.LastWriteTime == null)
                    ? "Never"
                    : lastFlightInsert.LastWriteTime.ToString();
                ViewBag.LastLoadPlanInsertTime = (lastLoadPlanInsert?.LastWriteTime == null)
                    ? "Never"
                    : lastLoadPlanInsert.LastWriteTime.ToString();
                ViewBag.LastAcarsInsertTime = (lastAcarsInsert?.LastWriteTime == null)
                    ? "Never"
                    : lastAcarsInsert.LastWriteTime.ToString();
                ViewBag.LastAcarsInsertTime = (lastAcarsInsert?.LastWriteTime == null)
                    ? "Never"
                    : lastAcarsInsert.LastWriteTime.ToString();
                ViewBag.LastErrorTime = (error?.LastWriteTime == null) ? "Never" : error.LastWriteTime.ToString();

                var flUpdDiff = new TimeSpan(365, 0, 0);
                if (lastFlightUpdate != null) flUpdDiff = DateTime.Now - lastFlightUpdate.LastWriteTime;
                ViewData["FlightUpdateImage"] = GetStatusImage(flUpdDiff);

                var flInsDiff = new TimeSpan(365, 0, 0);
                if (lastFlightInsert != null) flInsDiff = DateTime.Now - lastFlightInsert.LastWriteTime;
                ViewData["FlightInsertImage"] = GetStatusImage(flInsDiff);

                var lpnsiff = new TimeSpan(365, 0, 0);
                if (lastLoadPlanInsert != null) lpnsiff = DateTime.Now - lastLoadPlanInsert.LastWriteTime;
                ViewData["LoadPlanInsertImage"] = GetStatusImage(lpnsiff);

                var acarInsDiff = new TimeSpan(365, 0, 0);
                if (lastAcarsInsert != null) acarInsDiff = DateTime.Now - lastAcarsInsert.LastWriteTime;
                ViewData["AcarsInsertImage"] = GetStatusImage(acarInsDiff);

                var errorDiff = new TimeSpan(365, 0, 0);
                if (error != null) errorDiff = DateTime.Now - error.LastWriteTime;
                ViewData["LastError"] = GetErrorStatusImage(errorDiff);

                var flightInStore = GetFlightsInStore();
                ViewData["MWStore"] = (flightInStore > 0)
                    ? Url.Content("~/Content/red_light.png")
                    : Url.Content("~/Content/Green_light.png");
                ViewBag.FlightsInStore = flightInStore;
                return View();
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText($@"{Startup.RootPath}\Error_{DateTime.Now.Ticks}.log", ex.ToString());
                throw;
            }
        }

        private int GetFlightsInStore()
        {
            SqlConnection conn = new SqlConnection("Data Source=SQLMSGDEVLST;Initial Catalog=MessagingTest;Integrated Security=True");
            conn.Open();
            var  cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText =
                "select count(*) from MessagingTest.dbo.Event_Store" +
                " where application_category = 'FLIGHT'" +
                " and ReprocessedTimestamp is null";
            var flightsNotProcessed = cmd.ExecuteScalar();
            return (int)flightsNotProcessed;
        }

        private string GetStatusImage(TimeSpan diff)
        {
            var src = string.Empty;
            if (diff < new TimeSpan(0, 10, 0))
            {
                return Url.Content("~/Content/Green_light.png");
            }

            if (diff > new TimeSpan(0, 10, 0 ) && diff < new TimeSpan(0, 30, 0))
            {
                return Url.Content("~/Content/Yellow_light.png");
            }

                return Url.Content("~/Content/red_light.png");
        }

        private string GetErrorStatusImage(TimeSpan diff)
        {
            var src = string.Empty;
            if (diff < new TimeSpan(0, 10, 0))
            {
                return Url.Content("~/Content/red_light.png");
            }

            if (diff < new TimeSpan(12, 0, 0))
            {
                return Url.Content("~/Content/Yellow_light.png");
            }
            
            return Url.Content("~/Content/Green_light.png");
        }


        public IActionResult Error()
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }

       
    }
    public static class DirUtils
    {
        public static IEnumerable<FileInfo> GetFilesWithSubfolders(this DirectoryInfo dir)
        {
            IEnumerable<FileInfo> files = Enumerable.Empty<FileInfo>();

            files = GetFilesFromSubfolders(dir);
           
            return files;
        }

        private static IEnumerable<FileInfo> GetFilesFromSubfolders(DirectoryInfo dir)
        {
            
            var subdirs = dir.GetDirectories();
            IEnumerable<FileInfo> retfiles = dir.GetFiles();
            IEnumerable<FileInfo> files = Enumerable.Empty<FileInfo>();
            foreach (var sdir in subdirs)
            {
               
                retfiles = retfiles.Concat(GetFilesFromSubfolders(sdir)).ToArray();
            }

            return retfiles;
        }
    }
}
