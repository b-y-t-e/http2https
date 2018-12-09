using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Http2s
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "head", "put", "delete", "connect", "options", "trace", "patch", Route = null)] HttpRequest originalRequest,
            ILogger log)
        {
            try
            {
                //log.LogInformation("C# HTTP trigger function processed a request.");

                //string name = req.Query["name"];

                var address = GetEnvironmentVariable("destAddress");
                address = address.TrimEnd('/');
                var addressFull = address + originalRequest.QueryString.Value.TrimStart('?');

                HttpWebRequest proxyRequest = HttpWebRequest.Create(addressFull) as HttpWebRequest;
                if (proxyRequest != null)
                {
                    proxyRequest.Method = originalRequest.Method;
                    foreach (string header in originalRequest.Headers.Keys)
                    {
                        //log.LogInformation("header = " + header);

                        string val = originalRequest.Headers[header];

                        //log.LogInformation("header val = " + val);

                        proxyRequest.Headers.Add(header, val);
                        // result.Content.Headers.Add(
                    }

                    if (proxyRequest.Method != "GET")
                    {
                        Byte[] originalRequestBody = CopyToArray(
                            originalRequest.Body);

                        using (var stream = proxyRequest.GetRequestStream())
                        {
                            stream.Write(originalRequestBody, 0, originalRequestBody.Length);
                        }
                    }

                    HttpWebResponse response = proxyRequest.GetResponse() as HttpWebResponse;
                    try
                    {
                        if (response != null)
                        {
                            Byte[] bytes = CopyToArray(
                                response.GetResponseStream());

                            var result = new HttpResponseMessage(response.StatusCode);
                            result.Content = new ByteArrayContent(bytes);

                            result.Content.Headers.Clear();
                            foreach (string header in response.Headers.Keys)
                            {
                                //log.LogInformation("header = " + header);

                                string val = response.Headers[header];

                                //log.LogInformation("header val = " + val);

                                result.Headers.TryAddWithoutValidation(header, val);
                                // result.Content.Headers.Add(
                            }

                            /* result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                             { FileName = "file.html" };*/
                            // result.Content.Headers.ContentType = new MediaTypeHeaderValue(response.ContentType);

                            return result;

                            /*return ToStream(
                                response.Headers,
                                response.ContentType,
                                bytes);*/
                        }
                    }
                    finally
                    {
                        if (response != null)
                            response.Close();
                    }
                }

                throw new Exception("!!!");

                /*string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                name = name ?? data?.name;

                return name != null
                    ? (ActionResult)new OkObjectResult($"Hello, {name}")
                    : new BadRequestObjectResult("Please pass a name on the query string or in the request body");*/
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static string GetEnvironmentVariable(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process).Replace("'", "");
        }

        public static void CopyTo(Stream source, Stream destination)
        {
            byte[] buffer = new byte[16384];
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, bytesRead);
            }
        }

        public static byte[] CopyToArray(Stream source)
        {
            using (var ms = new MemoryStream())
            {
                CopyTo(source, ms);
                return ms.ToArray();
            }
        }

        /*static Stream ToStream(
           String FileName,
           Byte[] Content)
        {
            if (!String.IsNullOrEmpty(FileName) && Content != null)
            {
                var extension = Path.GetExtension(FileName);
                var mimeType = MimeTypeHelper.GetMimeType(extension);

                WebOperationContext.Current.OutgoingResponse.ContentType = mimeType;
                WebOperationContext.Current.OutgoingResponse.ContentLength = Content.Length;
                WebOperationContext.Current.OutgoingResponse.Headers.Add("Content-Disposition", "attachment; filename=" + FileName);

                return new MemoryStream(Content);
            }
            return null;
        }*/

        /* static Stream ToStream(
            WebHeaderCollection Headers,
            String ContentType,
            Byte[] Content)
         {
             if (Content != null)
             {
                 if (!String.IsNullOrEmpty(ContentType))
                 {
                     WebOperationContext.Current.OutgoingResponse.ContentType = ContentType;
                 }

                 WebOperationContext.Current.OutgoingResponse.ContentLength = Content.Length;

                 if (Headers != null)
                     foreach (String headerName in Headers.Keys)
                     {
                         String headerNameToLower = headerName.ToLower().Trim();
                         if (!new[] { "content-type", "content-length" }.Contains(headerNameToLower))
                         {
                             String val = Headers[headerName];

                             WebOperationContext.Current.OutgoingResponse.Headers.Set(
                                 headerName,
                                 val);

                         }
                     }
                 //if (!String.IsNullOrEmpty(FileName))
                 //  WebOperationContext.Current.OutgoingResponse.Headers.Add("Content-Disposition", "attachment; filename=" + FileName);

                 return new MemoryStream(Content);
             }
             return null;
         }*/

    }
}
