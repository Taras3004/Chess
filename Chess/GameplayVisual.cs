using System;
using System.Drawing;
using GameModel;
using System.Windows.Forms;
using Chess;

namespace GameVisual
{
    public sealed class BoardVisual
    {
        private const int BOARD_SIZE = 8;
        private readonly CellVisual[,] cellsVisuals = new CellVisual[BOARD_SIZE, BOARD_SIZE];

        public CellVisual[,] GetCellsVisuals => cellsVisuals;

        public BoardVisual(Control.ControlCollection controls, Point offset)
        {
            int gap = 5;
            int cellSize = CellVisual.CELL_SIZE;

            Cell[,] board = new Cell[BOARD_SIZE, BOARD_SIZE];

            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    cellsVisuals[i, j] = new CellVisual(new Point(i, j), board)
                    {
                        Location = new Point(cellSize * i + gap * i + offset.X, cellSize * j + gap * j + offset.Y)
                    };
                    board[i, j] = cellsVisuals[i, j].CellLogic;
                    controls.Add(cellsVisuals[i, j]);
                }
            }
        }
    }

    public sealed class CellVisual : Panel
    {
        public const int CELL_SIZE = 50;
        public readonly Cell CellLogic;
        private PieceVisual currPiece;
        private Panel highlightMark;

        public CellVisual(Point position, Cell[,] board)
        {
            CellLogic = new Cell(position, board);
            Size = new Size(CELL_SIZE, CELL_SIZE);
            BackColor = CellLogic.IsWhite ? Color.Beige : Color.Brown;

            CreateHighlightMark();
            highlightMark.Hide();

            AllowDrop = true;
            DragEnter += (sender, e) => e.Effect = DragDropEffects.Move;
            DragDrop += CellVisual_OnDragDrop;

            CellLogic.OnPieceChanged += CellLogic_OnPieceChanged;
            CellLogic.OnCellHighlighted += CellLogic_OnCellHighlighted;
        }


        private void CreateHighlightMark()
        {
            const int MARK_SIZE = 20;

            highlightMark = new Panel()
            {
                Location = new Point(
                    (CELL_SIZE - MARK_SIZE) / 2,
                    (CELL_SIZE - MARK_SIZE) / 2),
                Size = new Size(MARK_SIZE, MARK_SIZE),
                BackColor = Color.Green
            };
            Controls.Add(highlightMark);
            highlightMark.BringToFront();

            Cursor = Cursors.Hand;
            highlightMark.Click += HighlightMarkOnClick;
            

            void HighlightMarkOnClick(object sender, EventArgs e)
            {
                Game.Instance.Strategy.MakeMove(PieceSelection.Instance.CurrentSelected, CellLogic);
            }
        }

        private void CellLogic_OnCellHighlighted(bool obj)
        {
            if (obj == true)
            {
                highlightMark.Show();
                highlightMark.BringToFront();
            }
            else
            {
                highlightMark.Hide();
            }
        }

        private void CellVisual_OnDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(PieceVisual)) is PieceVisual pieceVisual)
            {
                Game.Instance.Strategy.MakeMove(pieceVisual.PieceLogic, CellLogic);
            }
        }

        private void CellLogic_OnPieceChanged(object sender, Cell.OnPiecePlacedEventArgs e)
        {
            if (e.Piece == null && currPiece != null)
            {
                Controls.Remove(currPiece);
                currPiece.Dispose();
                return;
            }

            currPiece = new PieceVisual(e.Piece);
            currPiece.Location = new Point(
                (CELL_SIZE - currPiece.Width) / 2,
                (CELL_SIZE - currPiece.Height) / 2
            );

            Controls.Add(currPiece);
            currPiece.BringToFront();
        }
    }

    public sealed class PieceVisual : PictureBox
    {
        public readonly Piece PieceLogic;

        private const int PIECE_SIZE = 40;
        private Point mouseDownLocation;
        private bool isDragging;

        public PieceVisual(Piece piece)
        {
            PieceLogic = piece;
            Size = new Size(PIECE_SIZE, PIECE_SIZE);
            BackColor = PieceLogic.IsWhite ? Color.White : Color.Black;
            Cursor = Cursors.Hand;

            MouseDown += PieceVisual_OnMouseDown;
            MouseMove += PieceVisual_OnMouseMove;
            MouseUp += PieceVisual_OnMouseUp;
        }

        private void PieceVisual_OnMouseDown(object sender, MouseEventArgs e)
        {
            isDragging = false;
            mouseDownLocation = e.Location;
        }

        private void PieceVisual_OnMouseMove(object sender, MouseEventArgs e)
        {
            var dx = Math.Abs(e.X - mouseDownLocation.X);
            var dy = Math.Abs(e.Y - mouseDownLocation.Y);

            if (!isDragging && (dx > 4 || dy > 4)) // поріг у 4 пікселі
            {
                isDragging = true;
                DoDragDrop(this, DragDropEffects.Move);
            }
        }

        private void PieceVisual_OnMouseUp(object sender, MouseEventArgs e)
        {
            if (!isDragging)
            {
                Game.Instance.Strategy.SelectPiece(PieceLogic);
                //PieceSelection.Instance.Select(PieceLogic);
            }
        }
    }

    public static class PieceImageResolver
    {
        public static Image GetImage(Piece piece)
        {
            throw new NotImplementedException();
            /*
            if (piece is Pawn && piece.Color == PieceColor.White)
                return Properties.Resources.WhitePawn;
            if (piece is Knight && piece.Color == PieceColor.Black)
                return Properties.Resources.BlackKnight;
            */
            return null;
        }
    }
}