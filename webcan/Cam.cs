// using System;
// using System.Drawing;
// using System.Windows.Forms;
// using AForge.Video;
// using AForge.Video.DirectShow;

// class Program
// {
//     static void Main(string[] args)
//     {
//         Application.EnableVisualStyles();
//         Application.SetCompatibleTextRenderingDefault(false);

//         // Inicializa o formulário
//         var form = new Form();
//         form.Text = "Visualização da Webcam";
//         form.StartPosition = FormStartPosition.CenterScreen;
//         form.FormClosing += (sender, eventArgs) => { Application.Exit(); };

//         var pictureBox = new PictureBox();
//         pictureBox.Dock = DockStyle.Fill;
//         pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
//         form.Controls.Add(pictureBox);

//         FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

//         if (videoDevices.Count == 0)
//         {
//             Console.WriteLine("Nenhuma câmera encontrada.");
//             return;
//         }

//         FilterInfo videoDevice = videoDevices[0];
//         VideoCaptureDevice videoSource = new VideoCaptureDevice(videoDevice.MonikerString);

//         videoSource.NewFrame += (sender, eventArgs) =>
//         {
//             Bitmap frame = (Bitmap)eventArgs.Frame.Clone();
//             pictureBox.Image = frame;
//         };

//         videoSource.Start();

//         Application.Run(form);

//         videoSource.SignalToStop();
//         videoSource.WaitForStop();
//     }
// }



// using System;
// using System.Media;
// using System.Threading;

// string caminhoDoArquivo = @"sound\zap.wav";


// SoundPlayer player = new SoundPlayer(caminhoDoArquivo);

// player.Play();
// Thread.Sleep(1500);

using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
class Camera
{
    private PictureBox pictureBox;
    private VideoCaptureDevice videoSource;

    public Camera(PictureBox pictureBox)
    {
        this.pictureBox = pictureBox;

        FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

        if (videoDevices.Count == 0)
        {
            MessageBox.Show("Nenhuma câmera encontrada.");
            return;
        }

        FilterInfo videoDevice = videoDevices[0];
        videoSource = new VideoCaptureDevice(videoDevice.MonikerString);

        videoSource.NewFrame += (sender, eventArgs) =>
        {
            Bitmap frame = (Bitmap)eventArgs.Frame.Clone();
            pictureBox.Image = frame;
        };

        videoSource.Start();
    }

    public void Stop()
    {
        if (videoSource != null && videoSource.IsRunning)
        {
            videoSource.SignalToStop();
            videoSource.WaitForStop();
        }
    }
}
