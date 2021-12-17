using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ImageContentRetrieval
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();

        }

        private Ash _ash = new(2048);
        private string _ashFile;
        private readonly List<string> _retrievalResult = new();

        private async void frmMain_Load(object sender, EventArgs e)
        {

            //var (graph, bottleneck,cast) = Another.import_graph();

            //var config = new ConfigProto
            //{
            //    AllowSoftPlacement = true,
            //    GpuOptions = new GPUOptions
            //    {
            //        AllowGrowth = true,
            //        ForceGpuCompatible = true
            //    }
            //};

            //using var session = new Session(graph, config);

            //var feature = Another.get_bottleneck_data(session, bottleneck, cast, @"C:\Users\cyclone\Desktop\test_dataset\b17eca8065380cd7b2dead6cad44ad345882816a.jpg");


            _ashFile = FeatureExtractor.GetFileAbsolutePath("features.ash");

            try
            {
                if (File.Exists(_ashFile))
                    _ash = await Ash.LoadAsync(_ashFile);

                //MessageBox.Show("loaded done");
            }
            catch (Exception)
            {
                MessageBox.Show("特征文件加载失败");
            }

        }


        private async void btnBuild_Click(object sender, EventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                btnCleanup.Enabled = btnBuild.Enabled = btnRetrieval.Enabled = false;

                var sw = System.Diagnostics.Stopwatch.StartNew();


                var files = Directory.EnumerateFiles(dialog.FileName, "*.jpg", SearchOption.AllDirectories);
                files = files.Concat(Directory.EnumerateFiles(dialog.FileName, "*.jpeg", SearchOption.AllDirectories));


                int count = 0;

                if (!File.Exists(_ashFile))
                {
                    var features = await FeatureExtractor.GetImagesFeatures(files);
                    count = features.Count();
                    Ash.Build(features, _ashFile, 2048);
                }
                else
                {
                    if (_ash == null)
                        _ash = Ash.Load(_ashFile);

                    files = _ash.Except(files);//排除重复项
                    var features = await FeatureExtractor.GetImagesFeatures(files);
                    foreach (var f in features)
                    {
                        if (_ash.Add(f))
                            count++;
                    }
                    _ash.Build();
                }


                sw.Stop();

                MessageBox.Show($"对 {count} 个文件建库，耗时 {sw.Elapsed}");

                btnCleanup.Enabled = btnRetrieval.Enabled = btnBuild.Enabled = true;
            }
        }



        private void btnRetrieval_Click(object sender, EventArgs e)
        {

            _retrievalResult.Clear();
            lbResult.Items.Clear();

            //if (!System.IO.File.Exists(_ashFile))
            if (_ash == null)
            {
                MessageBox.Show("特征库文件不存在，请先建库！");
                return;
            }

            using var ofd = new OpenFileDialog();
            //ofd.Filter = "JPG图像|*.jpg;*.jpeg";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                var target_feature = FeatureExtractor.GetImageFeature(ofd.FileName);

                sw.Stop();
                MessageBox.Show("提单张图片特征耗时 ：" + sw.Elapsed);
                sw.Restart();

                var result = _ash.Retrieval(target_feature, (int)numericUpDown1.Value);

                sw.Stop();

                MessageBox.Show("检索耗时 : " + sw.Elapsed);

                foreach (var item in result)
                {
                    lbResult.Items.Add($"距离 : {item.Item1} , 索引 : {item.Item2} , 文件 : {item.Item3}");
                    _retrievalResult.Add(item.Item3);
                }

                if (pbSource.Image != null)
                {
                    pbSource.Image.Dispose();
                    pbSource.Image = null;
                }

                //这样做就可以在打开的时候也删文件了
                var imageData = File.ReadAllBytes(ofd.FileName);
                using var imageStream = new MemoryStream(imageData);
                pbSource.Image = Image.FromStream(imageStream);
            }

        }



        private void lbResult_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbResult.SelectedIndex == -1)
                return;

            var file = _retrievalResult[lbResult.SelectedIndex];

            if (pbResult.Image != null)
            {
                pbResult.Image.Dispose();
                pbResult.Image = null;
            }

            //这样做就可以在打开的时候也删文件了
            var imageData = System.IO.File.ReadAllBytes(file);
            using var imageStream = new MemoryStream(imageData);
            pbResult.Image = Image.FromStream(imageStream);

        }



        private async void btnCleanup_Click(object sender, EventArgs e)
        {
            btnCleanup.Enabled = btnBuild.Enabled = btnRetrieval.Enabled = false;
            await Ash.CleanupAsync(_ashFile);
            btnCleanup.Enabled = btnRetrieval.Enabled = btnBuild.Enabled = true;
        }



        private void lbResult_DoubleClick(object sender, EventArgs e)
        {
            if (lbResult.SelectedIndex == -1)
                return;

            var file = _retrievalResult[lbResult.SelectedIndex];

            ShellOpenFolderAndSelectFile.LocateFile(file);
        }
    }
}
