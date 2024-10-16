using Microsoft.WindowsAPICodePack.Dialogs;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace ImageDocking;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 画像入力ディレクトリダイアログを表示
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void InputDirectoryDialogButton_Click(object sender, RoutedEventArgs e)
    {
        using CommonOpenFileDialog cofd = new()
        {
            // フォルダを選択できるようにする
            IsFolderPicker = true
        };
        if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
        {
            this.InputDirectoryPath.Text = cofd.FileName;
        }
    }

    /// <summary>
    /// 画像出力ディレクトリダイアログを表示
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OutputDirectoryDialogButton_Click(object sender, RoutedEventArgs e)
    {
        using CommonOpenFileDialog cofd = new()
        {
            // フォルダを選択できるようにする
            IsFolderPicker = true
        };
        if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
        {
            this.OutputDirectoryPath.Text = cofd.FileName;
        }
    }

    /// <summary>
    /// 画像結合実行
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void Execute_Click(object sender, RoutedEventArgs e)
    {
        string inputFolderPath = this.InputDirectoryPath.Text;
        // カレントディレクトリ内の画像ファイルを取得（jpg, png, gifなどの形式）
        var imageFiles = Directory
            .GetFiles(inputFolderPath, "*.*")
            .Where(file => file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                            file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                            file.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => Path.GetFileName(file))
            .ToArray();
        if (imageFiles.Length == 0)
        {
            MessageBox.Show("フォルダに画像がありません。");
            return;
        }

        // クルクルを表示
        this.LoadingOverlay.Visibility = Visibility.Visible;
        this.Whole.IsEnabled = false;

        // 結合後の画像の保存フォルダ
        string outputFolderPath = this.OutputDirectoryPath.Text;
        if (string.IsNullOrEmpty(outputFolderPath))
        {
            MessageBox.Show("出力ディレクトリが指定されていません。");
        }

        // 出力フォルダが存在しない場合は作成
        if (!Directory.Exists(outputFolderPath))
        {
            Directory.CreateDirectory(outputFolderPath);
        }

        // 実行
        await Task.Run(() =>
        {
            int counter = 1;

            for (int i = 0; i < imageFiles.Length; i += 2)
            {
                using Image img1 = Image.FromFile(imageFiles[i]);
                using Image img2 = i + 1 < imageFiles.Length
                    ? Image.FromFile(imageFiles[i + 1])
                    : new Func<Image>(() =>
                    {
                        // 指定したサイズのBitmapを作成
                        Bitmap bitmap = new(img1.Width, img1.Height);
                        // Graphicsオブジェクトを使ってBitmapを白で塗りつぶす
                        using Graphics g = Graphics.FromImage(bitmap);
                        g.Clear(Color.White);
                        // BitmapをImageとして返す
                        return bitmap;
                    })();

                // 画像の結合
                int width = img1.Width + img2.Width;
                int height = img1.Height > img2.Height ? img1.Height : img2.Height;

                int newHeight1 = img1.Height * height / img1.Height;
                int newWidth1 = img1.Width * height / img1.Height;
                Rectangle rect1 = new Rectangle(0, 0, newWidth1, newHeight1);

                int newHeight2 = img2.Height * height / img2.Height;
                int newWidth2 = img2.Width * height / img2.Height;
                Rectangle rect2 = new Rectangle(newWidth1, 0, newWidth2, newHeight2);

                Bitmap bmp = new(newWidth1 + newWidth2, height);
                Graphics g = Graphics.FromImage(bmp);
                g.DrawImage(img1, rect1);
                g.DrawImage(img2, rect2);
                g.Dispose();
                img1.Dispose();
                img2.Dispose();

                string outputFileName = Path.Combine(outputFolderPath, $"combined_image_{counter:D4}.jpg");
                bmp.Save(outputFileName, ImageFormat.Jpeg);
                bmp.Dispose();

                counter++;
            }
        });

        // 処理完了後、クルクルを非表示
        this.LoadingOverlay.Visibility = Visibility.Collapsed;
        this.Whole.IsEnabled = true;
        MessageBox.Show("処理終了");
    }
}
