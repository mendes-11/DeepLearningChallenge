using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Windows.Forms;

public partial class Game : Form
{
    private Dictionary<string, string> palavrasESons;
    private IEnumerator<KeyValuePair<string, string>> enumerator;
    private SoundPlayer player;

    private Label lblPalavra;
    private TextBox txtResposta;
    private Button btnResponder;
     private PictureBox pictureBox;
    private Camera camera;

    public Game()
    {
        InitializeComponent();
        IniciarJogo();

        this.FormClosing += Game_FormClosing;

        camera = new Camera(pictureBox);
    }

    private void IniciarJogo()
    {
        palavrasESons = new Dictionary<string, string>();
        palavrasESons.Add("zap", @"sound\zap.wav");

        enumerator = palavrasESons.GetEnumerator();

        player = new SoundPlayer();

        ProximaPalavra();
    }

    private void Game_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (camera != null)
        {
            camera.Stop();
        }
    }

    private void ProximaPalavra()
    {
        if (enumerator.MoveNext())
        {
            KeyValuePair<string, string> palavraESom = enumerator.Current;

            lblPalavra.Text = "Soletra a palavra:";
            txtResposta.Text = "";
            txtResposta.Focus();

            ReproduzirSom(palavraESom.Value);
        }
        else
        {
            MessageBox.Show("Fim do jogo!");
            this.Close();
        }
    }

    private void ReproduzirSom(string caminhoDoSom)
    {
        try
        {
            player.SoundLocation = caminhoDoSom;
            player.Load();
            player.Play();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao reproduzir o som: " + ex.Message);
        }
    }

    private void InitializeComponent()
    {
        this.lblPalavra = new Label();
        this.txtResposta = new TextBox();
        this.btnResponder = new Button();
        this.SuspendLayout();

        this.lblPalavra.AutoSize = true;
        this.lblPalavra.Location = new System.Drawing.Point(12, 9);
        this.lblPalavra.Name = "lblPalavra";
        this.lblPalavra.Size = new System.Drawing.Size(91, 13);
        this.lblPalavra.TabIndex = 0;
        this.lblPalavra.Text = "Soletra a palavra:";

        this.txtResposta.Location = new System.Drawing.Point(12, 33);
        this.txtResposta.Name = "txtResposta";
        this.txtResposta.Size = new System.Drawing.Size(100, 20);
        this.txtResposta.TabIndex = 1;

        this.btnResponder.Location = new System.Drawing.Point(12, 59);
        this.btnResponder.Name = "btnResponder";
        this.btnResponder.Size = new System.Drawing.Size(75, 23);
        this.btnResponder.TabIndex = 2;
        this.btnResponder.Text = "Responder";
        this.btnResponder.UseVisualStyleBackColor = true;
        this.btnResponder.Click += new System.EventHandler(this.btnResponder_Click);

        this.ClientSize = new System.Drawing.Size(284, 261);
        this.Controls.Add(this.btnResponder);
        this.Controls.Add(this.txtResposta);
        this.Controls.Add(this.lblPalavra);
        this.Name = "Game";
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private void btnResponder_Click(object sender, EventArgs e)
    {
        string resposta = txtResposta.Text.Trim();

        string palavraCorreta = enumerator.Current.Key;

        if (resposta.ToLower() == palavraCorreta.ToLower())
        {
            MessageBox.Show("Correto!");
        }
        else
        {
            MessageBox.Show("Incorreto! A palavra correta era: " + palavraCorreta);
        }

        ProximaPalavra();
    }
}
