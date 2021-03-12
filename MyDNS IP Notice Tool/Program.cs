using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace MyDNS_IP_Notice_Tool
{
    internal class Program
    {
        private const string NOTICE_ENDPOINT = "https://ipv4.mydns.jp/login.html";
        private const string IP_API_ENDPOINT = "http://www.httpbin.org/ip";

        private static IPAddress _ipAddressCache;

        private static readonly HttpClient client = new HttpClient();

        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));
            Config config = new Config();
            if(!config.Load())config.Save();
            if(config == null)
            {
                _logger.Warn("Failed to  load configuration file");
            }
            try{
                bool success = false;
                if (ShouldNoticeAddress())
                {
                    success = NoticeAsync(config.UserID, config.Password);
                }
                else
                {
                    _logger.Info("No need to notice IP address");
                }
                if (success)
                {
                    using (var writer = new BinaryWriter(new FileStream("appdata.bin", FileMode.OpenOrCreate)))
                    {
                        writer.Write(DateTime.Now.ToBinary());
                        writer.Write((_ipAddressCache ?? GetGlobalIPAddress()).GetAddressBytes());
                    }
                }
            }
            catch(Exception e)
            {
                _logger.Warn(e.ToString());
            }
        }

        private static bool ShouldNoticeAddress()
        {
            if (!File.Exists("appdata.bin")) return true;
            using (var reader = new BinaryReader(new FileStream("appdata.bin", FileMode.OpenOrCreate)))
            {
                try
                {
                    DateTime date = DateTime.FromBinary(reader.ReadInt64());
                    IPAddress address = new IPAddress(reader.ReadBytes(4));
                    _ipAddressCache = GetGlobalIPAddress();
                    if (!_ipAddressCache.Equals(address)) return true;
                    TimeSpan span = DateTime.Now - date;
                    if (span.TotalDays >= 1d) return true;
                    return false;
                }
                catch (Exception e)
                {
                    return true;
                }
            }
        }

        private static IPAddress GetGlobalIPAddress()
        {
            HttpResponseMessage response = client.Send(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(IP_API_ENDPOINT)
            });
            string result = response.Content.ReadAsStringAsync().Result;
            return IPAddress.Parse(Regex.Match(result, @"\d+.\d+.\d+.\d+").Value);
        }

        private static bool NoticeAsync(string userName, string userPassword)
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(NOTICE_ENDPOINT)
            };
            string EncodedAuthText = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", userName, userPassword)));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", EncodedAuthText);

            HttpResponseMessage response;
            response = client.Send(request);

            if (response.IsSuccessStatusCode)
            {
                string html = response.Content.ReadAsStringAsync().Result;
                string address = Regex.Match(Regex.Match(html, @"<DT>REMOTE ADDRESS:</DT><DD>\d+.\d+.\d+.\d+</DD>").Value, @"\d+.\d+.\d+.\d+").Value;

                _logger.Info($"Notification was successful(IP:{address})");
                return true;
            }
            else
            {
                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.Unauthorized:
                        _logger.Info("Notification failed : invalid UserID or Password");
                        break;

                    default:
                        _logger.Info("Notification failed : make sure you are connected to the internet");
                        break;
                }
                return false;
            }
        }
    }
}