using System;
using System.Windows.Forms;
using GameModel;

namespace Chess
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Game game = Game.Instance;

            game.StartGame(Controls);
            //game.OnKingMated += Game_OnKingMated;
        }


        private void Form1_Click(object sender, EventArgs e)
        {
            PieceSelection.Instance.Deselect();
        }
    }
}
