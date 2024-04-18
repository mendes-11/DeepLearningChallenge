using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace c_
{
    public class MainForm : Form
    {
        private PictureBox drawingPictureBox;
        private List<List<Point>> letters = new List<List<Point>>();
        private List<Point> currentDrawingPoints = new List<Point>();
        private Stack<List<List<Point>>> undoStack = new Stack<List<List<Point>>>();
        private Stack<List<List<Point>>> redoStack = new Stack<List<List<Point>>>();
        private bool isDrawing = false;
        private Label lblRecognizedLetter;
        private HttpClient client = new HttpClient();
        private Color drawingColor = Color.Black;
        private int brushSize = 6;
        private int zoomFactor = 100;

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
            drawingPictureBox.MouseWheel += DrawingPictureBox_MouseWheel;
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

            Button btnClear = new Button
            {
                Text = "Limpar",
                Location = new Point(300, 560),
                Size = new Size(100, 30)
            };
            btnClear.Click += BtnClear_Click;
            Controls.Add(btnClear);

            ColorDialog colorDialog = new ColorDialog();

            Button btnSelectColor = new Button
            {
                Text = "Selecionar Cor Escura",
                Location = new Point(10, 560),
                Size = new Size(150, 30)
            };
            btnSelectColor.Click += (sender, e) =>
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    if (IsColorDark(colorDialog.Color))
                    {
                        drawingColor = colorDialog.Color;
                    }
                    else
                    {
                        MessageBox.Show("Por favor, selecione uma cor escura.");
                    }
                }
            };
            Controls.Add(btnSelectColor);

            TrackBar trackBarBrushSize = new TrackBar
            {
                Minimum = 1,
                Maximum = 20,
                Value = 6,
                Location = new Point(180, 560),
                Size = new Size(100, 30)
            };
            trackBarBrushSize.ValueChanged += (sender, e) =>
            {
                brushSize = trackBarBrushSize.Value;
            };
            Controls.Add(trackBarBrushSize);

            Button btnUndo = new Button
            {
                Text = "Desfazer",
                Location = new Point(400, 560),
                Size = new Size(100, 30)
            };
            btnUndo.Click += BtnUndo_Click;
            Controls.Add(btnUndo);

            Button btnRedo = new Button
            {
                Text = "Refazer",
                Location = new Point(500, 560),
                Size = new Size(100, 30)
            };
            btnRedo.Click += BtnRedo_Click;
            Controls.Add(btnRedo);

            Button btnZoomIn = new Button
            {
                Text = "+",
                Location = new Point(610, 560),
                Size = new Size(30, 30)
            };
            btnZoomIn.Click += (sender, e) =>
            {
                ZoomIn();
            };
            Controls.Add(btnZoomIn);

            Button btnZoomOut = new Button
            {
                Text = "-",
                Location = new Point(650, 560),
                Size = new Size(30, 30)
            };
            btnZoomOut.Click += (sender, e) =>
            {
                ZoomOut();
            };
            Controls.Add(btnZoomOut);
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
            letters.Add(new List<Point>(currentDrawingPoints));
            undoStack.Push(new List<List<Point>>(letters));
            redoStack.Clear();
            drawingPictureBox.Invalidate();

            if (currentDrawingPoints.Count > 1)
            {
                await RecognizeLetter();
            }

            currentDrawingPoints.Clear();
        }

        private void DrawingPictureBox_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(drawingColor, brushSize))
            {
                pen.StartCap = pen.EndCap = LineCap.Round;

                foreach (var letterPoints in letters)
                {
                    if (letterPoints.Count > 1)
                    {
                        for (int i = 1; i < letterPoints.Count; i++)
                        {
                            e.Graphics.DrawLine(pen, letterPoints[i - 1], letterPoints[i]);
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

        private void BtnClear_Click(object sender, EventArgs e)
        {
            ClearCanvas();
        }

        private void ClearCanvas()
        {
            letters.Clear();
            undoStack.Clear();
            redoStack.Clear();
            drawingPictureBox.Invalidate();
        }

        private void BtnUndo_Click(object sender, EventArgs e)
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(new List<List<Point>>(letters));
                letters = new List<List<Point>>(undoStack.Pop());
                drawingPictureBox.Invalidate();
            }
        }

        private void BtnRedo_Click(object sender, EventArgs e)
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push(new List<List<Point>>(letters));
                letters = new List<List<Point>>(redoStack.Pop());
                drawingPictureBox.Invalidate();
            }
        }

        private async Task RecognizeLetter()
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

        private void DrawingPictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                ZoomIn();
            }
            else
            {
                ZoomOut();
            }
        }

        private void ZoomIn()
        {
            if (zoomFactor < 200)
            {
                zoomFactor += 10;
                drawingPictureBox.Width = (int)(800 * zoomFactor / 100.0);
                drawingPictureBox.Height = (int)(600 * zoomFactor / 100.0);
                drawingPictureBox.Invalidate();
            }
        }

        private void ZoomOut()
        {
            if (zoomFactor > 20)
            {
                zoomFactor -= 10;
                drawingPictureBox.Width = (int)(800 * zoomFactor / 100.0);
                drawingPictureBox.Height = (int)(600 * zoomFactor / 100.0);
                drawingPictureBox.Invalidate();
            }
        }

        private bool IsColorDark(Color color)
        {
            double luminance = (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255;
            double darkColorThreshold = 0.4;
            return luminance < darkColorThreshold;
        }
    }
}
