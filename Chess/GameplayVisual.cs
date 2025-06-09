using System;
using System.Drawing;
using GameModel;
using System.Windows.Forms;
using Chess;
using Chess.Properties;
using System.Runtime.CompilerServices;

namespace GameVisual
{
    public sealed class BoardVisual
    {
        private const int BOARD_SIZE = 8;
        private readonly CellVisual[,] cellsVisuals = new CellVisual[BOARD_SIZE, BOARD_SIZE];
        public readonly Board board;

        public BoardVisual(IGameModeStrategy strategy, Control.ControlCollection controls, Point offset)
        {
            int gap = 5;
            int cellSize = CellVisual.CELL_SIZE;

            board = new Board(new Cell[8, 8], strategy);

            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    cellsVisuals[i, j] = new CellVisual(board.BoardCells[i, j])
                    {
                        Location = new Point(cellSize * i + gap * i + offset.X, cellSize * j + gap * j + offset.Y)
                    };

                    const int labelSize = 20;
                    if (i is 0 or 7)
                    {
                        int textOffsetX = i == 0? cellsVisuals[i, j].Location.X + cellSize / 2 - 50 - labelSize / 2:
                            cellsVisuals[i, j].Location.X + cellSize / 2 + 50 - labelSize / 2;
                        string text = Math.Abs(j - 8).ToString();

                        Label rowNumber = GenerateLabel(text, 
                            new Point(textOffsetX, cellsVisuals[i, j].Location.Y + cellSize / 2 - labelSize / 2),
                            new Size(labelSize, labelSize));

                        controls.Add(rowNumber);
                    }

                    if (j is 0 or 7)
                    {
                        int textOffsetY = j == 0 ? cellsVisuals[i, j].Location.Y + cellSize / 2 - 50 - labelSize / 2 :
                            cellsVisuals[i, j].Location.Y + cellSize / 2 + 50 - labelSize / 2;
                        char letter = (char)('A' + i);

                        Label colLetter = GenerateLabel(letter.ToString(),
                            new Point(cellsVisuals[i, j].Location.X + cellSize / 2 - labelSize / 2, textOffsetY),
                            new Size(labelSize, labelSize));

                        controls.Add(colLetter);
                    }
                    controls.Add(cellsVisuals[i, j]);
                }
            }

