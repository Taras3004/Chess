using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess.Core.CustomEventArgs
{
    public class GameInitializedEventArgs : EventArgs
    {
        public Board Board { get; }

        public GameInitializedEventArgs(Board board)
        {
            Board = board;
        }
    }
}
