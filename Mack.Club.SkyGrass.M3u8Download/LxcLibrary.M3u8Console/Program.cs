using LxcLibrary.M3u8Download;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LxcLibrary.M3u8Console
{
    class Program
    {
        const string Type_DownLoad = "Type_DownLoad";
        const string Type_ParseJson = "Type_ParseJson";

        static void Main(string[] args)
        {
            M3u8DownloadTool ffmpeg = new M3u8DownloadTool();
            string type = Type_DownLoad;
            try
            {
                type = args[0];
            }
            catch (Exception)
            {                
            }

            if(type == Type_DownLoad)
            {
                string url = string.Empty;
                string video = string.Empty;
                if (args.Length == 3)
                {
                    url = args[1];
                    video = args[2];
                }
                else
                {
                    Console.WriteLine("请输入M3U8流地址:");
                    url = Console.ReadLine();
                    url = url.Trim();
                    if (!url.Equals("TEST"))
                    {
                        Console.WriteLine("请输入保存视频文件名:");
                        video = Console.ReadLine();
                        video = video.Trim();
                    }
                    else
                    {
                        // url = "http://lk-vod.lvb.eastmoney.com/4697b79evodcq1252033264/1c19f9625285890788896085733/playlist.m3u8?t=5eb58289&exper=0&us=0f10127396&sign=415428f9afe33ebb221e2b945bb1f24a";
                        // video = "LangKeTest1";

                        url = "http://cychengyuan-vod.48.cn/6742/20190428/cy/329395106997932032.m3u8";
                        video = "testVideo";
                    }

                }
                
                try
                {
                    ffmpeg.DownloadM3u8(url, video);
                }
                catch (Exception ex)
                {
                    File.AppendAllLines("Error.log", new List<string> { ex.StackTrace }, Encoding.UTF8);
                }
                Console.WriteLine("创建完成，按回车键退出");
                Console.ReadLine();
            }
            else if(type == Type_ParseJson)
            {
                string jsonList = string.Empty;
                string video = string.Empty;
                List<string> pathList = null;
                try
                {
                    jsonList = args[1];
                    Console.WriteLine(jsonList);
                    pathList = jsonList.Split(',').ToList();
                    video = args[2];
                }
                catch (Exception)
                {
                    pathList = new List<string>();
                }        
                ffmpeg.ParseVideoJson(pathList, video);
            }
            else
            {
                Console.WriteLine("命令不支持，请检查配置");
                Console.ReadLine();
            }
        }

        /*
        static void Main(string[] args)
        {
            CustomDownload.RegisterFFmpegBinaries();

            CustomDownload customDownload = new CustomDownload();
            customDownload.Init("ChangeResolution_1.m3u8");

            Console.ReadLine();
        }
        */
    }
}
