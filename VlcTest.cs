using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace VlcTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Core.Initialize();
            using var libvlc = new LibVLC();
            using var media = new Media(libvlc, args[0], FromType.FromPath);
            await media.Parse(MediaParseOptions.ParseLocal);
            
            Console.WriteLine($"Duration: {media.Duration}");
            
            var videoTrack = media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video);
            if (videoTrack.TrackType == TrackType.Video)
            {
                Console.WriteLine($"Resolution: {videoTrack.Data.Video.Width}x{videoTrack.Data.Video.Height}");
                Console.WriteLine($"FPS: {videoTrack.Data.Video.FrameRateNum / (double)videoTrack.Data.Video.FrameRateDen}");
            }
        }
    }
}
