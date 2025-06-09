using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Chess;

namespace GameModel
{
    public class Board
    {
        public Cell[,] BoardCells { get; private set; }

        public event EventHandler<PieceEventArgs> OnKingChecked;
        public event EventHandler<PieceEventArgs> OnKingMated;
        public event EventHandler OnStalemateHappened;
        public event EventHandler<PieceEventArgs> OnPawnPromoted;


        public Pawn LastPawnDoubleMove { get; private set; } = null;

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

            Strategy.OnPieceMoved += Strategy_OnPieceMoved;
        }

        private void Strategy_OnPieceMoved(object sender, PieceMovedEventArgs e)
        {
            PieceSelection.Instance.Deselect();
            Piece movedPiece = e.Piece;
            Cell from = e.From;
            if (movedPiece is Pawn pawn)
            {
                LastPawnDoubleMove = Math.Abs(pawn.CurrentCell.Position.Y - from.Position.Y) == 2 ? pawn : null;

                CheckPawnPromotion(pawn);

                if (pawn.EnPassant != null && pawn.CurrentCell.Position.X == pawn.EnPassant.Position.X)
                {
                    pawn.EnPassant.RemovePiece();
                }
            }
            else
                LastPawnDoubleMove = null;

            if (movedPiece is King king)
            {
                king.Moved();
                CheckCastling(king, from);
            }
            else if (movedPiece is Rook rook)
                rook.Moved();


            CheckKingProtection(movedPiece);
        }

        private void CheckPawnPromotion(Pawn pawn)
        {
            int endRow = pawn.IsWhite ? 0 : 7;
            if (pawn.CurrentCell.Position.Y == endRow)
            {
                OnPawnPromoted?.Invoke(this, new PieceEventArgs { Piece = pawn });
            }
        }

        private void CheckKingProtection(Piece piece)
        {
            bool isWhitePieceMoved = piece.IsWhite;
            King king = BoardManipulations.GetKing(piece.CurrentCell.Board, !isWhitePieceMoved);

            if (king.IsCheckmated())
            {
                OnKingMated?.Invoke(this, new PieceEventArgs { Piece = king });
            }
            else if (BoardManipulations.GetAllMoves(piece.CurrentCell.Board, !piece.IsWhite).Count == 0)
            {
                OnStalemateHappened?.Invoke(this, EventArgs.Empty);
            }
            else if (king.IsChecked())
            {
                OnKingChecked?.Invoke(this, new PieceEventArgs { Piece = king });
            }
        }

        private void CheckCastling(King king, Cell from)
        {
            if (Math.Abs(from.Position.X - king.CurrentCell.Position.X) == 2)
            {
                int y = from.Position.Y;
                Cell[,] boardCells = king.CurrentCell.Board.BoardCells;

                if (king.CurrentCell.Position.X == 6) // kingside
                {
                    Cell rookFrom = boardCells[7, y];
                    Cell rookTo = boardCells[5, y];
                    rookTo.PlacePiece(rookFrom.Piece);
                    rookFrom.RemovePiece();
                    (rookTo.Piece as Rook)?.Moved();
                }
                else if (king.CurrentCell.Position.X == 2) // queenside
                {
                    Cell rookFrom = boardCells[0, y];
                    Cell rookTo = boardCells[3, y];
                    rookTo.PlacePiece(rookFrom.Piece);
                    rookFrom.RemovePiece();
                    (rookTo.Piece as Rook)?.Moved();
                }
            }
        }

