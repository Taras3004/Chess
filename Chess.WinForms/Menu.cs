using Chess.Core;

namespace Chess
{
    public partial class Menu : Form
    {
        public Menu()
        {
            InitializeComponent();
        }



        private void button4_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                IGameModeStrategy strategy = button.Tag switch
                {
                    "PvP" => new PvPStrategy(),
                    "PvB" => new PvBStrategy(),
                    "Mod" => new ModelingStrategy(),
                    _ => new ModelingStrategy()
                };
                this.Hide();
                fGame gameForm = new fGame(this, strategy);
                gameForm.Show();
            }
        }
    }
}
