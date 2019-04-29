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

                Console.WriteLine("请输入保存视频文件名:");
                video = Console.ReadLine();
                video = video.Trim();
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
    }
}
