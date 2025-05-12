using System;
using System.Collections.Generic;
using System.Drawing;

namespace GameModel
{

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

        private void RemovePiece()
        {
            if(Piece == null)
                throw new NullReferenceException();
            Piece = null;

            OnPieceChanged?.Invoke(this, new OnPiecePlacedEventArgs
            {
                Piece = null
            });
        }
        
        public void TryMovePieceTo(Cell cell)
        {
            if (Piece == null)
                throw new NullReferenceException();
            if(Piece.CanMoveTo(cell) == false)
                return;
            if(cell.Piece != null && cell.Piece.IsWhite != Piece.IsWhite)
                cell.RemovePiece();

            cell.PlacePiece(Piece);
            RemovePiece();
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

    public abstract class Piece(bool isWhite)
    {
        private readonly List<Cell> observers = [];
        protected Cell currentCell;

        public bool IsWhite { get; } = isWhite;
        public Cell CurrentCell => currentCell;

        protected abstract List<Cell> CalculatePossibleMoves();

        public bool CanMoveTo(Cell cell)
        {
            return CalculatePossibleMoves().Contains(cell);
        }

        public void SetCurrentCell(Cell cell)
        {
            currentCell = cell;
            
            UpdateObservers();
        }

        public void UpdateObservers()
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

        private void DetachAllObservers()
        {
            foreach (var observer in observers)
            {
                observer.UpdateHighlightMode(false);
            }
            observers.Clear();
        }

        public void UpdateObservers(bool state)
        {
            if(observers == null)
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
            List<Cell> moves = new();

            int direction = IsWhite ? -1 : 1;
            int startRow = IsWhite ? 6 : 1;

            Point current = currentCell.Position;

            int nextRow = current.Y + direction;

            // Простий хід вперед
            if (IsInBounds(current.X, nextRow) && currentCell.Board[current.X, nextRow].IsOccupied() == false)
            {
                moves.Add(currentCell.Board[current.X, nextRow]);

                // Перший хід — 2 клітинки
                if (current.Y == startRow && currentCell.Board[current.X, current.Y + 2 * direction].IsOccupied() == false)
                {
                    moves.Add(currentCell.Board[current.X, current.Y + 2 * direction]);
                }
            }

            // Атака по діагоналі
            foreach (int dx in new[] { -1, 1 })
            {
                int newX = current.X + dx;
                int newY = current.Y + direction;
                if (IsInBounds(newX, newY))
                {
                    Cell targetCell = currentCell.Board[newX, newY];
                    if (targetCell.IsOccupied() && targetCell.Piece.IsWhite != this.IsWhite)
                    {
                        moves.Add(targetCell);
                    }
                }
            }

            return moves;
        }

        private bool IsInBounds(int x, int y)
        {
            return x is >= 0 and < 8 && y is >= 0 and < 8;
        }
    }

    public class Knight(bool isWhite) : Piece(isWhite)
    {

        protected override List<Cell> CalculatePossibleMoves()
        {
            throw new NotImplementedException();
        }
    }
}
