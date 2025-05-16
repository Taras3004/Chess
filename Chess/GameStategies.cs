using System;
using System.Collections.Generic;
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

        private readonly PieceSelection pieceSelection;

        public CurrentMove CurrentColorMove { get; private set; } = CurrentMove.White;

        public IGameModeStrategy Strategy => strategy;
        private IGameModeStrategy strategy;

        private Game()
        {
            pieceSelection = PieceSelection.Instance;
        }

        public void StartGame(Control.ControlCollection controls)
        {
            strategy = new PvBStrategy(controls);

            strategy.OnPieceSelected += Strategy_OnPieceSelected;
            strategy.OnPieceMoved += Strategy_OnPieceMoved;
        }

        private void Strategy_OnPieceMoved(object sender, PieceEventArgs e)
        {
            if (strategy is PvPStrategy)
            {
                CurrentColorMove = CurrentColorMove == CurrentMove.White ? CurrentMove.Black : CurrentMove.White;
            }
            else if (strategy is PvBStrategy)
                CurrentColorMove = CurrentColorMove == CurrentMove.White ? CurrentMove.None : CurrentMove.White;
        }


        private void Strategy_OnPieceSelected(object sender, PieceEventArgs e)
        {
            pieceSelection.TryToSelect(e.Piece);
        }
    }

    public enum CurrentMove
    {
        White,
        Black,
        None
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
            BoardVisual boardVisual = new(this, controls, new Point(100, 100));

            boardVisual.PlacePiece(3, 5, new Pawn(true));
            boardVisual.PlacePiece(4, 5, new King(true));
            boardVisual.PlacePiece(6, 6, new Rook(true));
            boardVisual.PlacePiece(6, 2, new Bishop(true));

            boardVisual.PlacePiece(6, 7, new Rook(false));
            boardVisual.PlacePiece(7, 4, new Knight(false));
            boardVisual.PlacePiece(0, 0, new King(false));
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
        private Board board;

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
            if (BoardManipulations.GetKing(board, false).IsCheckmated())
                return;
            var botDecision = bot.FindBestBotMove(board);
            botDecision.piece.CurrentCell.TryMovePieceTo(botDecision.move);
            isBotMove = !isBotMove;
            OnPieceMoved?.Invoke(this, new PieceEventArgs {Piece = botDecision.piece});
        }

        private void CreateBoard(Control.ControlCollection controls)
        {
            BoardVisual boardVisual = new(this, controls, new Point(100, 100));

            boardVisual.PlacePiece(0, 7, new Rook(true));
            boardVisual.PlacePiece(7, 7, new Rook(true));

            boardVisual.PlacePiece(2, 7, new King(true));
            boardVisual.PlacePiece(3, 7, new Knight(true));


            boardVisual.PlacePiece(0, 0, new King(false));
            boardVisual.PlacePiece(0, 1, new Pawn(false));
            boardVisual.PlacePiece(1, 1, new Pawn(false));
            boardVisual.PlacePiece(2, 1, new Pawn(false));
            boardVisual.PlacePiece(3, 1, new Pawn(false));
            boardVisual.PlacePiece(4, 1, new Pawn(false));
            boardVisual.PlacePiece(5, 1, new Pawn(false));
            boardVisual.PlacePiece(6, 1, new Pawn(false));
            boardVisual.PlacePiece(7, 1, new Pawn(false));

            board = boardVisual.board;
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