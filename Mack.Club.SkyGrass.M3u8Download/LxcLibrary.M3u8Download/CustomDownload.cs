using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace LxcLibrary.M3u8Download
{    
    public class CustomDownload
    {
        [DllImport("kernel32", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        public static void RegisterFFmpegBinaries()
        {
            Console.WriteLine("Current directory: " + Environment.CurrentDirectory);
            Console.WriteLine("Runnung in {0}-bit mode.", Environment.Is64BitProcess ? "64" : "32");

            var current = Environment.CurrentDirectory;
            var probe = Path.Combine("FFmpeg", "bin", Environment.Is64BitProcess ? "x64" : "x86");
            while (current != null)
            {
                var ffmpegDirectory = Path.Combine(current, probe);
                if (Directory.Exists(ffmpegDirectory))
                {
                    Console.WriteLine($"FFmpeg binaries found in: {ffmpegDirectory}");
                    SetDllDirectory(ffmpegDirectory);
                    return;
                }
                current = Directory.GetParent(current)?.FullName;
            }            
        }

        public unsafe void Init(string url = "")
        {
            #region ffmpeg 初始化
            
            // 初始化注册ffmpeg相关的编码器
            //ffmpeg.av_register_all();
            //ffmpeg.avcodec_register_all();
            //ffmpeg.avformat_network_init();
            #endregion

            Console.WriteLine($"FFmpeg version info: {ffmpeg.av_version_info()}");

            #region ffmpeg 转码


            // 分配音视频格式上下文
            var pFormatContext = ffmpeg.avformat_alloc_context();

            int error;

            //打开流
            error = ffmpeg.avformat_open_input(&pFormatContext, url, null, null);
            if (error != 0) throw new ApplicationException(GetErrorMessage(error));

            // 读取媒体流信息
            error = ffmpeg.avformat_find_stream_info(pFormatContext, null);
            if (error != 0) throw new ApplicationException(GetErrorMessage(error));

            // 这里只是为了打印些视频参数
            AVDictionaryEntry* tag = null;
            while ((tag = ffmpeg.av_dict_get(pFormatContext->metadata, "", tag, ffmpeg.AV_DICT_IGNORE_SUFFIX)) != null)
            {
                var key = Marshal.PtrToStringAnsi((IntPtr)tag->key);
                var value = Marshal.PtrToStringAnsi((IntPtr)tag->value);
                Console.WriteLine($"{key} = {value}");
            }

            // 从格式化上下文获取流索引
            AVStream* pStream = null, aStream;
            for (var i = 0; i < pFormatContext->nb_streams; i++)
            {
                if (pFormatContext->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    pStream = pFormatContext->streams[i];

                }
                else if (pFormatContext->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    aStream = pFormatContext->streams[i];

                }
            }
            if (pStream == null) throw new ApplicationException(@"Could not found video stream.");

            // 获取流的编码器上下文
            var codecContext = *pStream->codec;

            Console.WriteLine($"codec name: {ffmpeg.avcodec_get_name(codecContext.codec_id)}");
            // 获取图像的宽、高及像素格式
            var width = codecContext.width;
            var height = codecContext.height;
            var sourcePixFmt = codecContext.pix_fmt;

            // 得到编码器ID
            var codecId = codecContext.codec_id;
            // 目标像素格式
            var destinationPixFmt = AVPixelFormat.AV_PIX_FMT_BGR24;


            // 某些264格式codecContext.pix_fmt获取到的格式是AV_PIX_FMT_NONE 统一都认为是YUV420P
            if (sourcePixFmt == AVPixelFormat.AV_PIX_FMT_NONE && codecId == AVCodecID.AV_CODEC_ID_H264)
            {
                sourcePixFmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
            }

            // 得到SwsContext对象：用于图像的缩放和转换操作
            var pConvertContext = ffmpeg.sws_getContext(width, height, sourcePixFmt,
                width, height, destinationPixFmt,
                ffmpeg.SWS_FAST_BILINEAR, null, null, null);
            if (pConvertContext == null) throw new ApplicationException(@"Could not initialize the conversion context.");

            //分配一个默认的帧对象:AVFrame
            var pConvertedFrame = ffmpeg.av_frame_alloc();
            // 目标媒体格式需要的字节长度
            var convertedFrameBufferSize = ffmpeg.av_image_get_buffer_size(destinationPixFmt, width, height, 1);
            // 分配目标媒体格式内存使用
            var convertedFrameBufferPtr = Marshal.AllocHGlobal(convertedFrameBufferSize);
            var dstData = new byte_ptrArray4();
            var dstLinesize = new int_array4();
            // 设置图像填充参数
            ffmpeg.av_image_fill_arrays(ref dstData, ref dstLinesize, (byte*)convertedFrameBufferPtr, destinationPixFmt, width, height, 1);

            #endregion
        }

        private unsafe string GetErrorMessage(int error)
        {
            var bufferSize = 1024;
            var buffer = stackalloc byte[bufferSize];
            ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
            var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
            return message;
        }

    }
}