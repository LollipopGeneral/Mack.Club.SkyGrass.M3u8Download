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

            List<string> mp4FileList = ReadFileAndParse(localFilePath, videoPath, host);
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
            List<string> mp4FileList = new List<string>();
            List<string> files = new List<string>();
            files.Add("copy ..\\ffmpeg.exe .\\");
            files.Add("copy ..\\ffprobe.exe .\\");

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
                string mp4FileName = item.Key.Replace("m3u8", "mp4");
                string outline = $"ffmpeg -protocol_whitelist file,tcp,http -i {item.Key} -c copy -y {mp4FileName}";
                files.Add(outline);
                outline = $"ffprobe -v quiet -print_format json -show_format -show_streams {mp4FileName} > {mp4FileName.Replace("mp4", "json")} 2>&1";
                files.Add(outline);
                mp4Files.Add($"file '{mp4FileName}'");
                mp4FileList.Add(mp4FileName);
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

            return mp4FileList;
        }

        public void ParseVideoWidthAndHeight(List<string> jsonFilePathList, string videoPath)
        {
            int max_widht = 0;
            int max_height = 0;

            foreach (var jsonFilePath in jsonFilePathList)
            {
                string jsonInfo = File.ReadAllText(jsonFilePath, Encoding.UTF8);
                ShowStreamsModel model = new ShowStreamsModel();
                foreach (var stream in model.streams)
                {
                    if(stream.codec_type == "video")
                    {
                        if(stream.coded_width >= max_widht)
                        {
                            max_widht = stream.coded_width;
                        }
                        if (stream.coded_height >= max_height)
                        {
                            max_height = stream.coded_height;
                        }
                    }
                }
            }

            #region 写入文件 合并批量m3u8.bat   
            /*如果视频原始1920x800的话，完整的语法应该是：
            -vf 'scale=1280:534,pad=1280:720:0:93:black'*/

            // ffmpeg -i ChangeResolution_1.mp4 -vf pad=816:816:0:0:black -vcodec h264 ChangeResolution_1_v2.mp4

            List<string> files = new List<string>();
            string mp4_file_list_name = $"{videoPath}_mp4_file_list.txt";
            files.Add($"ffmpeg -f concat -i {mp4_file_list_name} -c copy -y -metadata comment=\"{VersionInfo}\" {videoPath}_output_concat.mp4");
            files.Add("pause");
            File.WriteAllLines(Path.Combine(videoPath, $"{videoPath}_concat_to_mp4.bat"), files, new UTF8Encoding(false));
            #endregion
        }
    }
}