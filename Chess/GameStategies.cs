using System;
using System.Drawing;
using System.Threading.Tasks;
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

        public IGameModeStrategy Strategy { get; private set; }

        private Game()
        {
            pieceSelection = PieceSelection.Instance;
        }

        public void StartGame(Control.ControlCollection controls, IGameModeStrategy strategy)
        {
            Strategy = strategy;
            Strategy.Init(controls);
            Strategy.OnPieceSelected += Strategy_OnPieceSelected;
            Strategy.OnPieceMoved += Strategy_OnPieceMoved;
        }

        private void Strategy_OnPieceMoved(object sender, PieceMovedEventArgs e)
        {
            if (Strategy is PvPStrategy or ModelingStrategy)
                CurrentColorMove = CurrentColorMove == CurrentMove.White ? CurrentMove.Black : CurrentMove.White;
            else if (Strategy is PvBStrategy)
                CurrentColorMove = CurrentColorMove == CurrentMove.White ? CurrentMove.None : CurrentMove.White;
        }


        private void Strategy_OnPieceSelected(object sender, PieceEventArgs e)
        {
            pieceSelection.TryToSelect(e.Piece);
        }

        public void EndGame()
        {
            instance = null;
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

        public void Init(Control.ControlCollection controls)
        {
            CreateBoard(controls);
        }

        private void CreateBoard(Control.ControlCollection controls)
        {
            BoardVisual boardVisual = new(this, controls, new Point(75, 75));

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
        private Board board;

        public void Init(Control.ControlCollection controls)
        {
            CreateBoard(controls);
        }

        private void CreateBoard(Control.ControlCollection controls)
        {
            BoardVisual boardVisual = new(this, controls, new Point(75, 75));

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

        public async void MakeMove(Piece piece, Cell targetCell)
        {
            Cell fromCell = piece.CurrentCell;
            if (piece.CurrentCell.TryMovePieceTo(targetCell))
            {
                isBotMove = !isBotMove;
                OnPieceMoved?.Invoke(this, new PieceMovedEventArgs() {Piece = piece, From = fromCell});
                board = targetCell.Board;
                //await Task.Delay(1000);

                if (BoardManipulations.GetKing(board, false).IsCheckmated())
                    return;

                var botDecision = await Task.Run(() => bot.FindBestBotMove(board));
                Cell botFromCell = botDecision.piece.CurrentCell;
                botDecision.piece.CurrentCell.TryMovePieceTo(botDecision.move);
                isBotMove = !isBotMove;
                OnPieceMoved?.Invoke(this, new PieceMovedEventArgs() { Piece = botDecision.piece, From = botFromCell });
            }
        }
    }

    public class ModelingStrategy : IGameModeStrategy
    {
        private enum StrategyState
        {
            Modelling, 
            Playing
        }

        public event EventHandler<PieceEventArgs> OnPieceSelected;
        public event EventHandler<PieceMovedEventArgs> OnPieceMoved;

        private StrategyState strategyState = StrategyState.Modelling;
        private bool isWhiteMove = true;
        private Button deleteButton;
        private Button startButton;
        private Piece selectedPiece;

        public void Init(Control.ControlCollection controls)
        {
            BoardVisual boardVisual = new(this, controls, new Point(75, 25));

            boardVisual.PlacePiece(4, 0, new King(false));
            boardVisual.PlacePiece(4, 7, new King(true));

            PieceSpawner whitePawnSpawner = new(new Pawn(true), new Point(200, 560), controls);
            PieceSpawner whiteKnightSpawner = new(new Knight(true), new Point(240, 560), controls);
            PieceSpawner whiteBishopSpawner = new(new Bishop(true), new Point(280, 560), controls);
            PieceSpawner whiteRookSpawner = new(new Rook(true), new Point(320, 560), controls);
            PieceSpawner whiteQueenSpawner = new(new Queen(true), new Point(360, 560), controls);

            PieceSpawner blackPawnSpawner = new(new Pawn(false), new Point(200, 520), controls);
            PieceSpawner blackKnightSpawner = new(new Knight(false), new Point(240, 520), controls);
            PieceSpawner blackBishopSpawner = new(new Bishop(false), new Point(280, 520), controls);
            PieceSpawner blackRookSpawner = new(new Rook(false), new Point(320, 520), controls);
            PieceSpawner blackQueenSpawner = new(new Queen(false), new Point(360, 520), controls);

            PieceSelection.Instance.OnPieceDeselected += PieceSelection_OnPieceDeselected;

            deleteButton = new Button
            {
                Location = new Point(550, 290),
                Size = new Size(50, 20),
                BackColor = Color.Beige,
                Text = "Delete"
            };
            deleteButton.Click += DeleteButton_OnClick;
            controls.Add(deleteButton);
            deleteButton.Hide();

            startButton = new Button
            {
                Location = new Point(550, 315),
                Size = new Size(50, 20),
                BackColor = Color.Beige,
                Text = "Start"
            };
            startButton.Click += StartButton_OnClick;
            controls.Add(startButton);
        }

        private void StartButton_OnClick(object sender, EventArgs e)
        {
            strategyState = StrategyState.Playing;
            startButton.Hide();
        }

        private void PieceSelection_OnPieceDeselected(object sender, EventArgs e)
        {
            deleteButton.Hide();
        }

        private void DeleteButton_OnClick(object sender, EventArgs e)
        {
            if (selectedPiece != null && selectedPiece is not King)
            {
                selectedPiece.DetachAllObservers();
                selectedPiece.CurrentCell.RemovePiece();
                deleteButton.Hide();
            }
        }

        public void SelectPiece(Piece p)
        {
            if (strategyState == StrategyState.Modelling)
            {
                deleteButton.Show();
                selectedPiece = p;
            }
            else if (p.IsWhite == isWhiteMove)
            {
                OnPieceSelected?.Invoke(this, new PieceEventArgs { Piece = p });
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

        public void Init(Control.ControlCollection controls);
        void SelectPiece(Piece p);
        void MakeMove(Piece piece, Cell targetCell);
    }
}