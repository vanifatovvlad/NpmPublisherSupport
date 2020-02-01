#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace NpmPackageLoader
{
    public class FtpData
    {
        public string Url { get; set; }
        public string User { get; set; }
        public string Password { get; set; }

        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
        public string UnityPackageName { get; set; }

        public string LocalFile { get; set; }
    }

    public static class FtpUtils
    {
        private static WebClient _request;

        public static void Upload(FtpData data, Action success, Action fail)
        {
            const string progressHeader = "Upload";

            try
            {
                var remoteServerUrl = data.Url;
                var remotePackageDir = $"{remoteServerUrl}/{data.PackageName}";
                var remoteVersionDir = $"{remotePackageDir}/{data.PackageVersion}";
                var remoteFile = new Uri($"{remoteVersionDir}/{data.UnityPackageName}.unitypackage");

                var localFile = data.LocalFile;
                var localFileInfo = new FileInfo(localFile);
                if (!localFileInfo.Exists)
                {
                    Debug.LogError($"{localFile} not exists");
                    fail();
                }

                var totalBytes = localFileInfo.Length;

                EditorUtility.DisplayProgressBar(progressHeader, remoteVersionDir, 0f);

                CreateDirectoryIfNotExists(remotePackageDir, data);
                CreateDirectoryIfNotExists(remoteVersionDir, data);

                _request = new WebClient();

                _request.UploadProgressChanged += (sender, args) =>
                {
                    var send = args.BytesSent;
                    var info = $"{ToHumanSize(send)} / {ToHumanSize(totalBytes)}";
                    var progress = 1f * send / totalBytes;
                    EditorUtility.DisplayProgressBar(progressHeader, info, progress);
                };

                _request.UploadFileCompleted += (sender, args) =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    _request.Dispose();
                    _request = null;

                    EditorUtility.ClearProgressBar();

                    if (args.Error != null)
                    {
                        Debug.LogException(args.Error);
                        fail();
                    }
                    else
                    {
                        success();
                    }
                };

                _request.Credentials = new NetworkCredential(data.User, data.Password);
                _request.UploadFileAsync(remoteFile, localFile);
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(ex);
                _request?.Dispose();

                fail();
            }
        }

        public static void Download(FtpData data, Action success, Action fail)
        {
            const string progressHeader = "Download";

            try
            {
                var localFile = data.LocalFile;
                var remoteVersionDir = $"{data.Url}/{data.PackageName}/{data.PackageVersion}/";
                var remoteFile = new Uri($"{remoteVersionDir}/{data.UnityPackageName}.unitypackage");
                var credentials = new NetworkCredential(data.User, data.Password);

                var totalBytes = FetchFileSize(remoteFile, credentials);

                _request = new WebClient();

                _request.DownloadProgressChanged += (sender, args) =>
                {
                    var received = args.BytesReceived;
                    var info = $"{ToHumanSize(received)} / {ToHumanSize(totalBytes)}";
                    var progress = 1f * received / totalBytes;
                    EditorUtility.DisplayProgressBar(progressHeader, info, progress);
                };

                _request.DownloadFileCompleted += (sender, args) =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    _request.Dispose();
                    _request = null;

                    EditorUtility.ClearProgressBar();

                    if (args.Error != null)
                    {
                        Debug.LogException(args.Error);
                        fail();
                    }
                    else
                    {
                        success();
                    }
                };

                _request.Credentials = credentials;
                _request.DownloadFileAsync(remoteFile, localFile);
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(ex);
                _request?.Dispose();
                fail();
            }
        }

        private static long FetchFileSize(Uri url, NetworkCredential credential)
        {
            long bytesTotal = 0;

            try
            {
                var request = (FtpWebRequest) WebRequest.Create(url);
                request.Method = WebRequestMethods.Ftp.GetFileSize;
                request.Credentials = credential;
                var response = (FtpWebResponse) request.GetResponse();

                bytesTotal = response.ContentLength;
                response.Close();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return Math.Max(bytesTotal, 1);
        }

        private static void CreateDirectoryIfNotExists(string url, FtpData data)
        {
            try
            {
                var r = (FtpWebRequest) WebRequest.Create(url);
                r.Credentials = new NetworkCredential(data.User, data.Password);
                r.Method = WebRequestMethods.Ftp.MakeDirectory;

                using (var resp = (FtpWebResponse) r.GetResponse())
                {
                    Console.WriteLine(resp.StatusCode);
                }
            }
            catch (WebException)
            {
                // already exist
            }
        }

        private static string ToHumanSize(long bytes)
        {
            if (bytes > 1024 * 1024) return (bytes / 1024 / 1024) + " Mb";
            if (bytes > 1024) return (bytes / 1024) + " Kb";
            return bytes + " b";
        }
    }
}

#endif