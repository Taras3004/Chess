using System;
using System.Collections.Generic;
using System.Drawing;
using Chess;

namespace GameModel
{
    public class Board
    {
        public Cell[,] BoardCells { get; }

        public event EventHandler<PieceEventArgs> OnKingChecked;
        public event EventHandler<PieceEventArgs> OnKingMated;

        public readonly IGameModeStrategy Strategy;

        public Board(Cell[,] boardCells, IGameModeStrategy strategy)
        {
            this.Strategy = strategy;
            BoardCells = boardCells;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    BoardCells[i, j] = new Cell(new Point(i, j), this);
                }
            }

            strategy.OnPieceMoved += Strategy_OnPieceMoved;
        }

        private void Strategy_OnPieceMoved(object sender, PieceEventArgs e)
        {
            bool isWhitePieceMoved = e.Piece.IsWhite;
            King king = null;

            king = BoardManipulations.GetKing(e.Piece.CurrentCell.Board, !isWhitePieceMoved);

            if (king.IsCheckmated())
            {
                OnKingMated?.Invoke(this, new PieceEventArgs { Piece = king });
            }
            else if (king.IsChecked())
            {
                OnKingChecked?.Invoke(this, new PieceEventArgs { Piece = king });
            }
        }
    }

    public class Cell(Point position, Board board)
    {
        public class OnPiecePlacedEventArgs : EventArgs
        {
            public Piece Piece;
        }

        public event EventHandler<OnPiecePlacedEventArgs> OnPieceChanged;
        public event Action<bool> OnCellHighlighted;

        public bool IsWhite => (position.X + position.Y) % 2 == 0;
        public Point Position => position;
        public Board Board => board;
        public Piece Piece { get; private set; }

        public void PlacePiece(Piece p)
        {
            OnPieceChanged?.Invoke(this, new OnPiecePlacedEventArgs
            {
                Piece = p
            });

            Piece = p;
            Piece.SetCurrentCell(this);
        }

        public void RemovePiece()
        {
            if (Piece == null)
                return;
            Piece = null;

            OnPieceChanged?.Invoke(this, new OnPiecePlacedEventArgs
            {
                Piece = null
            });
        }

        public bool TryMovePieceTo(Cell cell)
        {
            if (Piece == null)
                throw new NullReferenceException();
            if (Piece.CanMoveTo(cell) == false)
                return false;
            if (cell.Piece != null && cell.Piece.IsWhite != Piece.IsWhite)
                cell.RemovePiece();

            cell.PlacePiece(Piece);
            RemovePiece();
            return true;
        }

        public bool IsOccupied()
        {
            return Piece != null;
        }

        public void UpdateHighlightMode(bool state)
        {
            OnCellHighlighted?.Invoke(state);
        }
    }

    public class PieceSelection
    {
        public static PieceSelection Instance => instance ??= new PieceSelection();
        private static PieceSelection instance;

        public Piece CurrentSelected { get; private set; }

        private PieceSelection()
        {
        }

        private void Select(Piece piece, List<Cell> customObservers = null)
        {
            if (CurrentSelected != null && CurrentSelected != piece)
            {
                CurrentSelected.UpdateObservers(false);
            }

            CurrentSelected = piece;
            CurrentSelected.AttachObservers();
            CurrentSelected.UpdateObservers(true, customObservers);
        }

        public bool TryToSelect(Piece piece)
        {
            King king = BoardManipulations.GetKing(piece.CurrentCell.Board, piece.IsWhite);

            if (king.IsChecked())
            {
                List<(Piece defender, Cell to)> possibleSelectedPieces = king.KingDefenders();

                foreach (Cell move in king.PossibleMoves)
                {
                    possibleSelectedPieces.Add((king, move));
                }

                List<Cell> highlightedObservers = new List<Cell>();

                foreach ((Piece defender, Cell to)  in possibleSelectedPieces)
                {
                    if(defender == piece)
                        highlightedObservers.Add(to);
                }

                if (highlightedObservers.Count > 0)
                {
                    Select(piece, highlightedObservers);
                    return true;
                }
                

                return false;
            }

            Select(piece);
            return true;
        }

        public void Deselect()
        {
            if (CurrentSelected != null)
            {
                CurrentSelected.UpdateObservers(false);
                CurrentSelected = null;
            }
        }
    }

    public abstract class Piece(bool isWhite)
    {
        private readonly List<Cell> observers = [];
        protected Cell currentCell;

        public bool IsWhite { get; } = isWhite;
        public Cell CurrentCell => currentCell;

        protected abstract List<Cell> CalculatePossibleMoves();
        public abstract Piece Clone();

        public List<Cell> PossibleMoves => CalculatePossibleMoves();


        protected bool IsInBounds(int x, int y) => x is >= 0 and < 8 && y is >= 0 and < 8;

        public bool CanMoveTo(Cell cell)
        {
            return CalculatePossibleMoves().Contains(cell);
        }

        public void SetCurrentCell(Cell cell)
        {
            currentCell = cell;
            AttachObservers();
        }

        public void AttachObservers()
        {
            DetachAllObservers();
            foreach (Cell possibleMove in CalculatePossibleMoves())
            {
                AttachObserver(possibleMove);
            }
        }

        private void AttachObserver(Cell cell)
        {
            observers.Add(cell);
        }

        public void DetachAllObservers()
        {
            foreach (var observer in observers)
            {
                observer.UpdateHighlightMode(false);
            }

            observers.Clear();
        }

        public void UpdateObservers(bool state)
        {
            foreach (Cell observer in observers)
            {
                observer.UpdateHighlightMode(state);
            }
        }

        public void UpdateObservers(bool state, List<Cell> customObservers)
        {
            if (customObservers == null)
            {
                UpdateObservers(state);
                return;
            }

            foreach (Cell observer in customObservers)
            {
                if (observers.Contains(observer) == false)
                    throw new ArgumentException("Trying to highlight not attached observer!");
                observer.UpdateHighlightMode(state);
            }
        }
    }

    public abstract class LinearPiece(bool isWhite) : Piece(isWhite)
    {
        public abstract (int, int)[] Directions();

        protected List<Cell> GetLinearMoves((int dx, int dy)[] directions)
        {
            List<Cell> moves = [];
            Point current = currentCell.Position;

            foreach (var (dx, dy) in directions)
            {
                int x = current.X + dx;
                int y = current.Y + dy;

                while (IsInBounds(x, y))
                {
                    Cell target = currentCell.Board.BoardCells[x, y];

                    if (!target.IsOccupied())
                    {
                        moves.Add(target);
                    }
                    else
                    {
                        if (target.Piece.IsWhite != this.IsWhite)
                        {
                            moves.Add(target); // можлива атака
                        }

                        break; // зустріли фігуру — далі не йдемо
                    }

                    x += dx;
                    y += dy;
                }
            }

            return moves;
        }
    }

    public class Pawn(bool isWhite) : Piece(isWhite)
    {
        protected override List<Cell> CalculatePossibleMoves()
        {
            List<Cell> moves = [];

            int direction = IsWhite ? -1 : 1;
            int startRow = IsWhite ? 6 : 1;

            Point current = currentCell.Position;

            int nextRow = current.Y + direction;

            if (IsInBounds(current.X, nextRow) &&
                currentCell.Board.BoardCells[current.X, nextRow].IsOccupied() == false)
            {
                moves.Add(currentCell.Board.BoardCells[current.X, nextRow]);

                if (current.Y == startRow &&
                    currentCell.Board.BoardCells[current.X, current.Y + 2 * direction].IsOccupied() == false)
                {
                    moves.Add(currentCell.Board.BoardCells[current.X, current.Y + 2 * direction]);
                }
            }

            foreach (int dx in new[] { -1, 1 })
            {
                int newX = current.X + dx;
                int newY = current.Y + direction;
                if (IsInBounds(newX, newY))
                {
                    Cell targetCell = currentCell.Board.BoardCells[newX, newY];
                    if (targetCell.IsOccupied() && targetCell.Piece.IsWhite != IsWhite)
                    {
                        moves.Add(targetCell);
                    }
                }
            }

            return moves;
        }

        public override Piece Clone()
        {
            return new Pawn(IsWhite);
        }
    }

    public class Knight(bool isWhite) : Piece(isWhite)
    {
        protected override List<Cell> CalculatePossibleMoves()
        {
            List<Cell> moves = [];
            Point current = currentCell.Position;

            int[,] offsets =
            {
                { -2, -1 }, { -2, 1 }, { -1, -2 }, { -1, 2 },
                { 1, -2 }, { 1, 2 }, { 2, -1 }, { 2, 1 }
            };

            for (int i = 0; i < offsets.GetLength(0); i++)
            {
                int newX = current.X + offsets[i, 0];
                int newY = current.Y + offsets[i, 1];

                if (IsInBounds(newX, newY))
                {
                    Cell target = currentCell.Board.BoardCells[newX, newY];
                    if (!target.IsOccupied() || target.Piece.IsWhite != this.IsWhite)
                    {
                        moves.Add(target);
                    }
                }
            }

            return moves;
        }

        public override Piece Clone()
        {
            return new Knight(IsWhite);
        }
    }

    public class Rook(bool isWhite) : LinearPiece(isWhite)
    {
        public override (int, int)[] Directions()
        {
            return [(0, 1), (0, -1), (1, 0), (-1, 0)];
        }

        protected override List<Cell> CalculatePossibleMoves()
        {
            return GetLinearMoves(Directions());
        }

        public override Piece Clone()
        {
            return new Rook(IsWhite);
        }
    }

    public class Bishop(bool isWhite) : LinearPiece(isWhite)
    {
        public override (int, int)[] Directions()
        {
            return [(1, 1), (1, -1), (-1, 1), (-1, -1)];
        }

        protected override List<Cell> CalculatePossibleMoves()
        {
            return GetLinearMoves(Directions());
        }

        public override Piece Clone()
        {
            return new Bishop(IsWhite);
        }
    }

    public class Queen(bool isWhite) : LinearPiece(isWhite)
    {
        public override (int, int)[] Directions()
        {
            return
            [
                (0, 1), (0, -1), (1, 0), (-1, 0),
                (1, 1), (1, -1), (-1, 1), (-1, -1)
            ];
        }

        protected override List<Cell> CalculatePossibleMoves()
        {
            return GetLinearMoves(Directions());
        }

        public override Piece Clone()
        {
            return new Queen(IsWhite);
        }
    }

    public class King(bool isWhite) : Piece(isWhite)
    {
        public readonly (int, int)[] Directions =
            [(0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1)];

        protected override List<Cell> CalculatePossibleMoves()
        {
            List<Cell> moves = [];
            Point current = currentCell.Position;

            foreach (var (dx, dy) in Directions)
            {
                int x = current.X + dx;
                int y = current.Y + dy;

                if (!IsInBounds(x, y))
                    continue;

                Cell target = currentCell.Board.BoardCells[x, y];

                if (IsCanBePlacedOnCell(target) == false)
                    continue;

                if (!target.IsOccupied())
                {
                    moves.Add(target);
                }
                else
                {
                    if (target.Piece.IsWhite != this.IsWhite)
                    {
                        moves.Add(target);
                    }
                }
            }

            return moves;
        }

        public override Piece Clone()
        {
            return new King(IsWhite);
        }

        private bool IsCanBePlacedOnCell(Cell targetCell)
        {
            return GetAttackingPieces(targetCell).Count == 0;
        }

        private List<Piece> GetAttackingPieces(Cell cell)
        {
            List<Piece> pieces = new List<Piece>();
            foreach ((Piece attackedBy, Cell attackedCell) in BoardManipulations.GetAttackedCells(
                         currentCell.Board.BoardCells, !IsWhite))
            {
                if (attackedCell == cell)
                    pieces.Add(attackedBy);
            }

            return pieces;
        }

        public bool IsChecked()
        {
            return IsCanBePlacedOnCell(currentCell) == false;
        }

        public List<(Piece defender, Cell to)> KingDefenders()
        {
            List<Piece> attackingPieces = GetAttackingPieces(currentCell);
            List<(Piece defender, Cell to)> defendingPieces = new();

            if (attackingPieces.Count != 1)
                return defendingPieces;

            Piece attacker = attackingPieces[0];
            List<Cell> pathToBlock = new List<Cell>();

            if (attacker is Knight or Pawn)
            {
                pathToBlock.Add(attacker.CurrentCell);
            }
            else if (attacker is LinearPiece)
            {
                int dx = Math.Sign(currentCell.Position.X - attacker.CurrentCell.Position.X);
                int dy = Math.Sign(currentCell.Position.Y - attacker.CurrentCell.Position.Y);
                int x = attacker.CurrentCell.Position.X + dx;
                int y = attacker.CurrentCell.Position.Y + dy;

                while ((x, y) != (currentCell.Position.X, currentCell.Position.Y))
                {
                    pathToBlock.Add(currentCell.Board.BoardCells[x, y]);
                    x += dx;
                    y += dy;
                }

                pathToBlock.Add(attacker.CurrentCell);
            }

            foreach ((Piece piece, Cell move) in BoardManipulations.GetAllMoves(currentCell.Board, IsWhite))
            {
                if (pathToBlock.Contains(move))
                    defendingPieces.Add((piece, move));
            }

            return defendingPieces;
        }

        public bool IsCheckmated()
        {
            List<Piece> attackingPieces = GetAttackingPieces(currentCell);

            if (IsChecked() && PossibleMoves.Count == 0)
            {
                if (attackingPieces.Count > 1)
                    return true;

                if (attackingPieces.Count == 1 && KingDefenders().Count != 0)
                    return false;
                
                return true;
            }

            return false;
        }
    }

    public class Bot
    {
        private int EvaluateBoard(Board board)
        {
            int score = 0;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    var piece = board.BoardCells[x, y].Piece;
                    if (piece == null) continue;

                    int value = piece switch
                    {
                        Pawn => 1,
                        Knight => 3,
                        Bishop => 3,
                        Rook => 5,
                        Queen => 9,
                        King => -1,
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    score += piece.IsWhite ? -value : value; // бот — чорний
                }
            }

            return score;
        }

        public (Piece piece, Cell move) FindBestBotMove(Board originalBoard)
        {
            int bestScore = int.MinValue;
            List<(Piece, Cell)> possibleMoves = BoardManipulations.GetAllMoves(originalBoard, false); ;
            (Piece piece, Cell move) bestMove = (null, null);

            King king = BoardManipulations.GetKing(originalBoard, false);

            if (king.IsChecked())
            {
                possibleMoves = king.KingDefenders();
                foreach (Cell move in king.PossibleMoves)
                {
                    possibleMoves.Add((king, move));
                }
            }


            foreach ((Piece piece, Cell move) in possibleMoves)
            {
                // Клонуємо
                Board boardClone = BoardManipulations.CloneBoard(originalBoard);

                // Знаходимо piece-копію і move-копію
                Cell from = boardClone.BoardCells[piece.CurrentCell.Position.X, piece.CurrentCell.Position.Y];
                Cell to = boardClone.BoardCells[move.Position.X, move.Position.Y];

                from.TryMovePieceTo(to);

                // Найкраща відповідь гравця
                int worstResponse = int.MaxValue;
                foreach ((Piece playerPiece, Cell playerMove) in BoardManipulations.GetAllMoves(boardClone, true))
                {
                    Board simBoard = BoardManipulations.CloneBoard(boardClone);
                    Cell pFrom = simBoard.BoardCells[playerPiece.CurrentCell.Position.X,
                        playerPiece.CurrentCell.Position.Y];
                    Cell pTo = simBoard.BoardCells[playerMove.Position.X, playerMove.Position.Y];

                    pFrom.TryMovePieceTo(pTo);

                    int score = EvaluateBoard(simBoard);
                    if (score < worstResponse)
                        worstResponse = score;
                }

                // Мінімізуємо найгірший варіант
                if (worstResponse > bestScore)
                {
                    bestScore = worstResponse;
                    bestMove = (piece, move);
                }
            }

            return bestMove;
        }
    }

    public abstract class BoardManipulations
    {
        public static List<(Piece, Cell)> GetAllMoves(Board board, bool isWhite)
        {
            var moves = new List<(Piece, Cell)>();

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Cell cell = board.BoardCells[x, y];
                    if (cell.IsOccupied() && cell.Piece.IsWhite == isWhite)
                    {
                        var piece = cell.Piece;
                        foreach (var move in piece.PossibleMoves)
                        {
                            moves.Add((piece, move));
                        }
                    }
                }
            }

            return moves;
        }

        public static List<(Piece attackedBy, Cell attackedCell)> GetAttackedCells(Cell[,] board, bool isWhite)
        {
            var cells = new List<(Piece, Cell)>();

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Cell cell = board[x, y];
                    if (!cell.IsOccupied() || cell.Piece.IsWhite != isWhite)
                        continue;

                    Piece piece = cell.Piece;

                    if (piece is Pawn)
                    {
                        int direction = isWhite ? -1 : 1;
                        foreach (int dx in new[] { -1, 1 })
                        {
                            int tx = cell.Position.X + dx;
                            int ty = cell.Position.Y + direction;
                            if (IsInBounds(tx, ty))
                                cells.Add((piece, board[tx, ty]));
                        }
                    }
                    else if (piece is Knight)
                    {
                        foreach (Cell target in piece.PossibleMoves)
                        {
                            cells.Add((piece, target));
                        }
                    }
                    else if (piece is LinearPiece linearPiece)
                    {
                        var directions = linearPiece.Directions();

                        foreach (var (dx, dy) in directions)
                        {
                            int cx = x + dx;
                            int cy = y + dy;
                            while (IsInBounds(cx, cy))
                            {
                                var target = board[cx, cy];
                                cells.Add((piece, target));

                                if (target.IsOccupied() && target.Piece is King == false)
                                    break;

                                cx += dx;
                                cy += dy;
                            }
                        }
                    }
                    else if (piece is King king)
                    {
                        foreach (var (dx, dy) in king.Directions)
                        {
                            int tx = x + dx;
                            int ty = y + dy;
                            if (IsInBounds(tx, ty))
                                cells.Add((king, board[tx, ty]));
                        }
                    }
                }
            }

            return cells;

            bool IsInBounds(int x, int y) => x is >= 0 and < 8 && y is >= 0 and < 8;
        }

        public static Board CloneBoard(Board board)
        {
            Cell[,] clonedBoardCells = new Cell[8, 8];
            Board clonedBoard = new Board(clonedBoardCells, board.Strategy);
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    clonedBoardCells[x, y] = new Cell(new Point(x, y), clonedBoard);
                }
            }

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Cell original = board.BoardCells[x, y];
                    if (original.IsOccupied())
                    {
                        Piece clonedPiece = original.Piece.Clone();
                        clonedBoardCells[x, y].PlacePiece(clonedPiece);
                    }
                }
            }

            return clonedBoard;
        }

        public static King GetKing(Board board, bool isWhite)
        {
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (board.BoardCells[x, y].IsOccupied())
                    {
                        Piece piece = board.BoardCells[x, y].Piece;

                        if (piece is King king && king.IsWhite == isWhite)
                            return king;
                    }
                }
            }

            string col = isWhite ? "White" : "Black";
            throw new NullReferenceException($"{col} king doesn't exist");
        }
    }
}