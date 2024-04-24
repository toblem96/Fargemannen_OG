using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fargemannen
{

    public class ProgressWindow : Form
    {
        private ProgressBar progressBar;

        public ProgressWindow(int maxProgress)
        {
            progressBar = new ProgressBar();
            progressBar.Dock = DockStyle.Fill;
            progressBar.Maximum = maxProgress;
            progressBar.Step = 1;
            this.Controls.Add(progressBar);






            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 500;
            this.Height = 100;
            this.Show();
        }

        public void UpdateProgress(int progress)
        {
            if (this.InvokeRequired)  // Sikrer at UI oppdateringer skjer på riktig tråd
            {
                this.Invoke(new Action<int>(UpdateProgress), new object[] { progress });
                return;
            }
            progressBar.Value = progress;
            progressBar.Refresh();
        }

        public void Complete()
        {
            this.Close(); // Lukker formen når alt er ferdig
        }
    }
}