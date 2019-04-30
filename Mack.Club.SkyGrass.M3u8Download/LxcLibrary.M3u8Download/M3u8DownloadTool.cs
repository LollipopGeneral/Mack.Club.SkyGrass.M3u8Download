using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            RunBat(videoPath, $"{videoPath}_concat_to_mp4.bat");
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
            List<string> jsonFileList = new List<string>();
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
                string jsonFileName = item.Key.Replace("m3u8", "json");
                outline = $"ffprobe -v quiet -print_format json -show_format -show_streams {mp4FileName} > {jsonFileName} 2>&1";                
                files.Add(outline);
                mp4Files.Add($"file '{mp4FileName}'");
                mp4FileList.Add(mp4FileName);
                jsonFileList.Add(jsonFileName);
            }
            #endregion

            #region 写入文件 fileList.Txt
            string mp4_file_list_name = $"{videoPath}_mp4_file_list.txt";
            string mp4_file_list_path = Path.Combine(videoPath, mp4_file_list_name);
            File.WriteAllLines(mp4_file_list_path, mp4Files, new UTF8Encoding(false));
            #endregion

            #region 写入文件 批量m3u8.bat            
            // files.Add($"ffmpeg -f concat -i {mp4_file_list_name} -c copy -y -metadata comment=\"{VersionInfo}\" {videoPath}_output_concat.mp4");           
            #endregion

            #region 写入回调行信息
            string jsonListStr = string.Join(",", jsonFileList);
            string callbackLine = $"..\\LxcLibrary.M3u8Console.exe \"Type_ParseJson\" \"{jsonListStr}\" \"{videoPath}\"";
            files.Add(callbackLine);
            files.Add("pause");
            File.WriteAllLines(Path.Combine(videoPath, $"{videoPath}_concat_to_mp4.bat"), files, new UTF8Encoding(false));
            #endregion

            return mp4FileList;
        }

        private void RunBat(string targetDir, string fileName)
        {
            Process proc = null;
            try
            {
                proc = new Process();
                proc.StartInfo.WorkingDirectory = targetDir;
                proc.StartInfo.FileName = fileName;
                //proc.StartInfo.Arguments = string.Format("10");//this is argument
                //proc.StartInfo.CreateNoWindow = true;
                //proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;//这里设置DOS窗口不显示，经实践可行
                proc.Start();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString());
            }
        }

        public void ParseVideoJson(List<string> jsonFilePathList, string videoPath)
        {
            int max_widht = 0;
            int max_height = 0;
            Console.WriteLine($"jsonFilePathList.count: {jsonFilePathList.Count}");
            List<Mp4InfoModel> infoList = new List<Mp4InfoModel>();

            foreach (var jsonFilePath in jsonFilePathList)
            {
                if (!File.Exists(jsonFilePath))
                {
                    Console.WriteLine($"{jsonFilePath} 文件不存在");
                    continue;
                }
                string jsonInfo = File.ReadAllText(jsonFilePath, Encoding.UTF8);
                ShowStreamsModel model = JsonConvert.DeserializeObject<ShowStreamsModel>(jsonInfo);
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
                        Mp4InfoModel infoModel = new Mp4InfoModel {
                            name = jsonFilePath,
                            width = stream.width,
                            height = stream.height
                        };
                        infoList.Add(infoModel);
                    }
                }
            }
            Console.WriteLine($"{max_widht}, {max_height}");

            List<string> files = new List<string>();
            List<string> mp4Files = new List<string>();
            bool needChangeResolution = false;
            foreach (var info in infoList)
            {
                // w:h = aw:ah
                // ffmpeg -i ChangeResolution_1.mp4 -vf scale=-1:816,pad=816:816:173:0:black -vcodec h264 ChangeResolution_1_v5.mp4
                if (info.width == max_widht && info.height == max_height)
                {                    
                }
                else
                {
                    needChangeResolution = true;
                    string mp4Name = info.name.Replace("json", "mp4");
                    bool wOrH = info.width >= info.height;
                    string scaleStr = wOrH ? $"{max_widht}:-1" : $"-1:{max_height}";
                    int auto = wOrH ? (int)Math.Ceiling((info.height * max_widht) / info.width * 1.0) : (int)Math.Ceiling((info.width * max_height) / info.height * 1.0);
                    int dx = wOrH ? (int)Math.Ceiling((max_height - auto) / 2.0) : (int)Math.Ceiling((max_widht - auto) / 2.0);
                    string padStr = wOrH ? $"0:{dx}" : $"{dx}:0";
                    string line = $"ffmpeg -i {mp4Name} -vf scale={scaleStr},pad={max_widht}:{max_height}:{padStr}:black -vcodec h264 WH_{mp4Name}";
                    files.Add(line);
                    mp4Files.Add($"file 'WH_{mp4Name}'");
                }
            }
            Console.WriteLine($"是否需要转换分辨率:{needChangeResolution}");
            string mp4_file_list_name = $"{videoPath}_mp4_file_list.txt";
            if (needChangeResolution)
            {
                #region 写入文件 fileList.Txt                
                string mp4_file_list_path = mp4_file_list_name;
                File.WriteAllLines(mp4_file_list_path, mp4Files, new UTF8Encoding(false));
                #endregion

                #region 写入文件 自动转换分辨率.bat
                files.Add($"ffmpeg -f concat -i {mp4_file_list_name} -c copy -y -metadata comment=\"{VersionInfo}\" WH_{videoPath}_output_concat.mp4");
                #endregion
            }
            else
            {
                files.Add($"ffmpeg -f concat -i {mp4_file_list_name} -c copy -y -metadata comment=\"{VersionInfo}\" {videoPath}_output_concat.mp4");
            }

            files.Add("pause");
            File.WriteAllLines($"{videoPath}_concat_to_mp4.bat", files, new UTF8Encoding(false));

            RunBat(videoPath, $"{videoPath}_concat_to_mp4.bat");
        }
        
    }
}