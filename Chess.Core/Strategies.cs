using Chess.Core.CustomEventArgs;

namespace Chess.Core
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

        public void StartGame(IGameModeStrategy strategy)
        {
            Strategy = strategy;
            Strategy.Init();
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
        public event EventHandler<GameInitializedEventArgs> OnInitialized;

        public void Init()
        {
            Board createdBoard = CreateBoard();
            OnInitialized?.Invoke(this, new GameInitializedEventArgs(createdBoard));
        }

        private Board CreateBoard()
        {
            Board createdBoard = new(this);

            for (int i = 0; i < 8; i++)
            {
                createdBoard.PlacePiece(i, 6, new Pawn(true));
            }

            createdBoard.PlacePiece(0, 7, new Rook(true));
            createdBoard.PlacePiece(1, 7, new Knight(true));
            createdBoard.PlacePiece(2, 7, new Bishop(true));
            createdBoard.PlacePiece(3, 7, new Queen(true));
            createdBoard.PlacePiece(4, 7, new King(true));
            createdBoard.PlacePiece(5, 7, new Bishop(true));
            createdBoard.PlacePiece(6, 7, new Knight(true));
            createdBoard.PlacePiece(7, 7, new Rook(true));

            for (int i = 0; i < 8; i++)
            {
                createdBoard.PlacePiece(i, 1, new Pawn(false));
            }

            createdBoard.PlacePiece(0, 0, new Rook(false));
            createdBoard.PlacePiece(1, 0, new Knight(false));
            createdBoard.PlacePiece(2, 0, new Bishop(false));
            createdBoard.PlacePiece(3, 0, new Queen(false));
            createdBoard.PlacePiece(4, 0, new King(false));
            createdBoard.PlacePiece(5, 0, new Bishop(false));
            createdBoard.PlacePiece(6, 0, new Knight(false));
            createdBoard.PlacePiece(7, 0, new Rook(false));

            return createdBoard;
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
                OnPieceMoved?.Invoke(this, new PieceMovedEventArgs() { Piece = piece, From = fromCell });
                isWhiteTurn = !isWhiteTurn;
            }
        }
    }

    public class PvBStrategy : IGameModeStrategy
    {
        public event EventHandler<PieceEventArgs> OnPieceSelected;
        public event EventHandler<PieceMovedEventArgs> OnPieceMoved;
        public event EventHandler<GameInitializedEventArgs> OnInitialized;

        private bool isBotMove;
        private readonly Bot bot = new();
        private Board board;

        public void Init()
        {
            board = CreateBoard();
            OnInitialized?.Invoke(this, new GameInitializedEventArgs(board));
        }

        private Board CreateBoard()
        {
            Board createdBoard = new(this);

            for (int i = 0; i < 8; i++)
            {
                createdBoard.PlacePiece(i, 6, new Pawn(true));
            }

            createdBoard.PlacePiece(0, 7, new Rook(true));
            createdBoard.PlacePiece(1, 7, new Knight(true));
            createdBoard.PlacePiece(2, 7, new Bishop(true));
            createdBoard.PlacePiece(3, 7, new Queen(true));
            createdBoard.PlacePiece(4, 7, new King(true));
            createdBoard.PlacePiece(5, 7, new Bishop(true));
            createdBoard.PlacePiece(6, 7, new Knight(true));
            createdBoard.PlacePiece(7, 7, new Rook(true));

            for (int i = 0; i < 8; i++)
            {
                createdBoard.PlacePiece(i, 1, new Pawn(false));
            }

            createdBoard.PlacePiece(0, 0, new Rook(false));
            createdBoard.PlacePiece(1, 0, new Knight(false));
            createdBoard.PlacePiece(2, 0, new Bishop(false));
            createdBoard.PlacePiece(3, 0, new Queen(false));
            createdBoard.PlacePiece(4, 0, new King(false));
            createdBoard.PlacePiece(5, 0, new Bishop(false));
            createdBoard.PlacePiece(6, 0, new Knight(false));
            createdBoard.PlacePiece(7, 0, new Rook(false));

            return createdBoard;
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
                OnPieceMoved?.Invoke(this, new PieceMovedEventArgs() { Piece = piece, From = fromCell });
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
        public enum StrategyState
        {
            Modelling,
            Playing
        }

        public event EventHandler<PieceEventArgs> OnPieceSelected;
        public event EventHandler<PieceMovedEventArgs> OnPieceMoved;
        public event EventHandler<GameInitializedEventArgs> OnInitialized;

        public StrategyState strategyState { get; private set; } = StrategyState.Modelling;
        private bool isWhiteMove = true;

        public void Init()
        {
            Board createdBoard = new(this);

            createdBoard.PlacePiece(4, 0, new King(false));
            createdBoard.PlacePiece(4, 7, new King(true));

            OnInitialized?.Invoke(this, new GameInitializedEventArgs(createdBoard));
        }

        public void StartPlayMode()
        {
            strategyState = StrategyState.Playing;
        }

        public void SelectPiece(Piece p)
        {
            if (strategyState == StrategyState.Modelling)
            {
                OnPieceSelected?.Invoke(this, new PieceEventArgs { Piece = p }); 
            }
            else if (p.IsWhite == isWhiteMove)
            {
                OnPieceSelected?.Invoke(this, new PieceEventArgs { Piece = p });
            }
        }

        public void MakeMove(Piece piece, Cell targetCell)
        {
            if (targetCell.IsOccupied() && targetCell.Piece is King)
                return;
            Cell fromCell = piece.CurrentCell;
            if (piece.CurrentCell.TryMovePieceTo(targetCell))
            {
                OnPieceMoved?.Invoke(this, new PieceMovedEventArgs() { Piece = piece, From = fromCell });
                isWhiteMove = !isWhiteMove;
            }
        }
    }


    public interface IGameModeStrategy
    {
        public event EventHandler<PieceEventArgs> OnPieceSelected;

        public event EventHandler<PieceMovedEventArgs> OnPieceMoved;

        public event EventHandler<GameInitializedEventArgs> OnInitialized;

        public void Init();
        public void SelectPiece(Piece p);
        public void MakeMove(Piece piece, Cell targetCell);
    }
}