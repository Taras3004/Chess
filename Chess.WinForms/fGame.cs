using Chess.Core;
using GameVisual;

namespace Chess
{
    public partial class fGame : Form
    {
        private readonly Menu menuForm;

        private GameUI gameUI;

        public fGame(Menu mForm, IGameModeStrategy strategy)
        {
            InitializeComponent();
            menuForm = mForm;

            gameUI = new GameUI(strategy, Controls);
            gameUI.StartGame();
        }


        private void Form1_Click(object sender, EventArgs e)
        {
            PieceSelection.Instance.Deselect();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            gameUI.EndGame();
            this.Dispose();
            menuForm.Show();
        }

        private void fGame_Load(object sender, EventArgs e)
        {

        }
    }
}
