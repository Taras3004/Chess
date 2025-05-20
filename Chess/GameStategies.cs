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

        private void Strategy_OnPieceMoved(object sender, PieceMovedEventArgs e)
        {
            if (strategy is PvPStrategy or ModelingStrategy)
                CurrentColorMove = CurrentColorMove == CurrentMove.White ? CurrentMove.Black : CurrentMove.White;
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
        public event EventHandler<PieceMovedEventArgs> OnPieceMoved;

        public PvPStrategy(Control.ControlCollection controls)
        {
            CreateBoard(controls);
        }

        private void CreateBoard(Control.ControlCollection controls)
        {
            BoardVisual boardVisual = new(this, controls, new Point(100, 100));

            for (int i = 0; i < 8; i++)
            {
                boardVisual.PlacePiece(i, 6, new Pawn(true));
            }
            boardVisual.PlacePiece(0, 7, new Rook(true));
            boardVisual.PlacePiece(1, 7, new Knight(true));
            boardVisual.PlacePiece(2, 7, new Bishop(true));
            boardVisual.PlacePiece(3, 7, new Queen(true));
            boardVisual.PlacePiece(4, 7, new King(true));
            boardVisual.PlacePiece(5, 7, new Bishop(true));
            boardVisual.PlacePiece(6, 7, new Knight(true));
            boardVisual.PlacePiece(7, 7, new Rook(true));

            for (int i = 0; i < 8; i++)
            {
                boardVisual.PlacePiece(i, 1, new Pawn(false));
            }
            boardVisual.PlacePiece(0, 0, new Rook(false));
            boardVisual.PlacePiece(1, 0, new Knight(false));
            boardVisual.PlacePiece(2, 0, new Bishop(false));
            boardVisual.PlacePiece(3, 0, new Queen(false));
            boardVisual.PlacePiece(4, 0, new King(false));
            boardVisual.PlacePiece(5, 0, new Bishop(false));
            boardVisual.PlacePiece(6, 0, new Knight(false));
            boardVisual.PlacePiece(7, 0, new Rook(false));
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
            Cell fromCell = piece.CurrentCell;

            if (piece.CurrentCell.TryMovePieceTo(targetCell))
            {
                OnPieceMoved?.Invoke(this, new PieceMovedEventArgs(){Piece = piece, From = fromCell});
                isWhiteTurn = !isWhiteTurn;
            }
        }
    }

    public class PvBStrategy : IGameModeStrategy
    {
        public event EventHandler<PieceEventArgs> OnPieceSelected;
        public event EventHandler<PieceMovedEventArgs> OnPieceMoved;

        private bool isBotMove;
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
            Cell fromCell = botDecision.piece.CurrentCell;
            botDecision.piece.CurrentCell.TryMovePieceTo(botDecision.move);
            isBotMove = !isBotMove;
            OnPieceMoved?.Invoke(this, new PieceMovedEventArgs() {Piece = botDecision.piece, From = fromCell});
        }

        private void CreateBoard(Control.ControlCollection controls)
        {
            BoardVisual boardVisual = new(this, controls, new Point(100, 100));

            for (int i = 0; i < 8; i++)
            {
                boardVisual.PlacePiece(i, 6, new Pawn(true));
            }
            boardVisual.PlacePiece(0, 7, new Rook(true));
            boardVisual.PlacePiece(1, 7, new Knight(true));
            boardVisual.PlacePiece(2, 7, new Bishop(true));
            boardVisual.PlacePiece(3, 7, new Queen(true));
            boardVisual.PlacePiece(4, 7, new King(true));
            boardVisual.PlacePiece(5, 7, new Bishop(true));
            boardVisual.PlacePiece(6, 7, new Knight(true));
            boardVisual.PlacePiece(7, 7, new Rook(true));

            for (int i = 0; i < 8; i++)
            {
                boardVisual.PlacePiece(i, 1, new Pawn(false));
            }
            boardVisual.PlacePiece(0, 0, new Rook(false));
            boardVisual.PlacePiece(1, 0, new Knight(false));
            boardVisual.PlacePiece(2, 0, new Bishop(false));
            boardVisual.PlacePiece(3, 0, new Queen(false));
            boardVisual.PlacePiece(4, 0, new King(false));
            boardVisual.PlacePiece(5, 0, new Bishop(false));
            boardVisual.PlacePiece(6, 0, new Knight(false));
            boardVisual.PlacePiece(7, 0, new Rook(false));

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
            Cell fromCell = piece.CurrentCell;
            if (piece.CurrentCell.TryMovePieceTo(targetCell))
            {
                isBotMove = !isBotMove;
                OnPieceMoved?.Invoke(this, new PieceMovedEventArgs() {Piece = piece, From = fromCell});
                board = targetCell.Board;

                timer.Start();
            }
        }
    }

    public class ModelingStrategy : IGameModeStrategy
    {
        public event EventHandler<PieceEventArgs> OnPieceSelected;
        public event EventHandler<PieceMovedEventArgs> OnPieceMoved;

        private bool isWhiteMove = true;
        private readonly Button deleteButton;

        public ModelingStrategy(Control.ControlCollection controls)
        {
            BoardVisual boardVisual = new(this, controls, new Point(100, 100));

            boardVisual.PlacePiece(4, 0, new King(false));
            boardVisual.PlacePiece(4, 7, new King(true));

            PieceSpawner whitePawnSpawner = new(new Pawn(true), new Point(0, 0), controls);
            PieceSpawner whiteKnightSpawner = new(new Knight(true), new Point(40, 0), controls);
            PieceSpawner whiteBishopSpawner = new(new Bishop(true), new Point(80, 0), controls);
            PieceSpawner whiteRookSpawner = new(new Rook(true), new Point(120, 0), controls);
            PieceSpawner whiteQueenSpawner = new(new Queen(true), new Point(160, 0), controls);

            PieceSpawner blackPawnSpawner = new(new Pawn(false), new Point(0, 40), controls);
            PieceSpawner blackKnightSpawner = new(new Knight(false), new Point(40, 40), controls);
            PieceSpawner blackBishopSpawner = new(new Bishop(false), new Point(80, 40), controls);
            PieceSpawner blackRookSpawner = new(new Rook(false), new Point(120, 40), controls);
            PieceSpawner blackQueenSpawner = new(new Queen(false), new Point(160, 40), controls);

            deleteButton = new Button
            {
                Location = new Point(400, 40),
                Size = new Size(50, 20),
                BackColor = Color.Beige,
                Text = "Delete"
            };
            deleteButton.Click += DeleteButton_OnClick;
            controls.Add(deleteButton);
            deleteButton.Hide();
        }

        private void DeleteButton_OnClick(object sender, EventArgs e)
        {
            DeleteSelectedPiece();
        }

        public void SelectPiece(Piece p)
        {
            if (p.IsWhite == isWhiteMove)
            {
                OnPieceSelected?.Invoke(this, new PieceEventArgs { Piece = p });
                if (p is not King)
                    deleteButton.Show();
                else
                    deleteButton.Hide();
            }
        }

        public void MakeMove(Piece piece, Cell targetCell)
        {
            if(targetCell.IsOccupied() && targetCell.Piece is King)
                return;
            Cell fromCell = piece.CurrentCell;
            if (piece.CurrentCell.TryMovePieceTo(targetCell))
            {
                OnPieceMoved?.Invoke(this, new PieceMovedEventArgs() { Piece = piece, From = fromCell });
                isWhiteMove = !isWhiteMove;
            }
        }

        private void DeleteSelectedPiece()
        {
            Piece p = PieceSelection.Instance.CurrentSelected;
            if (p != null && p is not King)
            {
                p.DetachAllObservers();
                p.CurrentCell.RemovePiece();
                deleteButton.Hide();
            }
        }
    }


    public class PieceEventArgs : EventArgs
    {
        public Piece Piece;
    }

    public class PieceMovedEventArgs : EventArgs
    {
        public Piece Piece;
        public Cell From;
    }

    public interface IGameModeStrategy
    {
        event EventHandler<PieceEventArgs> OnPieceSelected;

        event EventHandler<PieceMovedEventArgs> OnPieceMoved;

        void SelectPiece(Piece p);

        void MakeMove(Piece piece, Cell targetCell);
    }
}