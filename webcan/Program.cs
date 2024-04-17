class Program
{
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var form = new Game();
        form.Text = "Jogo";
        form.StartPosition = FormStartPosition.CenterScreen;

        var pictureBox = new PictureBox();
        pictureBox.Dock = DockStyle.Fill;
        pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

        var camera = new Camera(pictureBox);
        form.Controls.Add(pictureBox); 

        form.ShowDialog();
    }
}
