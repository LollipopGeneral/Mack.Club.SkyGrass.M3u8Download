using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace LxcLibrary.M3u8Download
{
    public class M3u8DownloadTool
    {
        public const string VersionInfo = "Create By LxcLibrary.M3u8Console v1.0.0";
        public const string PointLine = "#EXT-X-DISCONTINUITY";
        public const string EndPointLine = "#EXT-X-ENDLIST";
        public const string FlagLine = "#EXTINF";

        public void DownloadM3u8(string m3u8FilePath, string videoPath)
        {
            WebClient wc = new WebClient();
            if (!Directory.Exists(videoPath))
            {
                Directory.CreateDirectory(videoPath);
            }
            string fileName = $"{videoPath}.m3u8";
            string localFilePath = Path.Combine(videoPath, fileName);
            try
            {
                wc.DownloadFile(m3u8FilePath, localFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }

            if (!File.Exists(localFilePath))
            {
                Console.WriteLine("文件下载失败");
                return;
            }

            Uri uri = new Uri(m3u8FilePath);
            string host = $"{uri.Scheme}://{uri.Host}";

            List<string> m3u8Files = ReadFileAndParse(localFilePath, videoPath, host);
        }

        private List<string> ReadFileAndParse(string localFilePath, string videoPath, string host)
        {
            string[] lines = File.ReadAllLines(localFilePath, new UTF8Encoding(false));
            List<string> headLines = new List<string>();
            int index = 0;

            #region 解析数据
            Dictionary<string, List<string>> dic = new Dictionary<string, List<string>>();
            foreach (var line in lines)
            {
                if (line.Equals(EndPointLine))
                {
                    continue;
                }

                if (line.Equals(PointLine))
                {
                    index++;
                    continue;
                }

                if (index == 0)
                {
                    headLines.Add(line);
                }
                else
                {
                    string noFlagLine = line;
                    if (!line.Contains(FlagLine))
                    {
                        noFlagLine = $"{host}{line}";
                    }

                    string fileNameKey = $"{videoPath}_{index}.m3u8";
                    List<string> fileTempList = null;
                    if (!dic.ContainsKey(fileNameKey))
                    {
                        fileTempList = new List<string>();
                        fileTempList.Add(noFlagLine);
                        dic[fileNameKey] = fileTempList;
                    }
                    else
                    {
                        fileTempList = dic[fileNameKey];
                        fileTempList.Add(noFlagLine);
                        dic[fileNameKey] = fileTempList;
                    }
                }
            }
            #endregion

            #region 写入文件 
            List<string> files = new List<string>();
            files.Add("copy ..\\ffmpeg.exe .\\");

            List<string> mp4Files = new List<string>();

            foreach (var item in dic)
            {
                string fileName = Path.Combine(videoPath, item.Key);
                List<string> fileLines = new List<string>();
                fileLines = fileLines.Concat(headLines).ToList();
                fileLines.Add(PointLine);
                fileLines = fileLines.Concat(item.Value).ToList();
                fileLines.Add(EndPointLine);
                File.WriteAllLines(fileName, fileLines, new UTF8Encoding(false));
                string outline = $"ffmpeg -protocol_whitelist file,tcp,http -i {item.Key} -c copy -y {item.Key.Replace("m3u8", "mp4")}";
                files.Add(outline);
                mp4Files.Add($"file '{item.Key.Replace("m3u8", "mp4")}'");
            }
            #endregion

            #region 写入文件 fileList.Txt
            string mp4_file_list_name = $"{videoPath}_mp4_file_list.txt";
            string mp4_file_list_path = Path.Combine(videoPath, mp4_file_list_name);
            File.WriteAllLines(mp4_file_list_path, mp4Files, new UTF8Encoding(false));
            #endregion

            #region 写入文件 批量m3u8.bat            
            files.Add($"ffmpeg -f concat -i {mp4_file_list_name} -c copy -y -metadata comment=\"{VersionInfo}\" {videoPath}_output_concat.mp4");
            files.Add("pause");
            File.WriteAllLines(Path.Combine(videoPath, $"{videoPath}_concat_to_mp4.bat"), files, new UTF8Encoding(false));
            #endregion

            return files;
        }
    }
}