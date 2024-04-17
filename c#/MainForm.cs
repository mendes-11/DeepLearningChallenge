using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Imaging;
using AForge.Imaging.Filters;
using Python.Runtime;
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
        private PictureBox pictureBox;
        private dynamic model;

        public MainForm()
        {
            InitializeComponents();
            InitializePython();
        }

        private void InitializePython()
        {
            Runtime.PythonDLL = "python311.dll";
            PythonEngine.Initialize();
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

            drawingBitmap = new Bitmap(drawingPanel.Width, drawingPanel.Height);
            drawingGraphics = Graphics.FromImage(drawingBitmap);
            drawingGraphics.Clear(Color.White);

            drawingPanel.MouseDown += StartDrawing;
            drawingPanel.MouseMove += Draw;
            drawingPanel.MouseUp += StopDrawing;
            drawingPanel.Paint += PanelPaint;

            var segmentButton = new Button
            {
                Text = "Segmentar e Identificar Frases",
                Location = new Point(10, 520),
                Size = new Size(200, 30)
            };
            segmentButton.Click += SegmentLetters;
            Controls.Add(segmentButton);

            pictureBox = new PictureBox
            {
                Location = new Point(220, 520),
                Size = new Size(550, 50),
                BorderStyle = BorderStyle.FixedSingle,
                AutoSize = true
            };
            Controls.Add(pictureBox);
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
                using (Pen pen = new Pen(Color.Black, 4))
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

        private void SegmentLetters(object sender, EventArgs e)
        {
            Bitmap grayImage = Grayscale.CommonAlgorithms.BT709.Apply(drawingBitmap);

            Threshold thresholdFilter = new Threshold(127);
            Bitmap binaryImage = thresholdFilter.Apply(grayImage);

            Invert invertFilter = new Invert();
            invertFilter.ApplyInPlace(binaryImage);

            ConnectedComponentsLabeling labeling = new ConnectedComponentsLabeling();
            Bitmap labeledImage = labeling.Apply(binaryImage);

            BlobCounter blobCounter = new BlobCounter
            {
                ObjectsOrder = ObjectsOrder.XY
            };
            blobCounter.ProcessImage(labeledImage);
            Blob[] blobs = blobCounter.GetObjectsInformation();

            string baseFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DeepLearningChallenge", "python");
            for (int index = 0; index < blobs.Length; index++)
            {
                Blob blob = blobs[index];
                Rectangle rect = blob.Rectangle;
                Bitmap croppedImage = new Bitmap(rect.Width, rect.Height);
                using (Graphics croppedGraphics = Graphics.FromImage(croppedImage))
                {
                    croppedGraphics.DrawImage(drawingBitmap, 0, 0, rect, GraphicsUnit.Pixel);
                }

                string filename = $"segmented_letter_{index}.jpg";
                string fullPath = Path.Combine(baseFolderPath, filename);
                croppedImage.Save(fullPath);

                string letter = PreverLetra(fullPath);
                MessageBox.Show($"Letter identified: {letter}");
            }

            drawingPanel.Invalidate();
        }

        private string PreverLetra(string imageName)
        {
            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                string scriptPath = @"C:\Users\MateusLeite\Desktop\DeepLearningChallenge\python";
                string modelPath = @"C:\Users\MateusLeite\Desktop\DeepLearningChallenge\models\GOD98_98.keras";
                sys.path.append(scriptPath);
                dynamic pythonScript = Py.Import("predict_letter");
                PyObject imageNamePy = new PyString(imageName);
                PyObject modelPathPy = new PyString(modelPath);
                if (!File.Exists(modelPath))
                {
                    MessageBox.Show($"Model file not found: {modelPath}");
                    return null;
                }
                PyObject result = pythonScript.InvokeMethod("predict_letter", imageNamePy, modelPathPy);
                return result.ToString();
            }
        }

    }
}
