using System.Drawing.Imaging;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace c_
{
    public class MainForm : Form
    {
        private PictureBox drawingPictureBox;
        private List<List<Point>> allDrawingPoints = new List<List<Point>>();
        private List<Point> currentDrawingPoints = new List<Point>();
        private bool isDrawing = false;
        private Label lblRecognizedLetter;
        private HttpClient client = new HttpClient();

        public MainForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Width = 800;
            Height = 600;

            drawingPictureBox = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(760, 500),
                BackColor = Color.White,
                Cursor = Cursors.Cross,
                BorderStyle = BorderStyle.Fixed3D
            };
            drawingPictureBox.MouseDown += DrawingPictureBox_MouseDown;
            drawingPictureBox.MouseMove += DrawingPictureBox_MouseMove;
            drawingPictureBox.MouseUp += DrawingPictureBox_MouseUp;
            drawingPictureBox.Paint += DrawingPictureBox_Paint;
            Controls.Add(drawingPictureBox);

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

        private void DrawingPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            isDrawing = true;
            currentDrawingPoints = new List<Point>();
            currentDrawingPoints.Add(e.Location);
        }

        private void DrawingPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                currentDrawingPoints.Add(e.Location);
                drawingPictureBox.Invalidate();
            }
        }

        private async void DrawingPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            isDrawing = false;
            allDrawingPoints.Add(new List<Point>(currentDrawingPoints));
            drawingPictureBox.Invalidate();

            if (currentDrawingPoints.Count > 1)
            {
                using (Bitmap bmp = new Bitmap(drawingPictureBox.Width, drawingPictureBox.Height))
                {
                    drawingPictureBox.DrawToBitmap(bmp, drawingPictureBox.ClientRectangle);
                    using (var stream = new MemoryStream())
                    {
                        bmp.Save(stream, ImageFormat.Jpeg);
                        var imageData = stream.ToArray();

                        await Task.Run(async () =>
                        {
                            try
                            {
                                using (var fileContent = new ByteArrayContent(imageData))
                                {
                                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                                    using (var content = new MultipartFormDataContent())
                                    {
                                        content.Add(fileContent, "file", "drawing.jpg");

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
                        });
                    }
                }
            }
        }

        private void DrawingPictureBox_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Color.Black, 6))
            {
                pen.StartCap = pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                foreach (var points in allDrawingPoints)
                {
                    if (points.Count > 1)
                    {
                        for (int i = 1; i < points.Count; i++)
                        {
                            e.Graphics.DrawLine(pen, points[i - 1], points[i]);
                        }
                    }
                }

                if (isDrawing && currentDrawingPoints.Count > 1)
                {
                    for (int i = 1; i < currentDrawingPoints.Count; i++)
                    {
                        e.Graphics.DrawLine(pen, currentDrawingPoints[i - 1], currentDrawingPoints[i]);
                    }
                }
            }
        }
    }
}
