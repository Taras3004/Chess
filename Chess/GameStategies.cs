using System;
using System.Drawing;
using System.Windows.Forms;
using GameModel;
using GameVisual;

namespace Chess
{
    public class Game
    {
        public static Game Instance => instance ??= new Game();
        private static Game instance;

        public event EventHandler OnKingChecked;
        public event EventHandler OnKingMated;

        private readonly PieceSelection pieceSelection;

        public IGameModeStrategy Strategy => strategy;
        private IGameModeStrategy strategy;

        private Game()
        {
            pieceSelection = PieceSelection.Instance;
        }

        public void StartGame(Control.ControlCollection controls)
        {
            strategy = new PvPStrategy(controls);

            strategy.OnPieceSelected += Strategy_OnPieceSelected;
            strategy.OnPieceMoved += Strategy_OnPieceMoved;
        }

        private void Strategy_OnPieceMoved(object sender, PieceEventArgs e)
        {
            bool isWhitePieceMoved = e.Piece.IsWhite;
            King king = null;

            king = BoardManipulations.GetKing(e.Piece.CurrentCell.Board, !isWhitePieceMoved);

            if (king.IsCheckmated())
            {
                OnKingMated?.Invoke(this, EventArgs.Empty);
            }
            else if (king.IsChecked())
            {
                pieceSelection.KingChecked();
                OnKingChecked?.Invoke(this, EventArgs.Empty);
            }
        }


        private void Strategy_OnPieceSelected(object sender, PieceEventArgs e)
        {
            pieceSelection.Select(e.Piece);
        }
    }

    public class PvPStrategy : IGameModeStrategy
    {
        private bool isWhiteTurn = true;

        public event EventHandler<PieceEventArgs> OnPieceSelected;
        public event EventHandler<PieceEventArgs> OnPieceMoved;

        public PvPStrategy(Control.ControlCollection controls)
        {
            CreateBoard(controls);
        }

        private void CreateBoard(Control.ControlCollection controls)
        {
            BoardVisual boardVisual = new(controls, new Point(100, 100));

            boardVisual.GetCellsVisuals[0, 6].CellLogic.PlacePiece(new King(true));
            boardVisual.GetCellsVisuals[0, 5].CellLogic.PlacePiece(new Pawn(true));

            boardVisual.GetCellsVisuals[1, 0].CellLogic.PlacePiece(new Rook(false));
            boardVisual.GetCellsVisuals[2, 1].CellLogic.PlacePiece(new Rook(false));
            boardVisual.GetCellsVisuals[3, 6].CellLogic.PlacePiece(new King(false));

            /*
            boardVisual.GetCellsVisuals[0, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[1, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[2, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[3, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[4, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[5, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[6, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[7, 6].CellLogic.PlacePiece(new Pawn(true));

            

            boardVisual.GetCellsVisuals[0, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[1, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[2, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[3, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[4, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[5, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[6, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[7, 1].CellLogic.PlacePiece(new Pawn(false));
            */
        }

        public void SelectPiece(Piece p)
        {
            if (p.IsWhite == isWhiteTurn)
            {
                OnPieceSelected?.Invoke(this, new PieceEventArgs() { Piece = p });
            }
        }

        public void MakeMove(Piece piece, Cell targetCell)
        {
            if (piece.CurrentCell.TryMovePieceTo(targetCell))
            {
                OnPieceMoved?.Invoke(this, new PieceEventArgs{Piece = piece});
                isWhiteTurn = !isWhiteTurn;
            }
        }
    }

    public class PvBStrategy : IGameModeStrategy
    {
        public event EventHandler<PieceEventArgs> OnPieceSelected;
        public event EventHandler<PieceEventArgs> OnPieceMoved;

        private bool isBotMove = false;
        private readonly Bot bot = new();
        private readonly Timer timer;
        private Cell[,] board;

        public PvBStrategy(Control.ControlCollection controls)
        {
            CreateBoard(controls);
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += Timer_OnTick;
        }

        private void Timer_OnTick(object sender, EventArgs e)
        {
            timer.Stop();

            var botDecision = bot.FindBestBotMove(board);
            botDecision.piece.CurrentCell.TryMovePieceTo(botDecision.move);
            isBotMove = !isBotMove;
            OnPieceMoved?.Invoke(this, new PieceEventArgs {Piece = botDecision.piece});
        }

        private void CreateBoard(Control.ControlCollection controls)
        {
            BoardVisual boardVisual = new(controls, new Point(100, 100));
            /*
            boardVisual.GetCellsVisuals[0, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[1, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[2, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[3, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[4, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[5, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[6, 6].CellLogic.PlacePiece(new Pawn(true));
            boardVisual.GetCellsVisuals[7, 6].CellLogic.PlacePiece(new Pawn(true));
            */
            boardVisual.GetCellsVisuals[0, 7].CellLogic.PlacePiece(new Rook(true));
            boardVisual.GetCellsVisuals[7, 7].CellLogic.PlacePiece(new Rook(true));

            boardVisual.GetCellsVisuals[2, 7].CellLogic.PlacePiece(new King(true));
            boardVisual.GetCellsVisuals[3, 7].CellLogic.PlacePiece(new Knight(true));

            boardVisual.GetCellsVisuals[0, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[1, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[2, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[3, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[4, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[5, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[6, 1].CellLogic.PlacePiece(new Pawn(false));
            boardVisual.GetCellsVisuals[7, 1].CellLogic.PlacePiece(new Pawn(false));
        }

        public void SelectPiece(Piece p)
        {
            if (isBotMove == false && p.IsWhite)
            {
                OnPieceSelected?.Invoke(this, new PieceEventArgs { Piece = p });
            }
        }

        public void MakeMove(Piece piece, Cell targetCell)
        {
            if (piece.CurrentCell.TryMovePieceTo(targetCell))
            {
                isBotMove = !isBotMove;
                OnPieceMoved?.Invoke(this, new PieceEventArgs {Piece = piece});
                board = targetCell.Board;

                timer.Start();
            }
        }
    }

    public class PieceEventArgs : EventArgs
    {
        public Piece Piece;
    }

    public interface IGameModeStrategy
    {
        event EventHandler<PieceEventArgs> OnPieceSelected;

        event EventHandler<PieceEventArgs> OnPieceMoved;

        void SelectPiece(Piece p);

        void MakeMove(Piece piece, Cell targetCell);
    }
}