using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FhirProfilePublisher.Engine;
using System.IO;

namespace FhirProfilePublisher
{
    public partial class FhirProfilePublisherDialog : Form
    {
        public FhirProfilePublisherDialog()
        {
            InitializeComponent();

            LoadUserData();

            this.tbFileList.TextChanged += (sender, e) => SaveUserData();
            this.tbOutputPath.TextChanged += (sender, e) => SaveUserData();
            this.tbTemplateHtml.TextChanged += (sender, e) => SaveUserData();
            this.tbHeadingText.TextChanged += (sender, e) => SaveUserData();
            this.tbPageTitlePrefix.TextChanged += (sender, e) => SaveUserData();
            this.tbIndexPageHtml.TextChanged += (sender, e) => SaveUserData();
        }

        private void LoadUserData()
        {
            this.tbOutputPath.Text = Properties.Settings.Default.OutputPath;

            if (string.IsNullOrWhiteSpace(this.tbOutputPath.Text))
                this.tbOutputPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            this.tbFileList.Text = Properties.Settings.Default.InputFileList;

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.TemplateHTML))
                this.tbTemplateHtml.Text = Properties.Settings.Default.TemplateHTML;

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.PageTitleSuffix))
                this.tbPageTitlePrefix.Text = Properties.Settings.Default.PageTitleSuffix;

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.IndexPageHTML))
                this.tbIndexPageHtml.Text = Properties.Settings.Default.IndexPageHTML;

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.HeadingText))
                this.tbHeadingText.Text = Properties.Settings.Default.HeadingText;
        }

        private void SaveUserData()
        {
            Properties.Settings.Default.InputFileList = tbFileList.Text;
            Properties.Settings.Default.OutputPath = tbOutputPath.Text;
            Properties.Settings.Default.TemplateHTML = tbTemplateHtml.Text;
            Properties.Settings.Default.PageTitleSuffix = tbPageTitlePrefix.Text;
            Properties.Settings.Default.IndexPageHTML = tbIndexPageHtml.Text;
            Properties.Settings.Default.HeadingText = tbHeadingText.Text;
            Properties.Settings.Default.Save();
        }

        private void tbBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.Filter = "XML files (*.xml)|*.xml";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    AddFilesToFileList(dialog.FileNames);
            }
        }

        private void tbClear_Click(object sender, EventArgs e)
        {
            tbFileList.Clear();
        }

        private void tbFileList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) 
                e.Effect = DragDropEffects.Link;
        }

        private void tbFileList_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddFilesToFileList(files);
        }

        private void AddFilesToFileList(string[] files)
        {
            foreach (string file in files)
                if ((file ?? string.Empty).ToLower().EndsWith(".xml"))
                    if (!tbFileList.Lines.Contains(file))
                        tbFileList.Text += file + Environment.NewLine;

            if (string.IsNullOrEmpty(tbOutputPath.Text))
                if (tbFileList.Lines.Length > 0)
                    tbOutputPath.Text = Path.GetDirectoryName(tbFileList.Lines.First());
        }

        private void tbGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                this.Enabled = false;
                this.Refresh();

                TextContent content = new TextContent()
                {
                    HeaderText = tbHeadingText.Text,
                    PageTitleSuffix = tbPageTitlePrefix.Text,
                    FooterText = tbFootingText.Text,
                    IndexPageHtml = tbIndexPageHtml.Text,
                    PageTemplate = tbTemplateHtml.Text
                };

                HtmlGenerator generator = new HtmlGenerator();
                string htmlFilePath = generator.Generate(tbFileList.Lines, tbOutputPath.Text, content);

                if (cbOpenBrowser.Checked)
                    WebHelper.LaunchBrowser(htmlFilePath);
            }
            catch (ReferenceNotFoundException rnfe)
            {
                MessageBox.Show(this, 
                "Could not publish profiles because of the following error:" + Environment.NewLine + Environment.NewLine + rnfe.Message,
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
                this.Enabled = true;
            }
        }

        private void tbBrowseOutputPath_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (!string.IsNullOrEmpty(tbOutputPath.Text))
                    dialog.SelectedPath = tbOutputPath.Text;
                
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    tbOutputPath.Text = dialog.SelectedPath;
            }
        }
    }
}
