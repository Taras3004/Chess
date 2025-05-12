using System;
using System.Drawing;
using System.Windows.Forms;
using GameVisual;
using GameModel;

namespace Chess
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            BoardVisual boardVisual = new BoardVisual(Controls, new Point(100, 100));

            boardVisual.GetCellsVisuals[1, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[2, 1].CellLogic.PlacePiece(new Pawn(false));
        }


        private void Form1_Click(object sender, EventArgs e)
        {
            PieceSelection.Deselect();
        }
    }
}
