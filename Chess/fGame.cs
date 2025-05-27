using System;
using System.Windows.Forms;
using GameModel;

namespace Chess
{
    public partial class fGame : Form
    {
        private readonly Menu menuForm;

        public fGame(Menu mForm, IGameModeStrategy strategy)
        {
            InitializeComponent();
            Game game = Game.Instance;
            menuForm = mForm;
            game.StartGame(Controls, strategy);
        }


        private void Form1_Click(object sender, EventArgs e)
        {
            PieceSelection.Instance.Deselect();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Game.Instance.EndGame();
            this.Dispose();
            menuForm.Show();
        }
    }
}
