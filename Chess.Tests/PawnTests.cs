using System.Drawing;
using Chess.Core;

namespace Chess.Tests
{
    public class PawnTests
    {
        [Fact]
        public void Pawn_HasTwoMoves_FromInitialPosition()
        {
            
            // Arrange
            var board = new Board(new PvPStrategy());
            var pawn = new Pawn(true);
            board.PlacePiece(0, 6, pawn); // e2

            // Act
            var moves = pawn.PossibleMoves;

            // Assert
            Assert.Equal(new Point(0, 5), moves[0].Position); // e3
            Assert.Equal(new Point(0, 4), moves[0].Position); // e4
            Assert.Equal(2, moves.Count);
            
        }
    }
}