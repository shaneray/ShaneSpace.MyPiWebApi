using MMALSharp;
using MMALSharp.Common;
using MMALSharp.Handlers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ShaneSpace.MyPiWebApi.Models
{
    public class Camera
    {
        private readonly string BaseCaptureDirectory = "/var/lib/ShaneSpaceMyPiWebApi";
        private CancellationTokenSource _cancellationTokenSource;

        public async Task TakePictureAsync()
        {
            // Singleton initialized lazily. Reference once in your application.
            var cam = MMALCamera.Instance;

            using (var imgCaptureHandler = new ImageStreamCaptureHandler(Path.Combine(BaseCaptureDirectory, "pictures"), "jpg"))
            {
                await cam.TakePicture(imgCaptureHandler, MMALEncoding.JPEG, MMALEncoding.I420).ConfigureAwait(false);
            }

            // Cleanup disposes all unmanaged resources and unloads Broadcom library. To be called when no more processing is to be done
            // on the camera.
            //cam.Cleanup();
        }

        public async Task StartVideo()
        {
            if (_cancellationTokenSource != null)
            {
                return;
            }

            // Singleton initialized lazily. Reference once in your application.
            var cam = MMALCamera.Instance;

            using (var vidCaptureHandler = new VideoStreamCaptureHandler(Path.Combine(BaseCaptureDirectory, "videos"), "h264"))
            {
                _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                await cam.TakeVideo(vidCaptureHandler, _cancellationTokenSource.Token).ConfigureAwait(false);
            }

            // Cleanup disposes all unmanaged resources and unloads Broadcom library. To be called when no more processing is to be done
            // on the camera.
            //cam.Cleanup();
        }

        public void StopVideo()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = null;
        }
    }
}