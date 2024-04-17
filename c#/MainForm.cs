using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Http;
using System.IO;

namespace c_
{
    public class MainForm : Form
    {
        private Panel drawingPanel;
        private bool isDrawing = false;
        private Bitmap drawingBitmap;
        private Graphics drawingGraphics;
        private Point lastPoint;
        private Button btnSaveSend;

        public MainForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Width = 800;
            Height = 600;

            drawingPanel = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(760, 500),
                BorderStyle = BorderStyle.Fixed3D
            };
            Controls.Add(drawingPanel);

            btnSaveSend = new Button
            {
                Text = "Save & Send",
                Location = new Point(10, 520),
                Size = new Size(100, 30)
            };
            Controls.Add(btnSaveSend);
            btnSaveSend.Click += SaveAndSendImage;

            drawingBitmap = new Bitmap(drawingPanel.Width, drawingPanel.Height);
            drawingGraphics = Graphics.FromImage(drawingBitmap);
            drawingGraphics.Clear(Color.White);

            drawingPanel.MouseDown += StartDrawing;
            drawingPanel.MouseMove += Draw;
            drawingPanel.MouseUp += StopDrawing;
            drawingPanel.Paint += PanelPaint;
        }

        private void PanelPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(drawingBitmap, Point.Empty);
        }

        private void StartDrawing(object sender, MouseEventArgs e)
        {
            isDrawing = true;
            lastPoint = e.Location;
        }

        private void Draw(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                using (Pen pen = new Pen(Color.Black, 10))
                {
                    drawingGraphics.DrawLine(pen, lastPoint, e.Location);
                }
                drawingPanel.Invalidate();
                lastPoint = e.Location;
            }
        }

        private void StopDrawing(object sender, MouseEventArgs e)
        {
            isDrawing = false;
        }

        private async void SaveAndSendImage(object sender, EventArgs e)
        {
            string imagePath = "drawing.png";
            drawingBitmap.Save(imagePath);

            using (var client = new HttpClient())
            {
                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new ByteArrayContent(File.ReadAllBytes(imagePath));
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                    content.Add(fileContent, "file", "drawing.png");

                    var response = await client.PostAsync("http://localhost:5000/upload", content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    MessageBox.Show(responseString, "Response from Server");
                }
            }
        }
    }
}
