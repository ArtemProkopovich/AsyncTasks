using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Task
{
    public static class Tasks
    {
        /// <summary>
        /// Returns the content of required uri's.
        /// Method has to use the synchronous way and can be used to compare the
        ///  performace of sync/async approaches. 
        /// </summary>
        /// <param name="uris">Sequence of required uri</param>
        /// <returns>The sequence of downloaded url content</returns>
        public static IEnumerable<string> GetUrlContent(this IEnumerable<Uri> uris)
        {
            using (WebClient client = new WebClient())
                return uris.Select(uri => client.DownloadString(uri)).ToList();
        }

        /// <summary>
        /// Returns the content of required uris.
        /// Method has to use the asynchronous way and can be used to compare the performace 
        /// of sync \ async approaches. 
        /// maxConcurrentStreams parameter should control the maximum of concurrent streams 
        /// that are running at the same time (throttling). 
        /// </summary>
        /// <param name="uris">Sequence of required uri</param>
        /// <param name="maxConcurrentStreams">Max count of concurrent request streams</param>
        /// <returns>The sequence of downloaded url content</returns>
        public static IEnumerable<string> GetUrlContentAsync(this IEnumerable<Uri> uris, int maxConcurrentStreams)
        {
            using (HttpClient client = new HttpClient())
            {
                List<Uri> uriList = uris.ToList();
                List<string> result = new List<string>();
                for (int j = 0; j < uriList.Count; j++)
                    result.Add(null);
                int[] tasksNums = new int[Math.Min(uriList.Count, maxConcurrentStreams)];
                Task<string>[] tasks = new Task<string>[Math.Min(uriList.Count, maxConcurrentStreams)];
                int i = 0;
                for (; i < maxConcurrentStreams && i < uriList.Count; i++)
                {
                    tasks[i] = client.GetStringAsync(uriList[i]);
                    tasksNums[i] = i;
                }

                while (i < uriList.Count)
                {
                    int num = System.Threading.Tasks.Task.WaitAny(tasks);
                    result[tasksNums[num]] =tasks[num].Result;
                    tasks[num] = client.GetStringAsync(uriList[i]);
                    tasksNums[num] = i;
                    i++;
                }
                System.Threading.Tasks.Task.WaitAll(tasks);
                for (int j = 0; j < tasks.Length; j++)
                    result[tasksNums[j]] = tasks[j].Result;
                return result;
            }   
        }

        /// <summary>
        /// Calculates MD5 hash of required resource.
        /// 
        /// Method has to run asynchronous. 
        /// Resource can be any of type: http page, ftp file or local file.
        /// </summary>
        /// <param name="resource">Uri of resource</param>
        /// <returns>MD5 hash</returns>
        public static async Task<string> GetMD5Async(this Uri resource)
        {
            Stream stream;
            switch (resource.Scheme)
            {
                case "https":
                case "http":
                    using (HttpClient client = new HttpClient())
                    {                      
                        stream = await client.GetStreamAsync(resource);
                    }
                    break;
                case "ftp":
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(resource);
                    request.Method = WebRequestMethods.Ftp.DownloadFile;
                    FtpWebResponse response = (FtpWebResponse) await request.GetResponseAsync();

                    Stream responseStream = response.GetResponseStream();
                    stream = responseStream;
                    break;
                default:
                    stream = new FileStream(resource.LocalPath, FileMode.Open, FileAccess.Read);
                    break;
            }
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(stream);
            stream.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
