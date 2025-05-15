using System;
using System.Collections.Generic;
using System.Drawing;
using Chess;

namespace GameModel
{
    public class Board(Cell[,] boardCells)
    {
        public Cell[,] BoardCells => boardCells;
    }

    public class Cell(Point position, Cell[,] board)
    {
        public class OnPiecePlacedEventArgs : EventArgs
        {
            public Piece Piece;
        }

        public event EventHandler<OnPiecePlacedEventArgs> OnPieceChanged;
        public event Action<bool> OnCellHighlighted;

        public bool IsWhite => (position.X + position.Y) % 2 == 0;
        public Point Position => position;
        public Cell[,] Board => board;
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
                throw new NullReferenceException();
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
        private bool isKingChecked;

        public Piece CurrentSelected { get; private set; }

        private PieceSelection()
        {
        }

        public void Select(Piece piece)
        {
            if(isKingChecked && piece is King == false)
                return;

            if (CurrentSelected != null && CurrentSelected != piece)
            {
                CurrentSelected.UpdateObservers(false);
            }

            CurrentSelected = piece;
            CurrentSelected.AttachObservers();
            CurrentSelected.UpdateObservers(true);
            
        }

        public void KingChecked()
        {
            isKingChecked = true;
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
                    Cell target = currentCell.Board[x, y];

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
            if (observers == null)
                return;

            foreach (Cell observer in observers)
            {
                observer.UpdateHighlightMode(state);
            }
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

            if (IsInBounds(current.X, nextRow) && currentCell.Board[current.X, nextRow].IsOccupied() == false)
            {
                moves.Add(currentCell.Board[current.X, nextRow]);

                if (current.Y == startRow &&
                    currentCell.Board[current.X, current.Y + 2 * direction].IsOccupied() == false)
                {
                    moves.Add(currentCell.Board[current.X, current.Y + 2 * direction]);
                }
            }

            foreach (int dx in new[] { -1, 1 })
            {
                int newX = current.X + dx;
                int newY = current.Y + direction;
                if (IsInBounds(newX, newY))
                {
                    Cell targetCell = currentCell.Board[newX, newY];
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
                    Cell target = currentCell.Board[newX, newY];
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

    public class Rook(bool isWhite) : Piece(isWhite)
    {
        protected override List<Cell> CalculatePossibleMoves()
        {
            return GetLinearMoves([(0, 1), (0, -1), (1, 0), (-1, 0)]);
        }

        public override Piece Clone()
        {
            return new Rook(IsWhite);
        }
    }

    public class Bishop(bool isWhite) : Piece(isWhite)
    {
        protected override List<Cell> CalculatePossibleMoves()
        {
            return GetLinearMoves([(1, 1), (1, -1), (-1, 1), (-1, -1)]);
        }

        public override Piece Clone()
        {
            return new Bishop(IsWhite);
        }
    }

    public class Queen(bool isWhite) : Piece(isWhite)
    {
        protected override List<Cell> CalculatePossibleMoves()
        {
            return GetLinearMoves([
                (0, 1), (0, -1), (1, 0), (-1, 0),
                (1, 1), (1, -1), (-1, 1), (-1, -1)
            ]);
        }

        public override Piece Clone()
        {
            return new Queen(IsWhite);
        }
    }

    public class King(bool isWhite) : Piece(isWhite)
    {

        public event EventHandler OnKingChecked;
        public event EventHandler OnKingMated;

        private readonly (int, int)[] directions =
            [(0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1)];

        protected override List<Cell> CalculatePossibleMoves()
        {
            List<Cell> moves = [];
            Point current = currentCell.Position;


            foreach (var (dx, dy) in directions)
            {
                int x = current.X + dx;
                int y = current.Y + dy;

                if (!IsInBounds(x, y))
                    continue;

                Cell target = currentCell.Board[x, y];

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
            foreach (Cell attackedCell in BoardManipulations.GetAttackedCells(currentCell.Board, !IsWhite, true))
            {
                if (attackedCell == targetCell)
                    return false;
            }

            return true;
        }

        public bool IsChecked()
        {
            OnKingChecked?.Invoke(this, EventArgs.Empty);
            return IsCanBePlacedOnCell(currentCell) == false;
            
        }

        public bool IsCheckmated()
        {
            if (IsChecked() && PossibleMoves.Count == 0)
            {
                OnKingMated?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }
    }

    public class Bot
    {
        private int EvaluateBoard(Cell[,] board)
        {
            int score = 0;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    var piece = board[x, y].Piece;
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

        public (Piece piece, Cell move) FindBestBotMove(Cell[,] originalBoard)
        {
            int bestScore = int.MinValue;
            (Piece piece, Cell move) bestMove = (null, null);

            foreach ((Piece piece, Cell move) in BoardManipulations.GetAllMoves(originalBoard, false))
            {
                // Клонуємо
                var boardClone = BoardManipulations.CloneBoard(originalBoard);

                // Знаходимо piece-копію і move-копію
                Cell from = boardClone[piece.CurrentCell.Position.X, piece.CurrentCell.Position.Y];
                Cell to = boardClone[move.Position.X, move.Position.Y];

                from.TryMovePieceTo(to);

                // Найкраща відповідь гравця
                int worstResponse = int.MaxValue;
                foreach ((Piece playerPiece, Cell playerMove) in BoardManipulations.GetAllMoves(boardClone, true))
                {
                    Cell[,] simBoard = BoardManipulations.CloneBoard(boardClone);
                    Cell pFrom = simBoard[playerPiece.CurrentCell.Position.X, playerPiece.CurrentCell.Position.Y];
                    Cell pTo = simBoard[playerMove.Position.X, playerMove.Position.Y];

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
        public static List<(Piece, Cell)> GetAllMoves(Cell[,] board, bool isWhite)
        {
            var moves = new List<(Piece, Cell)>();

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Cell cell = board[x, y];
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

        public static List<Cell> GetAttackedCells(Cell[,] board, bool isWhite, bool ignoreOppositeKing)
        {
            var cells = new List<Cell>();

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
                                cells.Add(board[tx, ty]);
                        }
                    }
                    else if (piece is Knight)
                    {
                        cells.AddRange(piece.PossibleMoves);
                    }
                    else if (piece is Rook or Bishop or Queen)
                    {
                        var directions = piece switch
                        {
                            Rook => [(0, 1), (0, -1), (1, 0), (-1, 0)],
                            Bishop => [(1, 1), (1, -1), (-1, 1), (-1, -1)],
                            Queen => new[] { (0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1) },
                            _ => []
                        };

                        foreach (var (dx, dy) in directions)
                        {
                            int cx = x + dx;
                            int cy = y + dy;
                            while (IsInBounds(cx, cy))
                            {
                                var target = board[cx, cy];
                                cells.Add(target);

                                if (target.IsOccupied() && target.Piece is King == false)
                                    break;

                                cx += dx;
                                cy += dy;
                            }
                        }
                    }
                    else if (piece is King)
                    {
                        foreach (var (dx, dy) in new[] {
                    (0, 1), (0, -1), (1, 0), (-1, 0),
                    (1, 1), (1, -1), (-1, 1), (-1, -1)
                })
                        {
                            int tx = x + dx;
                            int ty = y + dy;
                            if (IsInBounds(tx, ty))
                                cells.Add(board[tx, ty]);
                        }
                    }
                }
            }

            return cells;

            bool IsInBounds(int x, int y) => x is >= 0 and < 8 && y is >= 0 and < 8;
        }

        public static Cell[,] CloneBoard(Cell[,] board)
        {
            var clonedBoard = new Cell[8, 8];
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    clonedBoard[x, y] = new Cell(new Point(x, y), clonedBoard);
                }
            }

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    Cell original = board[x, y];
                    if (original.IsOccupied())
                    {
                        Piece clonedPiece = original.Piece.Clone();
                        clonedBoard[x, y].PlacePiece(clonedPiece);
                    }
                }
            }

            return clonedBoard;
        }

        public static King GetKing(Cell[,] board, bool isWhite)
        {
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (board[x, y].IsOccupied())
                    {
                        Piece piece = board[x, y].Piece;

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