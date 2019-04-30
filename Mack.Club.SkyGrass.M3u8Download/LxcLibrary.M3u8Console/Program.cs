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
        static void Main(string[] args)
        {
            string url = string.Empty;
            string video = string.Empty;
            if (args.Length == 2)
            {
                url = args[0];
                video = args[1];
            }
            else
            {
                Console.WriteLine("请输入M3U8流地址:");
                url = Console.ReadLine();
                url = url.Trim();
                if(!url.Equals("TEST"))
                {
                    Console.WriteLine("请输入保存视频文件名:");
                    video = Console.ReadLine();
                    video = video.Trim();
                }
                else
                {
                    url = "http://cychengyuan-vod.48.cn/6742/20190428/cy/329395106997932032.m3u8";
                    video = "testVideo";
                }
                
            }

            M3u8DownloadTool ffmpeg = new M3u8DownloadTool();
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
