﻿using Serilog;
using System.Diagnostics;
using System.Net;
using System.Web;

namespace Segra.Backend.ContentServer
{
    internal class ContentServer
    {
        private static HttpListener _httpListener;

        public static void StartServer(string prefix)
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(prefix);
            _httpListener.Start();
            Console.WriteLine($"Server started at {prefix}");
            _httpListener.BeginGetContext(OnRequest, null);
        }

        private static void OnRequest(IAsyncResult result)
        {
            try
            {
                _httpListener.BeginGetContext(OnRequest, null);
                var context = _httpListener.EndGetContext(result);
                var request = context.Request;
                var response = context.Response;

                if (request.RawUrl.StartsWith("/api/thumbnail"))
                {
                    HandleThumbnailRequest(context);
                }
                else if (request.RawUrl.StartsWith("/api/content"))
                {
                    HandleContentRequest(context);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    using (var writer = new StreamWriter(response.OutputStream))
                    {
                        writer.Write("Invalid endpoint.");
                    }
                    response.Close();
                }
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode != 995)
                {
                    Log.Error(ex, "HttpListenerException occurred.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in OnRequest.");
            }
        }

        private static void HandleThumbnailRequest(HttpListenerContext context)
        {
            var query = HttpUtility.ParseQueryString(context.Request.Url.Query);
            string input = query["input"];
            string timeParam = query["time"];
            var response = context.Response;

            try
            {
                Log.Information("HandleThumbnailRequest started: input={Input}, time={TimeParam}", input, timeParam);
                response.AddHeader("Access-Control-Allow-Origin", "*");

                if (!File.Exists(input))
                {
                    Log.Warning("Thumbnail request file not found: {Input}", input);
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    using (var writer = new StreamWriter(response.OutputStream))
                    {
                        writer.Write("File not found.");
                    }
                    return;
                }

                if (string.IsNullOrEmpty(timeParam))
                {
                    Log.Information("No time param provided; serving existing thumbnail from disk: {Input}", input);
                    byte[] buffer = File.ReadAllBytes(input);
                    response.ContentType = "image/jpeg";
                    response.AddHeader("Cache-Control", "public, max-age=86400");
                    response.AddHeader("Expires", DateTime.UtcNow.AddDays(1).ToString("R"));
                    var lastModified = File.GetLastWriteTimeUtc(input);
                    response.AddHeader("Last-Modified", lastModified.ToString("R"));
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    Log.Information("Served existing thumbnail: {Input} (length={Length} bytes)", input, buffer.Length);
                }
                else
                {
                    Log.Information("Time param is {TimeParam}; generating on-the-fly thumbnail from {Input}", timeParam, input);
                    if (!double.TryParse(timeParam, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out double timeSeconds))
                    {
                        Log.Warning("Could not parse timeParam={TimeParam} as double, using 0.0 fallback", timeParam);
                        timeSeconds = 0.0;
                    }

                    string ffmpegPath = "ffmpeg.exe";
                    if (!File.Exists(ffmpegPath))
                    {
                        Log.Error("FFmpeg executable not found: {FfmpegPath}", ffmpegPath);
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        using (var writer = new StreamWriter(response.OutputStream))
                        {
                            writer.Write("FFmpeg not found on server.");
                        }
                        return;
                    }

                    string timeString = timeSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string ffmpegArgs = $"-y -ss {timeString} -i \"{input}\" -frames:v 1 -vf scale=320:-1 -f image2pipe -vcodec mjpeg -q:v 20 pipe:1";
                    Log.Information("Running FFmpeg: {FfmpegPath} {FfmpegArgs}", ffmpegPath, ffmpegArgs);

                    var processInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = ffmpegArgs,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    byte[] jpegBytes;
                    using (var ffmpegProcess = new Process { StartInfo = processInfo })
                    {
                        ffmpegProcess.Start();

                        // Read the image bytes from stdout.
                        using (var ms = new MemoryStream())
                        {
                            ffmpegProcess.StandardOutput.BaseStream.CopyTo(ms);
                            jpegBytes = ms.ToArray();
                        }

                        // Read any error messages
                        string ffmpegStdErr = ffmpegProcess.StandardError.ReadToEnd();
                        ffmpegProcess.WaitForExit();
                        if (ffmpegProcess.ExitCode != 0)
                        {
                            Log.Error("FFmpeg error (exit={ExitCode}). Stderr={StdErr}", ffmpegProcess.ExitCode, ffmpegStdErr);
                            response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            using (var writer = new StreamWriter(response.OutputStream))
                            {
                                writer.Write($"FFmpeg error:\n{ffmpegStdErr}");
                            }
                            return;
                        }
                        else
                        {
                            Log.Information("FFmpeg completed successfully. (Stderr={StdErr})", ffmpegStdErr);
                        }
                    }

                    // Serve the image directly from memory
                    if (jpegBytes != null && jpegBytes.Length > 0)
                    {
                        response.ContentType = "image/jpeg";
                        response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");
                        response.AddHeader("Pragma", "no-cache");
                        response.AddHeader("Expires", "0");
                        response.ContentLength64 = jpegBytes.Length;
                        response.OutputStream.Write(jpegBytes, 0, jpegBytes.Length);
                        Log.Information("Served on-the-fly thumbnail (length={Length} bytes) at time={TimeSeconds}", jpegBytes.Length, timeSeconds);
                    }
                    else
                    {
                        Log.Error("No thumbnail data received from FFmpeg.");
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        using (var writer = new StreamWriter(response.OutputStream))
                        {
                            writer.Write("Failed to generate thumbnail.");
                        }
                    }
                }
            }
            catch (HttpListenerException ex)
            {
                Log.Warning(ex, "HttpListenerException in HandleThumbnailRequest, possibly client disconnected.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while processing thumbnail request.");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                using (var writer = new StreamWriter(response.OutputStream))
                {
                    writer.Write("An error occurred.");
                }
            }
            finally
            {
                try
                {
                    response.Close();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error closing response in HandleThumbnailRequest.");
                }
            }
        }

