using Chess.Core;

namespace Chess.Tests
{
    public class GameTests
    {
        [Fact]
        public void Strategy_SelectPiece_SelectOppositeColor()
        {
            var strategy = new PvPStrategy();
            Piece blackRook = new Rook(false);

            strategy.OnInitialized += (sender, e) =>
            {
                e.Board.PlacePiece(0, 0, blackRook);
            };

            var game = Game.Instance;
            game.StartGame(strategy);

            strategy.SelectPiece(blackRook);

            Assert.Null(PieceSelection.Instance.CurrentSelected);
        }

        [Fact]
        public void Strategy_SelectPiece_SelectCurrentColor()
        {
            var strategy = new PvPStrategy();
            Piece whiteRook = new Rook(true);

            strategy.OnInitialized += (sender, e) =>
            {
                e.Board.PlacePiece(0, 7, whiteRook);
            };

            var game = Game.Instance;
            game.StartGame(strategy);

            strategy.SelectPiece(whiteRook);

            Assert.Equal(whiteRook, PieceSelection.Instance.CurrentSelected);
        }

        [Fact]
        public void Strategy_MovePiece_SelectCurrentColor()
        {
            var strategy = new PvPStrategy();
            Piece whiteBishop = new Bishop(true);
            Board board = new Board(strategy);

            board.PlacePiece(0, 0, whiteBishop);
            strategy.MakeMove(whiteBishop, board.BoardCells[7, 7]);

            Assert.Equal(whiteBishop, board.BoardCells[7,7].Piece);
        }
    }
}
