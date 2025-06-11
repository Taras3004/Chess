using Chess.Core;
using Chess.Core.CustomEventArgs;
using GameVisual;

namespace Chess
{
    public partial class fChoosePiece : Form
    {
        private readonly Pawn promotedPawn;

        public fChoosePiece(Pawn pawn)
        {
            InitializeComponent();
            promotedPawn = pawn;

            PieceButton knightButton = new PieceButton(new Knight(pawn.IsWhite), new Point(25, 25));
            PieceButton bishopButton = new PieceButton(new Bishop(pawn.IsWhite), new Point(75, 25));
            PieceButton rookButton = new PieceButton(new Rook(pawn.IsWhite), new Point(125, 25));
            PieceButton queenButton = new PieceButton(new Queen(pawn.IsWhite), new Point(175, 25));

            knightButton.OnPieceButtonClicked += PieceButton_OnClick;
            bishopButton.OnPieceButtonClicked += PieceButton_OnClick;
            rookButton.OnPieceButtonClicked += PieceButton_OnClick;
            queenButton.OnPieceButtonClicked += PieceButton_OnClick;

            Controls.Add(knightButton);
            Controls.Add(bishopButton);
            Controls.Add(rookButton);
            Controls.Add(queenButton);
        }

        private void PieceButton_OnClick(object sender, PieceEventArgs e)
        {
            Cell pawnCell = promotedPawn.CurrentCell;
            promotedPawn.CurrentCell.RemovePiece();
            
            pawnCell.PlacePiece(e.Piece);

            DialogResult = DialogResult.OK;
        }
    }

    public sealed class PieceButton : PictureBox
    {
        public event EventHandler<PieceEventArgs> OnPieceButtonClicked;

        private const int CELL_SiZE = 50;
        private Piece Piece { get; }

        public PieceButton(Piece piece, Point location)
        {
            Piece = piece;

            BackColor = Color.Transparent;
            BackgroundImage = PieceImageResolver.GetImage(Piece);
            BackgroundImageLayout = ImageLayout.Stretch;

            Location = location;
            Size = new Size(CELL_SiZE, CELL_SiZE);

            Click += OnClick;
        }

        private void OnClick(object sender, EventArgs e)
        {
            OnPieceButtonClicked?.Invoke(this, new PieceEventArgs {Piece = Piece});
        }
    }
}