        private static void HandleContentRequest(HttpListenerContext context)
        {
            try
            {
                var query = HttpUtility.ParseQueryString(context.Request.Url.Query);
                string fileName = query["input"];
                string type = query["type"];
                var response = context.Response;

                if (File.Exists(fileName))
                {
                    FileInfo fileInfo = new FileInfo(fileName);
                    long fileLength = fileInfo.Length;
                    string rangeHeader = context.Request.Headers["Range"];
                    long start = 0, end = fileLength - 1;

                    if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes="))
                    {
                        string[] range = rangeHeader.Substring("bytes=".Length).Split('-');
                        if (!string.IsNullOrEmpty(range[0]))
                            start = long.Parse(range[0]);
                        if (!string.IsNullOrEmpty(range[1]))
                            end = long.Parse(range[1]);
                    }

                    if (start > end || end >= fileLength)
                    {
                        response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
                        response.Close();
                        return;
                    }

                    response.StatusCode = (int)HttpStatusCode.PartialContent;
                    response.Headers.Add("Accept-Ranges", "bytes");
                    response.Headers.Add("Content-Range", $"bytes {start}-{end}/{fileLength}");
                    response.ContentType = "video/mp4";

                    long contentLength = end - start + 1;
                    response.ContentLength64 = contentLength;

                    using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                    {
                        fs.Seek(start, SeekOrigin.Begin);
                        byte[] buffer = new byte[64 * 1024];
                        long bytesRemaining = contentLength;
                        while (bytesRemaining > 0)
                        {
                            int bytesRead = fs.Read(buffer, 0, (int)Math.Min(buffer.Length, bytesRemaining));
                            if (bytesRead == 0)
                                break;
                            response.OutputStream.Write(buffer, 0, bytesRead);
                            bytesRemaining -= bytesRead;
                        }
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    using (var writer = new StreamWriter(response.OutputStream))
                    {
                        writer.Write("Video not found.");
                    }
                }
                response.Close();
            }
            catch (HttpListenerException ex)
            {
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
        }

        public static void StopServer()
        {
            _httpListener.Stop();
        }
    }
}