            board.OnKingMated += Board_OnKingMated;
            board.OnKingChecked += Board_OnKingChecked;
            board.OnStalemateHappened += Board_OnStalemateHappened;
            board.OnPawnPromoted += Board_OnPawnPromoted;
        }

        private Label GenerateLabel(string text, Point location, Size size)
        {
            Label label = new Label()
            {
                Location = location,
                Size = size,
                Text = text,
                ForeColor = Color.White,
                Font = new Font("Arial Rounded MT", 14, FontStyle.Bold)
            };

            return label;
        }

        private void Board_OnPawnPromoted(object sender, PieceEventArgs e)
        {
            if (board.Strategy is PvBStrategy && e.Piece.IsWhite == false)
            {
                Cell pawnCell = e.Piece.CurrentCell;
                pawnCell.RemovePiece();
                pawnCell.PlacePiece(new Queen(false));
                return;
            }
            fChoosePiece choosePieceForm = new fChoosePiece(e.Piece as Pawn);

            if (choosePieceForm.ShowDialog() == DialogResult.OK) { }
        }

        private void Board_OnStalemateHappened(object sender, EventArgs e)
        {
            MessageBox.Show("Stalemate!", "Stalemate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void Board_OnKingMated(object sender, PieceEventArgs e)
        {
            string kingColor = e.Piece.IsWhite ? "White" : "Black";
            MessageBox.Show($"{kingColor} king mated!", "Mate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void Board_OnKingChecked(object sender, PieceEventArgs e)
        {
            MessageBox.Show("Check!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public void PlacePiece(int x, int y, Piece piece)
        {
            board.BoardCells[x, y].PlacePiece(piece);
        }
    }

    public sealed class CellVisual : Panel
    {
        public const int CELL_SIZE = 50;
        private readonly Cell cellLogic;
        private PieceVisual currPiece;
        private Panel highlightMark;

        public bool isHighlighted { get; private set; }

        public CellVisual(Cell cell)
        {
            cellLogic = cell;
            Size = new Size(CELL_SIZE, CELL_SIZE);
            BackColor = cellLogic.IsWhite ? Color.Beige : Color.Brown;

            CreateHighlightMark();
            highlightMark.Hide();

            AllowDrop = true;
            DragEnter += (sender, e) => e.Effect = DragDropEffects.Move;
            DragDrop += CellVisual_OnDragDrop;

            cellLogic.OnPieceChanged += CellLogic_OnPieceChanged;
            cellLogic.OnCellHighlighted += CellLogic_OnCellHighlighted;
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
                BackColor = Color.DarkGreen
            };
            Controls.Add(highlightMark);
            highlightMark.BringToFront();

            Cursor = Cursors.Hand;
            highlightMark.Click += HighlightMarkOnClick;
            

            void HighlightMarkOnClick(object sender, EventArgs e)
            {
                Game.Instance.Strategy.MakeMove(PieceSelection.Instance.CurrentSelected, cellLogic);
            }
        }

        private void CellLogic_OnCellHighlighted(bool obj)
        {
            if (obj == true)
            {
                highlightMark.Show();
                highlightMark.BringToFront();
                isHighlighted = true;
            }
            else
            {
                highlightMark.Hide();
                isHighlighted = false;
            }
        }

        private void CellVisual_OnDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(PieceVisual)) is PieceVisual pieceVisual)
            {
                Game.Instance.Strategy.MakeMove(pieceVisual.PieceLogic, cellLogic);
            }

            if (e.Data.GetData(typeof(PieceSpawner)) is PieceSpawner pieceSpawner)
            {
                if (cellLogic.IsOccupied() == false)
                {
                    cellLogic.PlacePiece(pieceSpawner.GetPiece());
                }
            }
        }

        private void CellLogic_OnPieceChanged(object sender, PieceEventArgs e)
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

        private const int PIECE_SIZE = 50;
        private Point mouseDownLocation;
        private bool isDragging;

        public PieceVisual(Piece piece)
        {
            PieceLogic = piece;
            Size = new Size(PIECE_SIZE, PIECE_SIZE);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;

            BackgroundImage = PieceImageResolver.GetImage(piece);
            BackgroundImageLayout = ImageLayout.Stretch;

            isDragging = true;

            MouseDown += PieceVisual_OnMouseDown;
            MouseMove += PieceVisual_OnMouseMove;
            MouseUp += PieceVisual_OnMouseUp;
        }

        private void PieceVisual_OnMouseDown(object sender, MouseEventArgs e)
        {
            isDragging = false;
            mouseDownLocation = e.Location;

            Game.Instance.Strategy.SelectPiece(PieceLogic);
        }

        private void PieceVisual_OnMouseMove(object sender, MouseEventArgs e)
        {
            var dx = Math.Abs(e.X - mouseDownLocation.X);
            var dy = Math.Abs(e.Y - mouseDownLocation.Y);

            if ((!PieceLogic.IsWhite || Game.Instance.CurrentColorMove != CurrentMove.White) &&
                (PieceLogic.IsWhite || Game.Instance.CurrentColorMove != CurrentMove.Black)) return;

            if (BoardManipulations.GetKing(PieceLogic.CurrentCell.Board, PieceLogic.IsWhite).IsCheckmated())
                return;

            if (!isDragging && (dx > 4 || dy > 4)) // поріг у 4 пікселі
            {
                isDragging = true;
                DoDragDrop(this, DragDropEffects.Move);
            }
        }

        private void PieceVisual_OnMouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }
    }

    public sealed class PieceSpawner : PictureBox
    {
        public event EventHandler OnPiecePlaced;

        private readonly Piece piece;

        public PieceSpawner(Piece piece, Point location, Control.ControlCollection controls)
        {
            this.piece = piece;
            Location = location;
            BackColor = Color.DarkGray;
            Size = new Size(40, 40);
            Cursor = Cursors.Hand;

            BackgroundImage = PieceImageResolver.GetImage(piece);
            BackgroundImageLayout = ImageLayout.Stretch;

            controls.Add(this);

            MouseMove += OnMouseMove;
        }

        public Piece GetPiece()
        {
            OnPiecePlaced?.Invoke(this, EventArgs.Empty);
            return piece.Clone();
        }

    private void OnMouseMove(object sender, MouseEventArgs e)
        {
            DoDragDrop(this, DragDropEffects.Move);
        }
    }

    public static class PieceImageResolver
    {
        public static Image GetImage(Piece piece)
        {
            return piece switch
            {
                Pawn when piece.IsWhite => Resources.Chess_plt60,
                Knight when piece.IsWhite => Resources.Chess_nlt60,
                Bishop when piece.IsWhite => Resources.Chess_blt60,
                Queen when piece.IsWhite => Resources.Chess_qlt60,
                King when piece.IsWhite => Resources.Chess_klt60,
                Rook when piece.IsWhite => Resources.Chess_rlt60,
                Pawn when !piece.IsWhite => Resources.Chess_pdt60,
                Knight when !piece.IsWhite => Resources.Chess_ndt60,
                Bishop when !piece.IsWhite => Resources.Chess_bdt60,
                Queen when !piece.IsWhite => Resources.Chess_qdt60,
                King when !piece.IsWhite => Resources.Chess_kdt60,
                Rook when !piece.IsWhite => Resources.Chess_rdt60,
                _ => null
            };
        }
    }
}