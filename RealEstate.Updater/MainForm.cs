using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace RealEstate.Updater
{
    public partial class MainForm : Form
    {
        private readonly GithubProxy _github;
        private readonly FileManager _fileManager;
        private readonly BackgroundWorker _updateWorker;
        private readonly BackgroundWorker _loadWorker;

        public MainForm()
        {
            InitializeComponent();
            _github = new GithubProxy();
            _fileManager = new FileManager();
            _updateWorker = new BackgroundWorker();
            _updateWorker.DoWork += _worker_DoWork;
            _loadWorker = new BackgroundWorker();
            _loadWorker.DoWork += _loadWorker_DoWork;
        }

        void _loadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            AppendLine("Получаю номер последней версии....");
            try
            {
                var vers = GithubProxy.GetAviableVersion();
                var vers1 = vers;
                BeginInvoke((Action)(() => { lAviable.Text = vers1; }));
                AppendLine("Готово");

                vers = FileManager.GetCurentVersion();
                BeginInvoke((Action)(() => { lCurrent.Text = vers; }));
                AppendLine("Текущей номер версии: " + lCurrent.Text);

                if (lCurrent.Text != lAviable.Text)
                {
                    BeginInvoke((Action)(() => { bUpdate.Enabled = true; }));
                }
            }
            catch (Exception ex)
            {
                AppendLine(ex.Message);
            }

            BeginInvoke((Action)(() => { prgrsBar.Style = ProgressBarStyle.Continuous; }));
        }

        void _worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BeginInvoke((Action)(() =>{ prgrsBar.Value = 0; }));
            try
            {
                AppendLine("Начинаю обновление....");
                var status = GithubProxy.GetProgramFile();
                AppendLine("Файл загружен. Распаковка....");
                var list = _fileManager.Restore(status);
                AppendLine("Файл обновления успешно загружен.");

                BeginInvoke((Action)(() => { prgrsBar.Maximum = list.Count; }));

                foreach (var file in list)
                {
                    try
                    {
                        if (File.Exists(file.Path))
                        {
                            if (FileManager.MD5HashFile(file.Path) != file.Hash)
                            {
                                AppendLine("Файл " + file.Path + " изменён. Скачиваю...");
                                _github.DownloadFile(file.Path);
                                AppendLine("Скачено");
                            }
                        }
                        else
                        {
                            AppendLine("Новый файл: " + file.Path + ". Скачиваю...");
                            _github.DownloadFile(file.Path);
                            AppendLine("Скачено");
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLine("Ошибка обработки файла: " + file.Path);
                        AppendLine(ex.ToString());
                    }

                    BeginInvoke((Action)(() => prgrsBar.PerformStep()));
                }
            }
            catch (Exception ex)
            {
                AppendLine(ex.ToString());
                return;
            }

            AppendLine("Обновление завершено");
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            prgrsBar.Style = ProgressBarStyle.Marquee;
            _loadWorker.RunWorkerAsync();
        }

        private void bUpdate_Click(object sender, EventArgs e)
        {
            _updateWorker.RunWorkerAsync();
        }

        private void пересоздатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                AppendLine("Генерирую статус-файл....");
                if (_fileManager.GenerateFile())
                    AppendLine("Генерация завершена");
            }
            catch (Exception ex)
            {
                AppendLine(ex.ToString());
            }
        }

        private void AppendLine(string text)
        {
            BeginInvoke((Action)(() =>
            {
                tbStatus.Text += string.Format("\r\n{0}", text);
                tbStatus.SelectionStart = tbStatus.Text.Length;
                tbStatus.ScrollToCaret();
                tbStatus.Refresh();
            }));
        }
    }
}
