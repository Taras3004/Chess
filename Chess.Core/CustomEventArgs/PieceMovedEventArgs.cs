namespace Chess.Core.CustomEventArgs
{
    public class PieceMovedEventArgs : EventArgs
    {
        public Piece Piece;
        public Cell From;
    }
}
