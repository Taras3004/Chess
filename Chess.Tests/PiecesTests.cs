using System.Drawing;
using Chess.Core;

namespace Chess.Tests
{
    public class PiecesTests
    {
        [Fact]
        public void Pawn_HasTwoMoves_FromInitialPosition()
        {
            var board = new Board(new PvPStrategy());
            var pawn = new Pawn(true);
            board.PlacePiece(4, 6, pawn);

            var moves = pawn.PossibleMoves;

            Assert.Equal(2, moves.Count);
            Assert.Contains(moves, m => m.Position == new Point(4, 5));
            Assert.Contains(moves, m => m.Position == new Point(4, 4));
        }

        [Fact]
        public void Pawn_CannotMove_IfBlocked()
        {
            var board = new Board(new PvPStrategy());
            var pawn = new Pawn(true);
            var blocker = new Pawn(true);

            board.PlacePiece(4, 6, pawn);
            board.PlacePiece(4, 5, blocker);

            var moves = pawn.PossibleMoves;

            Assert.Empty(moves);
        }

        [Fact]
        public void Pawn_CanCaptureDiagonally()
        {
            var board = new Board(new PvPStrategy());
            var whitePawn = new Pawn(true);
            var blackPawn1 = new Pawn(false);
            var blackPawn2 = new Pawn(false);

            board.PlacePiece(4, 4, whitePawn);
            board.PlacePiece(3, 3, blackPawn1);
            board.PlacePiece(5, 3, blackPawn2);

            var moves = whitePawn.PossibleMoves;

            Assert.Contains(moves, m => m.Position == new Point(3, 3));
            Assert.Contains(moves, m => m.Position == new Point(5, 3));
        }

        [Fact]
        public void Pawn_CanEnPassant()
        {
            PvPStrategy strategy = new PvPStrategy();
            Board board = new Board(strategy);
            Pawn whitePawn = new Pawn(true);
            Pawn blackPawn = new Pawn(false);

            board.PlacePiece(2, 4, whitePawn);
            board.PlacePiece(1, 1, blackPawn);

            strategy.MakeMove(whitePawn, board.BoardCells[2, 3]);
            strategy.MakeMove(blackPawn, board.BoardCells[1, 3]);

            var moves = whitePawn.PossibleMoves;

            Assert.Contains(moves, m => m.Position == new Point(1, 2));
        }

        [Fact]
        public void Knight_HasEightMoves_InCenter()
        {
            var board = new Board(new PvPStrategy());
            var knight = new Knight(true);
            board.PlacePiece(4, 4, knight);

            var moves = knight.PossibleMoves;

            Assert.Equal(8, moves.Count);
        }

        [Fact]
        public void Bishop_StopsBeforeFriendlyPiece()
        {
            var board = new Board(new PvPStrategy());
            var bishop = new Bishop(true);
            var blocker = new Pawn(true);

            board.PlacePiece(2, 2, bishop);
            board.PlacePiece(4, 4, blocker);

            var moves = bishop.PossibleMoves;

            Assert.DoesNotContain(moves, m => m.Position == new Point(4, 4));
            Assert.Contains(moves, m => m.Position == new Point(3, 3));
        }

        [Fact]
        public void Rook_CannotPassThroughPieces()
        {
            var board = new Board(new PvPStrategy());
            var rook = new Rook(true);
            var blocker = new Pawn(true);

            board.PlacePiece(0, 0, rook);
            board.PlacePiece(0, 2, blocker);

            var moves = rook.PossibleMoves;

            Assert.DoesNotContain(moves, m => m.Position == new Point(0, 3));
        }

        [Fact]
        public void Queen_MovesProperly()
        {
            var board = new Board(new PvPStrategy());
            var queen = new Queen(true);

            board.PlacePiece(4, 4, queen);

            var moves = queen.PossibleMoves;

            Assert.Contains(moves, m => m.Position == new Point(0, 0)); // a1
            Assert.Contains(moves, m => m.Position == new Point(4, 0)); // e1
            Assert.Contains(moves, m => m.Position == new Point(7, 4)); // h5
        }

        [Fact]
        public void King_CannotMoveIntoCheck()
        {
            var board = new Board(new PvPStrategy());
            var king = new King(true);
            var enemyRook = new Rook(false);

            board.PlacePiece(4, 4, king);
            board.PlacePiece(4, 0, enemyRook);

            var moves = king.PossibleMoves;

            Assert.DoesNotContain(moves, m => m.Position == new Point(4, 5));
            Assert.DoesNotContain(moves, m => m.Position == new Point(4, 3));
        }

        [Fact]
        public void King_HasAllMoves_WhenSafe()
        {
            var board = new Board(new PvPStrategy());
            var king = new King(true);
            board.PlacePiece(4, 4, king);

            var moves = king.PossibleMoves;

            Assert.Equal(8, moves.Count);
        }
    }
}