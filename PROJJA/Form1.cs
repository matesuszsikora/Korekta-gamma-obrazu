using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace PROJJA
{
    public partial class Form1 : Form
    {

        //ścieżki do DLL

        [DllImport(@"C:\Users\mateu\OneDrive\Pulpit\PROJJA\x64\Release\ASMDLL.dll")]
        public static extern void MyProc1(IntPtr data, ref double gamma, long length);

        [DllImport(@"C:\Users\mateu\OneDrive\Pulpit\PROJJA\x64\Release\CDLL.dll")]

        public static extern void MyProc2(IntPtr data, ref double gamma, long length);



        private string originalImagePath = string.Empty;
        private string processedImagePath = string.Empty;

        public Form1()
        {
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        private void LoadImage(string path)
        {
            try
            {
                originalImagePath = path;
                pictureBox2.Image = new Bitmap(path); // Wczytaj obraz do PictureBox
                label2.Text = path; // Wyświetl ścieżkę
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas wczytywania obrazu: " + ex.Message);
            }
        }


        private void ApplyGammaCorrection(double gamma)
        {
            try
            {
                if (string.IsNullOrEmpty(originalImagePath))
                {
                    MessageBox.Show("Najpierw załaduj obraz.");
                    return;
                }

                // Pobranie liczby wątków z TextBox1
                int watekCount;
                if (!int.TryParse(textBox1.Text, out watekCount) || watekCount <= 0)
                {
                    watekCount = 1; // Ustaw domyślną liczbę wątków na 1
                }

                // Pomiar czasu
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Wczytaj obraz oryginalny
                Bitmap originalBitmap = new Bitmap(originalImagePath);
                int width = originalBitmap.Width;
                int height = originalBitmap.Height;

                // Zamiana obrazu na tablicę bajtów
                Rectangle rect = new Rectangle(0, 0, width, height);
                System.Drawing.Imaging.BitmapData bitmapData = originalBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                IntPtr imagePtr = bitmapData.Scan0;
                int length = Math.Abs(bitmapData.Stride) * height;

                byte[] pixels = new byte[length];
                Marshal.Copy(imagePtr, pixels, 0, length);

                int chunkSize = length / watekCount;
                chunkSize = (chunkSize / 4) * 4; // Dopasowanie do wielokrotności 4
                if (chunkSize == 0) chunkSize = 4;

                Task[] tasks = new Task[watekCount];

                for (int i = 0; i < watekCount; i++)
                {
                    int start = i * chunkSize;
                    int end = (i == watekCount - 1) ? length : start + chunkSize;

                    tasks[i] = Task.Run(() =>
                    {
                        if (radioButton1.Checked)
                        {
                            MyProc1(Marshal.UnsafeAddrOfPinnedArrayElement(pixels, start), ref gamma, end - start);
                        }
                        else if (radioButton2.Checked)
                        {
                            MyProc2(Marshal.UnsafeAddrOfPinnedArrayElement(pixels, start), ref gamma, end - start);
                        }
                    });
                }

                Task.WaitAll(tasks);

                Marshal.Copy(pixels, 0, imagePtr, length);
                originalBitmap.UnlockBits(bitmapData);

                // Zapisanie i wyświetlenie przetworzonego obrazu
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                    pictureBox1.Image = null;
                }

                processedImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "processed_image.png");
                originalBitmap.Save(processedImagePath);

                pictureBox1.Image = new Bitmap(processedImagePath);
                label3.Text = processedImagePath;

                // Zakończenie pomiaru czasu
                stopwatch.Stop();
                label5.Text = $"Czas: {stopwatch.ElapsedMilliseconds} ms";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas korekty gamma: " + ex.Message);
            }
        }






        private void Form1_Load(object sender, EventArgs e)
        {

        }

        //make button
        private void button2_Click(object sender, EventArgs e)
        {
            if (double.TryParse(textBox2.Text, out double gamma))
            {
                ApplyGammaCorrection(gamma);
            }
            else
            {
                MessageBox.Show("Wprowadź poprawny współczynnik gamma.");
            }
        }

        //load small button
        private void button5_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.bmp;*.jpg;*.png";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadImage(openFileDialog.FileName);
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(originalImagePath) || string.IsNullOrEmpty(processedImagePath))
            {
                MessageBox.Show("Najpierw załaduj obraz i wykonaj korekcję gamma.");
                return;
            }

            try
            {
                // Wczytanie obrazów
                Bitmap originalBitmap = new Bitmap(originalImagePath);
                Bitmap processedBitmap = new Bitmap(processedImagePath);

                // Obliczenie histogramów
                int[] originalRedHistogram = new int[256];
                int[] originalGreenHistogram = new int[256];
                int[] originalBlueHistogram = new int[256];

                int[] processedRedHistogram = new int[256];
                int[] processedGreenHistogram = new int[256];
                int[] processedBlueHistogram = new int[256];

                ComputeHistogram(originalBitmap, originalRedHistogram, originalGreenHistogram, originalBlueHistogram);
                ComputeHistogram(processedBitmap, processedRedHistogram, processedGreenHistogram, processedBlueHistogram);

                // Ścieżka do pliku CSV
                string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "histogram_data.csv");

                // Tworzenie pliku CSV
                using (StreamWriter writer = new StreamWriter(csvPath))
                {
                    writer.WriteLine("Value,Original Red,Original Green,Original Blue,Processed Red,Processed Green,Processed Blue");

                    for (int i = 0; i < 256; i++)
                    {
                        writer.WriteLine($"{i},{originalRedHistogram[i]},{originalGreenHistogram[i]},{originalBlueHistogram[i]},{processedRedHistogram[i]},{processedGreenHistogram[i]},{processedBlueHistogram[i]}");
                    }
                }

                MessageBox.Show($"Plik CSV został wygenerowany: {csvPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas generowania pliku CSV: " + ex.Message);
            }
        }

        // Funkcja obliczająca histogram dla obrazu
        private void ComputeHistogram(Bitmap bitmap, int[] redHistogram, int[] greenHistogram, int[] blueHistogram)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);

                    redHistogram[pixel.R]++;
                    greenHistogram[pixel.G]++;
                    blueHistogram[pixel.B]++;
                }
            }
        }


        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }



        // Funkcja do konwersji Bitmap na tablicę bajtów
        private byte[] ImageToByteArray(Bitmap image)
        {
            int bytesPerPixel = 4; // Dla formatu PixelFormat.Format32bppArgb
            byte[] imageData = new byte[image.Width * image.Height * bytesPerPixel];

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    int index = (y * image.Width + x) * bytesPerPixel;

                    imageData[index] = pixel.R;
                    imageData[index + 1] = pixel.G;
                    imageData[index + 2] = pixel.B;
                    imageData[index + 3] = pixel.A;
                }
            }

            return imageData;
        }




        // Funkcja do konwersji tablicy bajtów na Bitmap
        private Bitmap ByteArrayToImage(byte[] data, int width, int height)
        {
            Bitmap image = new Bitmap(width, height);

            int bytesPerPixel = 4; // Dla formatu PixelFormat.Format32bppArgb
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width + x) * bytesPerPixel;

                    Color pixel = Color.FromArgb(
                        data[index + 3], // A
                        data[index],     // R
                        data[index + 1], // G
                        data[index + 2]  // B
                    );

                    image.SetPixel(x, y, pixel);
                }
            }

            return image;
        }

    }
}
