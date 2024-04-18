using System.Drawing.Imaging;
using System.Net.Http.Headers;
using AForge.Video;
using AForge.Video.DirectShow;
using Newtonsoft.Json;

namespace c_
{
    public class MainForm : Form
    {
        private PictureBox webcamPictureBox;
        private Button captureButton;
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private Label lblRecognizedLetter;
        private HttpClient client = new HttpClient();

        public MainForm()
        {
            InitializeComponents();
            InitializeWebcam();
        }

        private void InitializeComponents()
        {
            Width = 800;
            Height = 600;

            webcamPictureBox = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(760, 500),
                BackColor = Color.Black,
                BorderStyle = BorderStyle.Fixed3D
            };
            Controls.Add(webcamPictureBox);

            captureButton = new Button
            {
                Text = "Capturar Foto",
                Location = new Point(10, 520),
                Size = new Size(100, 30)
            };
            captureButton.Click += CaptureButton_Click;
            Controls.Add(captureButton);

            lblRecognizedLetter = new Label
            {
                Location = new Point(250, 520),
                Size = new Size(300, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(lblRecognizedLetter);
        }

        private void InitializeWebcam()
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count == 0)
            {
                MessageBox.Show("Nenhuma webcam encontrada.");
                return;
            }

            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            videoSource.NewFrame += VideoSource_NewFrame;
            videoSource.Start();
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            webcamPictureBox.Image = (Bitmap)eventArgs.Frame.Clone();
        }

        private async void CaptureButton_Click(object sender, EventArgs e)
        {
            if (webcamPictureBox.Image == null)
            {
                MessageBox.Show("Nenhuma imagem para capturar.");
                return;
            }

            using (var stream = new MemoryStream())
            {
                webcamPictureBox.Image.Save(stream, ImageFormat.Jpeg);
                var imageData = stream.ToArray();

                try
                {
                    using (var fileContent = new ByteArrayContent(imageData))
                    {
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                        using (var content = new MultipartFormDataContent())
                        {
                            content.Add(fileContent, "file", "webcam.jpg");

                            var response = await client.PostAsync("http://localhost:5000/upload", content);
                            var responseString = await response.Content.ReadAsStringAsync();
                            var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseString);
                            var phrasesList = jsonResponse?.phrases?.ToObject<List<string>>() ?? new List<string>();
                            var phrases = string.Join(" ", phrasesList);

                            lblRecognizedLetter.Invoke((MethodInvoker)delegate
                            {
                                if (phrases.Length > 1)
                                {
                                    lblRecognizedLetter.Text = $"Letra Reconhecida: {phrases.Substring(1)}";
                                }
                                else
                                {
                                    lblRecognizedLetter.Text = "Nenhuma letra reconhecida.";
                                }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    lblRecognizedLetter.Invoke((MethodInvoker)delegate
                    {
                        lblRecognizedLetter.Text = $"Erro ao processar: {ex.Message}";
                    });
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (videoSource != null && videoSource.IsRunning)
                videoSource.Stop();
        }
    }
}
