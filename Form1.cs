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
                openFileDialog1.Filter = "�Ϲ����(JPeg, Gif, Bmp, etc.)|.jpg;*jpeg;*.gif;*.bmp;*.tif;*.tiff;*.png|�Ҧ����(*.*)|*.*";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    Bitmap MyBitmap = new Bitmap(openFileDialog1.FileName);
                    this.pictureBox1.Image = MyBitmap;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "�T�����");
            }
        }

        private Bitmap edgeImage = null; // �Ω�s�x��t�v��
        private void button2_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("�Х����J�v���I", "���~", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Bitmap inputImage = new Bitmap(pictureBox1.Image);
            edgeImage = SobelEdgeDetection(inputImage);

            pictureBox2.Image = edgeImage; // �b PictureBox2 �������t�˴����G
            pictureBox2.Refresh();

            MessageBox.Show("Sobel ��t�˴������I", "�T��");
        }

        private Bitmap SobelEdgeDetection(Bitmap inputImage)
        {
            Bitmap grayImage = Grayscale(inputImage); // �N�v���ର�Ƕ�
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
                            int intensity = pixel.R; // �Ƕ���

                            gradientX += intensity * gx[ky + 1, kx + 1];
                            gradientY += intensity * gy[ky + 1, kx + 1];
                        }
                    }

                    int gradient = (int)Math.Sqrt(gradientX * gradientX + gradientY * gradientY);
                    gradient = Math.Min(255, Math.Max(0, gradient)); // �T�O�b�d��
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
                MessageBox.Show("�Х��i����t�˴��I", "���~", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Bitmap resultImage = DetectCircles(edgeImage); // �������˴�
            pictureBox2.Image = resultImage;
            pictureBox2.Refresh();

            // ��ܧ����T��
            MessageBox.Show("Hough ����˴������A�˴��쪺��H����ХܡC", "�T��");
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

            // ��l���N�ҪŶ��֥[��
            short[,,] accumulator = new short[width, height, radiusMax];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (edgeImage.GetPixel(x, y).G > 128) // ���]��t�����Ȭ����G
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

            // �q�֥[��������]�[�J�D���j�ȧ��^
            int count = 0;
            List<string> circleDetails = new List<string>();

            // �إ߶�βM��
            List<(int a, int b, int r, int votes)> circles = new List<(int, int, int, int)>();

            // �����Ҧ��i�઺���
            for (int a = 0; a < width; a++)
            {
                for (int b = 0; b < height; b++)
                {
                    for (int r = radiusMin; r < radiusMax; r++)
                    {
                        if (accumulator[a, b, r] >= threshold)
                        {
                            circles.Add((a, b, r, accumulator[a, b, r]));
                        }
                    }
                }
            }

            // �D���j�ȧ��G�L�o���۪񪺶��
            List<(int a, int b, int r)> filteredCircles = new List<(int, int, int)>();
            foreach (var circle in circles)
            {
                bool isUnique = true;
                foreach (var filtered in filteredCircles)
                {
                    double distance = Math.Sqrt(Math.Pow(circle.a - filtered.a, 2) + Math.Pow(circle.b - filtered.b, 2));
                    if (distance < Math.Min(circle.r, filtered.r)) // ��߶Z���p��b�|�A�������|
                    {
                        isUnique = false;
                        break;
                    }
                }
                if (isUnique)
                {
                    filteredCircles.Add((circle.a, circle.b, circle.r));
                }
            }

            // �b�Ϥ��Wø�s��P�аO
            using (Graphics g = Graphics.FromImage(resultImage))
            {
                foreach (var circle in filteredCircles)
                {
                    // ø�s�����P
                    g.DrawEllipse(Pens.Red, circle.a - circle.r, circle.b - circle.r, 2 * circle.r, 2 * circle.r);

                    // �s�����
                    g.DrawString((++count).ToString(), new Font("Arial", 10), Brushes.Blue, circle.a, circle.b);

                    // �O����߻P�b�|
                    circleDetails.Add($"��� {count}: ���=({circle.a}, {circle.b}), �b�|={circle.r}");
                }
            }

            // ��ܵ��G
            string resultDetails = string.Join("\n", circleDetails);
            MessageBox.Show(resultDetails, "�˴����G", MessageBoxButtons.OK, MessageBoxIcon.Information);


            return resultImage;
        }

    }
}