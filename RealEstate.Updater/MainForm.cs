using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RealEstate.Updater
{
    public partial class MainForm : Form
    {
        private readonly GithubProxy _github;
        private readonly FileManager _fileManager;

        public MainForm()
        {
            InitializeComponent();
            _github = new GithubProxy();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void bUpdate_Click(object sender, EventArgs e)
        {

        }

        private void пересоздатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                AppendLine("Генерирую статус-файл....");
                if(_fileManager.GenerateFile())
                    AppendLine("Генерация завершена");
            }
            catch (Exception ex)
            {
                AppendLine(ex.ToString());
            }
        }

        private void AppendLine(string text)
        {
            tbStatus.Text += "\r\n" + text;
        }
    }
}
