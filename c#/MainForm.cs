using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Imaging;
using AForge.Imaging.Filters;
using Python.Runtime;
using Keras.Models;
using Keras.PreProcessing.Image;

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
            using (Graphics g = Graphics.FromImage(drawingBitmap))
            {
                g.Clear(Color.White);
            }

            drawingGraphics = Graphics.FromImage(drawingBitmap);

            drawingPanel.MouseDown += StartDrawing;
            drawingPanel.MouseMove += Draw;
            drawingPanel.MouseUp += StopDrawing;
            drawingPanel.Paint += PanelPaint;

            var segmentButton = new Button
            {
                Text = "Segmentar Letras",
                Location = new Point(10, 520),
                Size = new Size(150, 30)
            };
            segmentButton.Click += SegmentLetters;
            Controls.Add(segmentButton);

            pictureBox = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(760, 500),
                BorderStyle = BorderStyle.Fixed3D
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

            BlobCounter blobCounter = new BlobCounter();
            blobCounter.ProcessImage(labeledImage);
            Blob[] blobs = blobCounter.GetObjectsInformation();

            int index = 0;
            foreach (Blob blob in blobs)
            {
                Rectangle rect = blob.Rectangle;
                Bitmap croppedImage = new Bitmap(rect.Width, rect.Height);
                Graphics croppedGraphics = Graphics.FromImage(croppedImage);
                croppedGraphics.DrawImage(drawingBitmap, 0, 0, rect, GraphicsUnit.Pixel);
                croppedGraphics.Dispose();

                string filename = $"segmented_letter_{index}.jpg";
                croppedImage.Save(filename);
                PreverLetra(filename);

                index++;
            }
            drawingPanel.Invalidate();
        }

        private void PreverLetra(string imageName)
        {
            using (Py.GIL()) 
            {
                dynamic sys = Py.Import("sys");
                sys.path.append(@"C:\Users\disrct\Desktop\DeepLearningChallenge\python");
                dynamic pythonScript = Py.Import("predict_letter");
                PyObject imageNamePy = new PyString(imageName);
                PyObject modelPathPy = new PyString("../models/GOD98_98.keras");
                PyObject result = pythonScript.InvokeMethod(
                    "predict_letter",
                    imageNamePy,
                    modelPathPy
                );
                MessageBox.Show($"Resultado: {result.ToString()}");
            }
        }
    }
}
