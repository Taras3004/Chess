using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chess.Core;

namespace Chess.Tests
{
    public class BoardTests
    {

        [Fact]
        public void PlacePiece_StoresPieceCorrectly()
        {
            var board = new Board(new PvPStrategy());
            var rook = new Rook(true);
            board.PlacePiece(0, 0, rook);

            var piece = board.BoardCells[0, 0].Piece;
            Assert.Equal(rook, piece);
        }

        [Fact]
        public void Clone_ProducesIndependentBoard()
        {
            var board = new Board(new PvPStrategy());
            var queen = new Queen(true);
            board.PlacePiece(0, 0, queen);

            var clone = BoardManipulations.CloneBoard(board);
            clone.BoardCells[0, 0].RemovePiece();

            Assert.NotNull(board.BoardCells[0, 0].Piece);
        }
    }
}
