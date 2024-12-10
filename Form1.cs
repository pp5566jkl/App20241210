namespace App20241210
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Filter = "圖像文件(JPeg, Gif, Bmp, etc.)|.jpg;*jpeg;*.gif;*.bmp;*.tif;*.tiff;*.png|所有文件(*.*)|*.*";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    Bitmap MyBitmap = new Bitmap(openFileDialog1.FileName);
                    this.pictureBox1.Image = MyBitmap;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "訊息顯示");
            }
        }

        private Bitmap edgeImage = null; // 用於存儲邊緣影像
        private void button2_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("請先載入影像！", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Bitmap inputImage = new Bitmap(pictureBox1.Image);
            edgeImage = SobelEdgeDetection(inputImage);

            pictureBox2.Image = edgeImage; // 在 PictureBox2 中顯示邊緣檢測結果
            pictureBox2.Refresh();

            MessageBox.Show("Sobel 邊緣檢測完成！", "訊息");
        }

        private Bitmap SobelEdgeDetection(Bitmap inputImage)
        {
            Bitmap grayImage = Grayscale(inputImage); // 將影像轉為灰階
            Bitmap edgeImage = new Bitmap(grayImage.Width, grayImage.Height);

            int[,] gx = new int[,]
            {
        { -1, 0, 1 },
        { -2, 0, 2 },
        { -1, 0, 1 }
            };

            int[,] gy = new int[,]
            {
        { -1, -2, -1 },
        { 0,  0,  0 },
        { 1,  2,  1 }
            };

            for (int y = 1; y < grayImage.Height - 1; y++)
            {
                for (int x = 1; x < grayImage.Width - 1; x++)
                {
                    int gradientX = 0, gradientY = 0;

                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            Color pixel = grayImage.GetPixel(x + kx, y + ky);
                            int intensity = pixel.R; // 灰階值

                            gradientX += intensity * gx[ky + 1, kx + 1];
                            gradientY += intensity * gy[ky + 1, kx + 1];
                        }
                    }

                    int gradient = (int)Math.Sqrt(gradientX * gradientX + gradientY * gradientY);
                    gradient = Math.Min(255, Math.Max(0, gradient)); // 確保在範圍內
                    edgeImage.SetPixel(x, y, Color.FromArgb(gradient, gradient, gradient));
                }
            }

            return edgeImage;
        }

        private Bitmap Grayscale(Bitmap inputImage)
        {
            Bitmap grayImage = new Bitmap(inputImage.Width, inputImage.Height);

            for (int y = 0; y < inputImage.Height; y++)
            {
                for (int x = 0; x < inputImage.Width; x++)
                {
                    Color originalColor = inputImage.GetPixel(x, y);
                    int grayValue = (int)(originalColor.R * 0.3 + originalColor.G * 0.59 + originalColor.B * 0.11);
                    grayImage.SetPixel(x, y, Color.FromArgb(grayValue, grayValue, grayValue));
                }
            }

            return grayImage;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (edgeImage == null)
            {
                MessageBox.Show("請先進行邊緣檢測！", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Bitmap resultImage = DetectCircles(edgeImage); // 執行圓形檢測
            pictureBox2.Image = resultImage;
            pictureBox2.Refresh();

            // 顯示完成訊息
            MessageBox.Show("Hough 圓形檢測完成，檢測到的圓以紅色標示。", "訊息");
        }

        private Bitmap DetectCircles(Bitmap edgeImage)
        {
            Bitmap resultImage = new Bitmap(edgeImage);
            int width = edgeImage.Width;
            int height = edgeImage.Height;
            int radiusMin = 10;
            int radiusMax = 100;
            int angleStep = 10;
            int threshold = (int)(360.0 / angleStep * 0.7);

            // 初始化霍夫空間累加器
            short[,,] accumulator = new short[width, height, radiusMax];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (edgeImage.GetPixel(x, y).G > 128) // 假設邊緣像素值為高亮
                    {
                        for (int r = radiusMin; r < radiusMax; r++)
                        {
                            for (int angle = 0; angle < 360; angle += angleStep)
                            {
                                double theta = angle * Math.PI / 180;
                                int a = (int)(x - r * Math.Cos(theta));
                                int b = (int)(y - r * Math.Sin(theta));
                                if (a >= 0 && a < width && b >= 0 && b < height)
                                {
                                    accumulator[a, b, r]++;
                                }
                            }
                        }
                    }
                }
            }

            // 從累加器中找到圓
            int count = 0;
            List<string> circleDetails = new List<string>();
            using (Graphics g = Graphics.FromImage(resultImage))
            {
                for (int a = 0; a < width; a++)
                {
                    for (int b = 0; b < height; b++)
                    {
                        for (int r = radiusMin; r < radiusMax; r++)
                        {
                            if (accumulator[a, b, r] >= threshold)
                            {
                                // 繪製紅色圓周
                                g.DrawEllipse(Pens.Red, a - r, b - r, 2 * r, 2 * r);

                                // 編號圓形
                                g.DrawString((++count).ToString(), new Font("Arial", 10), Brushes.Blue, a, b);

                                // 記錄圓心與半徑
                                circleDetails.Add($"圓形 {count}: 圓心=({a}, {b}), 半徑={r}");
                            }
                        }
                    }
                }
            }

            // 列印所有圓形資訊
            MessageBox.Show(string.Join(Environment.NewLine, circleDetails), "檢測到的圓形資訊", MessageBoxButtons.OK);

            return resultImage;
        }

    }
}