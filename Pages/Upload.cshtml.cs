using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace OTA_Service
{
    public class UploadModel : PageModel
    {
        // https://community.scripture.software.sil.org/t/distributing-ios-ipa-apps-with-ota-over-the-air-instead-of-apple-app-store/728

        private const string Placeholder_Url = "{url}";
        private const string Placeholder_Title = "{title}";
        private const string Placeholder_BundleIdentifier = "{bundle-identifier}";
        private const string Placeholder_PListUrl = "{plist-url}";
        private const string Placeholder_Version = "{version}";



        [BindProperty]
        public IFormFile Upload { get; set; }

        [BindProperty]
        public string Name { get; set; }
        [BindProperty]
        public string BundleIdentifier { get; set; }


        public async Task OnPostAsync()
        {
            if (string.IsNullOrEmpty(this.BundleIdentifier))
                return;
            if (Upload == null)
                return;

            string bundleIdentifier = this.BundleIdentifier;
            string title = this.Name;
            string version = "1.0.0";

            UploadModel.CreateAppsDirectory(bundleIdentifier);

            string ipaUrl = await CopyIPA(bundleIdentifier);
            string pListUrl = CopyPList(bundleIdentifier, title, version, ipaUrl);
            string htmlUrl = CopyHtml(bundleIdentifier, title, pListUrl);

            AppPackageCache.Add(this.BundleIdentifier, this.Name, version);
            AppPackageCache.Save();
            this.Upload = null;
            this.Name = string.Empty;
            this.BundleIdentifier = string.Empty;
        }
        public void OnGet()
        {
            AppPackageCache.Initialize();
            System.Diagnostics.Debug.WriteLine("Cached files: " + AppPackageCache.Count);
        }

        public string Host
        {
            get
            {

                if (this.Request.Host.Host.ToLowerInvariant().Equals("localhost"))
                    return GetLocalIPAddress();
                return this.Request.Host.Host;
            }
        }


        // ! ! ! !
        //  move the 'saved' info about an application package to an xml or json file
        //  ->  use information from that file on a separate *.cshtml file to display a list of download links pointing to the respective *.plist files


        private async Task<string> CopyIPA(string bundleIdentifier)
        {
            string hostValue = GetLocalIPAddress();
            //string hostValue2 = GetLocalIPAddress2();

            // {title}
            // {plist-url}
            UploadModel.CreateAppsDirectory(bundleIdentifier);

            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "apps");
            FileInfo fiUpload = new FileInfo(Upload.FileName);
            FileInfo fiTarget = new FileInfo(Path.Combine(path, bundleIdentifier, this.BundleIdentifier + ".ipa"));
            string ipaUrl = PathWeb.Combine("https://" + this.Host, "apps", bundleIdentifier, this.BundleIdentifier + ".ipa");

            if (fiTarget.Exists)
            {
                fiTarget.Delete();
                fiTarget.Refresh();
            }

            using (var fileStream = new FileStream(fiTarget.FullName, FileMode.Create))
            {
                await Upload.CopyToAsync(fileStream);
            }

            return ipaUrl;
        }

        private static void CreateAppsDirectory(string bundleIdentifier)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "apps");
            DirectoryInfo diTarget = new DirectoryInfo(Path.Combine(path, bundleIdentifier));
            if (!diTarget.Exists)
                diTarget.Create();
        }
        private string CopyPList(string bundleIdentifier, string title, string version, string urlToIPA)
        {
            // {url}
            // {title}
            // {bundle-identifier}
            UploadModel.CreateAppsDirectory(bundleIdentifier);
            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "apps");
            FileInfo fiSource = new FileInfo(Path.Combine(path, "template.xml"));
            FileInfo fiTarget = new FileInfo(Path.Combine(path, bundleIdentifier, bundleIdentifier + ".plist"));
            string pListUrl = PathWeb.Combine("https://" + this.Host, "apps", bundleIdentifier, bundleIdentifier + ".plist");
            if (fiTarget.Exists)
            {
                fiTarget.Delete();
                fiTarget.Refresh();
            }

            using (StreamWriter sw = fiTarget.CreateText())
            {
                using (StreamReader sr = new StreamReader(fiSource.OpenRead()))
                {
                    string content = sr.ReadToEnd();
                    content = content.Replace(Placeholder_Url, urlToIPA);
                    content = content.Replace(Placeholder_Title, title);
                    content = content.Replace(Placeholder_BundleIdentifier, bundleIdentifier);
                    content = content.Replace(Placeholder_Version, version);
                    sw.Write(content);
                    sw.Flush();
                }
                sw.Close();
            }
            return pListUrl;
        }
        private string CopyHtml(string bundleIdentifier, string title, string urlToPList)
        {
            // {title}
            // {plist-url}
            UploadModel.CreateAppsDirectory(bundleIdentifier);
            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "apps");
            FileInfo fiSource = new FileInfo(Path.Combine(path, "template.html"));
            FileInfo fiTarget = new FileInfo(Path.Combine(path, bundleIdentifier, "index.html"));
            string htmlUrl = PathWeb.Combine("https://" + this.Host, "apps", bundleIdentifier, "index.html");

            if (fiTarget.Exists)
            {
                fiTarget.Delete();
                fiTarget.Refresh();
            }

            using (StreamWriter sw = fiTarget.CreateText())
            {
                using (StreamReader sr = new StreamReader(fiSource.OpenRead()))
                {
                    string content = sr.ReadToEnd();
                    content = content.Replace(Placeholder_PListUrl, urlToPList);
                    content = content.Replace(Placeholder_Title, title);
                    sw.Write(content);
                    sw.Flush();
                }
                sw.Close();
            }
            return htmlUrl;
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        public static string GetLocalIPAddress2()
        {
            string localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            return localIP;
        }

    }


    public class PathWeb
    {
        public static string Combine(params string[] values)
        {
            return string.Join("/", values);
        }
    }

    public class AppPackageInfo
    {
        public string Url { get; set; }
        public string Identifier { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }

    }

    public class AppPackageCache
    {

        private static object _cacheLock = new object();
        private static List<AppPackageInfo> _packageInfos = new List<AppPackageInfo>();

        public static int Count
        { get => _packageInfos.Count; }

        public static void Initialize()
        {
            if (_packageInfos.Count == 0)
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "apps");
                FileInfo fiCache = new FileInfo(Path.Combine(path, "cache.json"));
                if (fiCache.Exists)
                {
                    lock (_cacheLock)
                    {
                        string jsonString = File.ReadAllText(fiCache.FullName);
                        List<AppPackageInfo> packageInfos = JsonSerializer.Deserialize<List<AppPackageInfo>>(jsonString);
                        _packageInfos = packageInfos;
                    }
                }
            }
        }

        public static void Save()
        {
            if (_packageInfos != null)
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "apps");
                FileInfo fiCache = new FileInfo(Path.Combine(path, "cache.json"));

                lock (_cacheLock)
                {
                    File.WriteAllText(fiCache.FullName, JsonSerializer.Serialize<List<AppPackageInfo>>(_packageInfos));
                }
            }
        }


        public static void Add(string identifier, string name, string version = "1.0.0")
        {
            AppPackageInfo info = new AppPackageInfo() { Identifier = identifier, Name = name, Version = version, Url = PathWeb.Combine("apps", identifier, "index.html") };
            AppPackageInfo existingInfo = _packageInfos.FirstOrDefault(x => (x.Name.Equals(info.Name) && x.Identifier.Equals(info.Identifier)));
            if (existingInfo != null)
                _packageInfos.Remove(existingInfo);
            _packageInfos.Add(info);
        }




    }

}