        public void DeleteBoard()
        {
            BoardCells = null;
            Strategy.OnPieceMoved -= Strategy_OnPieceMoved;
        }
    }

    public class Cell(Point position, Board board)
    {
        public event EventHandler<PieceEventArgs> OnPieceChanged;
        public event Action<bool> OnCellHighlighted;

        public bool IsWhite => (position.X + position.Y) % 2 == 0;
        public Point Position => position;
        public Board Board => board;
        public Piece Piece { get; private set; }

        public void PlacePiece(Piece p)
        {
            OnPieceChanged?.Invoke(this, new PieceEventArgs
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

            OnPieceChanged?.Invoke(this, new PieceEventArgs
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
        public event EventHandler OnPieceDeselected;

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

                foreach ((Piece defender, Cell to) in possibleSelectedPieces)
                {
                    if (defender == piece)
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
                OnPieceDeselected?.Invoke(this, EventArgs.Empty);
                CurrentSelected.UpdateObservers(false);
                CurrentSelected = null;
            }
        }
    }

    public abstract class Piece(bool isWhite)
    {
        private readonly List<Cell> observers = [];
        private Cell currentCell;
        public bool IsWhite { get; } = isWhite;
        public Cell CurrentCell => currentCell;
        public List<Cell> PossibleMoves => CalculatePossibleMoves();

        public abstract Piece Clone();
        public abstract List<Cell> GetAllMoves();

        protected bool IsInBounds(int x, int y) => x is >= 0 and < 8 && y is >= 0 and < 8;

        private List<Cell> CalculatePossibleMoves()
        {
            return GetAllMoves().Where(cell =>
                (!cell.IsOccupied() || (cell.IsOccupied() && cell.Piece.IsWhite != IsWhite)) &&
                !IsKingUnderThreat(cell)).ToList();
        }

        public bool CanMoveTo(Cell cell)
        {
            return CalculatePossibleMoves().Contains(cell);
        }

        public void SetCurrentCell(Cell cell)
        {
            currentCell = cell;
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

        protected bool IsKingUnderThreat(Cell cell)
        {
            var clonedBoard = BoardManipulations.CloneBoard(cell.Board);

            King king = BoardManipulations.GetKing(clonedBoard, IsWhite);

            var fromCell = clonedBoard.BoardCells[currentCell.Position.X, currentCell.Position.Y];
            var toCell = clonedBoard.BoardCells[cell.Position.X, cell.Position.Y];

            bool isKing = fromCell.Piece == king;

            toCell.PlacePiece(fromCell.Piece);
            fromCell.RemovePiece();

            List<(Piece attackedBy, Cell attackedCell)> attackedCells =
                BoardManipulations.GetAttackedCells(clonedBoard.BoardCells, !IsWhite);

            if (isKing)
            {
                int row = IsWhite ? 7 : 0;

                if (fromCell.Position == new Point(4, row)) // check castling
                {
                    if (toCell.Position == new Point(6, row))
                    {
                        var pathCells = new[]
                        {
                            clonedBoard.BoardCells[5, row],
                            clonedBoard.BoardCells[6, row]
                        };

                        if (pathCells.Any(pathCell =>
                                attackedCells.Any(x => x.attackedCell.Position == pathCell.Position)))
                        {
                            clonedBoard.DeleteBoard();
                            return true;
                        }
                    }
                    else if (toCell.Position == new Point(2, row))
                    {
                        var pathCells = new[]
                        {
                            clonedBoard.BoardCells[3, row],
                            clonedBoard.BoardCells[2, row]
                        };

                        if (pathCells.Any(pathCell =>
                                attackedCells.Any(x => x.attackedCell.Position == pathCell.Position)))
                        {
                            clonedBoard.DeleteBoard();
                            return true;
                        }
                    }
                }

                foreach ((Piece attackedBy, Cell attackedCell) in attackedCells)
                {
                    if (attackedCell == king.currentCell)
                    {
                        clonedBoard.DeleteBoard();
                        return true;
                    }
                }
            }
            else
            {
                foreach ((Piece attackedBy, Cell attackedCell) in attackedCells)
                {
                    if (attackedCell == king.currentCell)
                    {
                        clonedBoard.DeleteBoard();
                        return true;
                    }
                }
            }

            clonedBoard.DeleteBoard();
            return false;
        }
    }

    public abstract class LinearPiece(bool isWhite) : Piece(isWhite)
    {
        protected abstract (int, int)[] Directions();

        public override List<Cell> GetAllMoves()
        {
            List<Cell> moves = new List<Cell>();

            foreach (var (dx, dy) in Directions())
            {
                int cx = CurrentCell.Position.X + dx;
                int cy = CurrentCell.Position.Y + dy;

                while (IsInBounds(cx, cy))
                {
                    Cell target = CurrentCell.Board.BoardCells[cx, cy];
                    moves.Add(target);

                    if (target.IsOccupied())
                        break;

                    cx += dx;
                    cy += dy;
                }
            }

            return moves;
        }
    }

    public class Pawn(bool isWhite) : Piece(isWhite)
    {
        public Cell EnPassant { get; private set; }

        public override List<Cell> GetAllMoves()
        {
            List<Cell> moves = [];

            int direction = IsWhite ? -1 : 1;
            int startRow = IsWhite ? 6 : 1;

            Point current = CurrentCell.Position;
            ;

            if (IsInBounds(current.X, current.Y + direction) &&
                CurrentCell.Board.BoardCells[current.X, current.Y + direction].IsOccupied() == false)
            {
                moves.Add(CurrentCell.Board.BoardCells[current.X, current.Y + direction]);

                if (current.Y == startRow &&
                    CurrentCell.Board.BoardCells[current.X, current.Y + 2 * direction].IsOccupied() == false)
                {
                    moves.Add(CurrentCell.Board.BoardCells[current.X, current.Y + 2 * direction]);
                }
            }

            Pawn doubleMovedPawn = CurrentCell.Board.LastPawnDoubleMove; // en passant

            if (doubleMovedPawn != null &&
                IsWhite != doubleMovedPawn.IsWhite &&
                doubleMovedPawn.CurrentCell.Position.Y == CurrentCell.Position.Y &&
                Math.Abs(doubleMovedPawn.CurrentCell.Position.X - CurrentCell.Position.X) == 1)
            {
                int dir = IsWhite ? -1 : 1;
                EnPassant = doubleMovedPawn.CurrentCell;
                moves.Add(CurrentCell.Board.BoardCells[EnPassant.Position.X, EnPassant.Position.Y + 1 * dir]);
            }

            foreach (int dx in new[] { -1, 1 })
            {
                int newX = current.X + dx;
                int newY = current.Y + direction;
                if (IsInBounds(newX, newY))
                {
                    Cell targetCell = CurrentCell.Board.BoardCells[newX, newY];
                    if (targetCell.IsOccupied())
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
        public override List<Cell> GetAllMoves()
        {
            List<Cell> moves = new List<Cell>();
            Point current = CurrentCell.Position;

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
                    moves.Add(CurrentCell.Board.BoardCells[newX, newY]);
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
        public bool HasMoved { get; private set; }

        public void Moved()
        {
            HasMoved = true;
        }

        protected override (int, int)[] Directions()
        {
            return [(0, 1), (0, -1), (1, 0), (-1, 0)];
        }

        public override Piece Clone()
        {
            return new Rook(IsWhite);
        }
    }

    public class Bishop(bool isWhite) : LinearPiece(isWhite)
    {
        protected override (int, int)[] Directions()
        {
            return [(1, 1), (1, -1), (-1, 1), (-1, -1)];
        }

        public override Piece Clone()
        {
            return new Bishop(IsWhite);
        }
    }

    public class Queen(bool isWhite) : LinearPiece(isWhite)
    {
        protected override (int, int)[] Directions()
        {
            return
            [
                (0, 1), (0, -1), (1, 0), (-1, 0),
                (1, 1), (1, -1), (-1, 1), (-1, -1)
            ];
        }

        public override Piece Clone()
        {
            return new Queen(IsWhite);
        }
    }

    public class King(bool isWhite) : Piece(isWhite)
    {
        private bool HasMoved { get; set; } = false;

        private readonly (int, int)[] directions =
            [(0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1)];

        public void Moved()
        {
            HasMoved = true;
        }

        public override List<Cell> GetAllMoves()
        {
            List<Cell> moves = new List<Cell>();
            Point current = CurrentCell.Position;

            foreach ((int dx, int dy) in directions)
            {
                int x = current.X + dx;
                int y = current.Y + dy;

                if (!IsInBounds(x, y))
                    continue;

                Cell target = CurrentCell.Board.BoardCells[x, y];

                moves.Add(target);
            }

            Cell[,] boardCells = CurrentCell.Board.BoardCells;

            if (!HasMoved && !IsChecked()) //castling
            {
                if (IsAvailable(new Point(current.X + 1, current.Y)) && // king side
                    IsAvailable(new Point(current.X + 2, current.Y)) &&
                    IsInBounds(current.X + 3, current.Y) &&
                    boardCells[current.X + 3, current.Y].IsOccupied() &&
                    boardCells[current.X + 3, current.Y].Piece is Rook { HasMoved: false })
                {
                    moves.Add(boardCells[current.X + 2, current.Y]);
                }

                if (IsAvailable(new Point(current.X - 1, current.Y)) && // queen side
                    IsAvailable(new Point(current.X - 2, current.Y)) &&
                    IsAvailable(new Point(current.X - 3, current.Y)) &&
                    IsInBounds(current.X - 4, current.Y) &&
                    boardCells[current.X - 4, current.Y].IsOccupied() &&
                    boardCells[current.X - 4, current.Y].Piece is Rook { HasMoved: false })
                {
                    moves.Add(boardCells[current.X - 2, current.Y]);
                }

                bool IsAvailable(Point cellPosition)
                {
                    return IsInBounds(cellPosition.X, cellPosition.Y) &&
                           !boardCells[cellPosition.X, cellPosition.Y].IsOccupied();
                }
            }

            return moves;
        }

        public override Piece Clone()
        {
            return new King(IsWhite);
        }

        private List<Piece> GetAttackingPieces(Cell cell)
        {
            List<Piece> pieces = new List<Piece>();
            foreach ((Piece attackedBy, Cell attackedCell) in BoardManipulations.GetAttackedCells(
                         CurrentCell.Board.BoardCells, !IsWhite))
            {
                if (attackedCell == cell)
                    pieces.Add(attackedBy);
            }

            return pieces;
        }

        public bool IsChecked()
        {
            return IsKingUnderThreat(CurrentCell);
        }

        public List<(Piece defender, Cell to)> KingDefenders()
        {
            List<Piece> attackingPieces = GetAttackingPieces(CurrentCell);
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
                int dx = Math.Sign(CurrentCell.Position.X - attacker.CurrentCell.Position.X);
                int dy = Math.Sign(CurrentCell.Position.Y - attacker.CurrentCell.Position.Y);
                int x = attacker.CurrentCell.Position.X + dx;
                int y = attacker.CurrentCell.Position.Y + dy;

                while ((x, y) != (CurrentCell.Position.X, CurrentCell.Position.Y))
                {
                    pathToBlock.Add(CurrentCell.Board.BoardCells[x, y]);
                    x += dx;
                    y += dy;
                }

                pathToBlock.Add(attacker.CurrentCell);
            }

            foreach ((Piece piece, Cell move) in BoardManipulations.GetAllMoves(CurrentCell.Board, IsWhite))
            {
                if (pathToBlock.Contains(move))
                    defendingPieces.Add((piece, move));
            }

            return defendingPieces;
        }

        public bool IsCheckmated()
        {
            if (IsChecked() == false)
                return false;

            List<Piece> attackingPieces = GetAttackingPieces(CurrentCell);

            if (PossibleMoves.Count == 0)
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
                    Piece piece = board.BoardCells[x, y].Piece;
                    if (piece == null) continue;

                    int value = piece switch
                    {
                        Pawn => 1,
                        Knight => 3,
                        Bishop => 3,
                        Rook => 5,
                        Queen => 9,
                        King => 100,
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
            Random rnd = new Random();

            List<(Piece, Cell)> possibleMoves = BoardManipulations.GetAllMoves(originalBoard, false);
            
            List<(Piece piece, Cell move)> bestMoves = new();

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
                Board boardClone = BoardManipulations.CloneBoard(originalBoard);

                Cell from = boardClone.BoardCells[piece.CurrentCell.Position.X, piece.CurrentCell.Position.Y];
                Cell to = boardClone.BoardCells[move.Position.X, move.Position.Y];

                from.TryMovePieceTo(to);

                const int depth = 3;
                int score = Minimax(boardClone, depth - 1, false);

                boardClone.DeleteBoard();


                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoves.Clear();
                    bestMoves.Add((piece, move));
                }
                else if (score == bestScore)
                {
                    bestMoves.Add((piece, move));
                }
            }

            return bestMoves[rnd.Next(bestMoves.Count)];
        }

        private int Minimax(Board board, int currentDepth, bool botTurn, int alpha = int.MinValue, int beta = int.MaxValue)
        {
            if (currentDepth == 0)
            {
                return EvaluateBoard(board);
            }

            int bestValue;

            if (botTurn)
            {
                bestValue = int.MinValue;

                var moves = BoardManipulations.GetAllMoves(board, false);

                foreach ((Piece piece, Cell move) in moves)
                {
                    Board clonedBoard = BoardManipulations.CloneBoard(board);
                    Cell fromCellClone = clonedBoard.BoardCells[piece.CurrentCell.Position.X, piece.CurrentCell.Position.Y];
                    Cell moveCellClone = clonedBoard.BoardCells[move.Position.X, move.Position.Y];

                    fromCellClone.TryMovePieceTo(moveCellClone);

                    int evaluateBoard = Minimax(clonedBoard, currentDepth - 1, false, alpha, beta);

                    bestValue = Math.Max(bestValue, evaluateBoard);
                    alpha = Math.Max(alpha, evaluateBoard);
                    clonedBoard.DeleteBoard();

                    if(beta <= alpha)
                        break;
                }
            }
            else
            {
                bestValue = int.MaxValue;

                var moves = BoardManipulations.GetAllMoves(board, true);

                foreach ((Piece piece, Cell move) in moves)
                {
                    Board clonedBoard = BoardManipulations.CloneBoard(board);
                    Cell fromCellClone = clonedBoard.BoardCells[piece.CurrentCell.Position.X, piece.CurrentCell.Position.Y];
                    Cell moveCellClone = clonedBoard.BoardCells[move.Position.X, move.Position.Y];

                    fromCellClone.TryMovePieceTo(moveCellClone);

                    int evaluateBoard = Minimax(clonedBoard, currentDepth - 1, true, alpha, beta);
                    
                    bestValue = Math.Min(bestValue, evaluateBoard);
                    beta = Math.Min(beta, evaluateBoard);
                    clonedBoard.DeleteBoard();

                    if (beta <= alpha)
                        break;
                }
            }

            return bestValue;
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
                        foreach (Cell move in piece.GetAllMoves())
                        {
                            if (move.Position.X != piece.CurrentCell.Position.X)
                                cells.Add((piece, move));
                        }

                        continue;
                    }

                    foreach (Cell move in piece.GetAllMoves())
                    {
                        cells.Add((piece, move));
                    }
                }
            }

            return cells;
        }

        public static Board CloneBoard(Board board) // DELETE BOARD AFTER USING!!!
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