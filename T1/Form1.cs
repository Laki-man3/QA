using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kursuck
{
    public partial class Form1 : Form
    {
        private Bitmap? originalImage;
        private Bitmap? processedImage;
        private EmguHumanDetector? humanDetector;
        private string lastImagePath = string.Empty;
        private TrackBar trackBarSensitivity;
        private Label labelSensitivity;
        private CheckBox checkBoxHighSensitivity;

        public Form1()
        {
            InitializeComponent();
            
            // Уменьшаем размер pictureBox, чтобы освободить место для элементов управления
            pictureBoxOriginal.Height = pictureBoxOriginal.Height - 100;
            pictureBoxProcessed.Height = pictureBoxProcessed.Height - 100;
            
            // Смещаем pictureBox вниз, чтобы освободить место сверху
            pictureBoxOriginal.Top += 100;
            pictureBoxProcessed.Top += 100;
            
            // Перемещаем элементы управления в видимую область
            // Добавляем трекбар для настройки чувствительности в верхней части формы
            labelSensitivity = new Label
            {
                Text = "Чувствительность: 25%",
                Location = new Point(pictureBoxOriginal.Left, btnLoadImage.Bottom + 10),
                Size = new Size(150, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(labelSensitivity);
            
            trackBarSensitivity = new TrackBar
            {
                Minimum = 1,
                Maximum = 50,
                Value = 25,
                Location = new Point(labelSensitivity.Left, labelSensitivity.Bottom + 5),
                Size = new Size(150, 45),
                TickFrequency = 5,
                TickStyle = TickStyle.Both
            };
            trackBarSensitivity.ValueChanged += TrackBarSensitivity_ValueChanged;
            Controls.Add(trackBarSensitivity);
            
            // Добавляем чекбокс для режима высокой чувствительности рядом с trackBar
            checkBoxHighSensitivity = new CheckBox
            {
                Text = "Режим высокой чувствительности",
                Location = new Point(trackBarSensitivity.Right + 20, trackBarSensitivity.Top + 10),
                Size = new Size(250, 25),
                AutoSize = true
            };
            checkBoxHighSensitivity.CheckedChanged += CheckBoxHighSensitivity_CheckedChanged;
            Controls.Add(checkBoxHighSensitivity);
            
            // Явно выводим метку статуса под элементами управления, но над изображениями
            lblStatus.BringToFront();
            lblStatus.Location = new Point(pictureBoxOriginal.Left, trackBarSensitivity.Bottom + 5);
            
            InitializeDetector();
        }

        private void CheckBoxHighSensitivity_CheckedChanged(object? sender, EventArgs e)
        {
            if (humanDetector != null)
            {
                humanDetector.HighSensitivityMode = checkBoxHighSensitivity.Checked;
                
                if (checkBoxHighSensitivity.Checked)
                {
                    MessageBox.Show(
                        "Режим высокой чувствительности включен.\n\n" +
                        "В этом режиме детектор будет использовать более низкие пороги и разные размеры входа " +
                        "для обнаружения людей на фоне или частично видимых людей.\n\n" +
                        "Обработка может занять больше времени.",
                        "Информация", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                }
            }
        }

        private void TrackBarSensitivity_ValueChanged(object? sender, EventArgs e)
        {
            if (humanDetector != null)
            {
                float value = trackBarSensitivity.Value / 100.0f;
                humanDetector.ConfidenceThreshold = value;
                labelSensitivity.Text = $"Чувствительность: {trackBarSensitivity.Value}%";
            }
        }

        private async void InitializeDetector()
        {
            lblStatus.Text = "Инициализация детектора...";
            btnDetectHuman.Enabled = false;

            humanDetector = new EmguHumanDetector();
            humanDetector.StatusChanged += HumanDetector_StatusChanged;

            try
            {
                await humanDetector.InitializeAsync();
                btnDetectHuman.Enabled = originalImage != null && humanDetector.IsReady;
                
                // Установка начальной чувствительности
                humanDetector.ConfidenceThreshold = trackBarSensitivity.Value / 100.0f;
                humanDetector.HighSensitivityMode = checkBoxHighSensitivity.Checked;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации детектора: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Ошибка инициализации";
            }
        }

        private void HumanDetector_StatusChanged(object? sender, string message)
        {
            // Так как это событие может произойти из другого потока, используем Invoke для обновления UI
            if (InvokeRequired)
            {
                Invoke(new Action(() => lblStatus.Text = message));
            }
            else
            {
                lblStatus.Text = message;
            }
        }

        private void btnLoadImage_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    lastImagePath = openFileDialog.FileName;
                    originalImage = new Bitmap(lastImagePath);
                    pictureBoxOriginal.Image = originalImage;
                    btnDetectHuman.Enabled = humanDetector != null && humanDetector.IsReady;
                    lblStatus.Text = "Изображение загружено. Готово к обработке.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void btnDetectHuman_Click(object sender, EventArgs e)
        {
            if (originalImage == null || humanDetector == null || !humanDetector.IsReady)
            {
                MessageBox.Show("Сначала загрузите изображение и дождитесь инициализации детектора", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lblStatus.Text = "Обработка...";
            btnDetectHuman.Enabled = false;
            btnLoadImage.Enabled = false;
            checkBoxHighSensitivity.Enabled = false;
            trackBarSensitivity.Enabled = false;

            try
            {
                // Создаем копию изображения, чтобы избежать проблем с доступом
                Bitmap imageCopy = new Bitmap(originalImage);
                
                await Task.Run(() =>
                {
                    // Обнаруживаем людей на изображении
                    var detections = humanDetector.DetectHumans(imageCopy, true);

                    // Рисуем обнаруженные объекты
                    processedImage = humanDetector.DrawDetections(imageCopy, detections);

                    // Обновляем UI
                    Invoke(new Action(() =>
                    {
                        pictureBoxProcessed.Image = processedImage;
                        lblStatus.Text = $"Обнаружено: {detections.Count} человек";
                        btnSaveImage.Enabled = true;
                        btnDetectHuman.Enabled = true;
                        btnLoadImage.Enabled = true;
                        checkBoxHighSensitivity.Enabled = true;
                        trackBarSensitivity.Enabled = true;
                        
                        // Если ничего не найдено, показываем сообщение
                        if (detections.Count == 0)
                        {
                            MessageBox.Show("На изображении не обнаружено людей", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обработки изображения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Ошибка обработки";
                btnDetectHuman.Enabled = true;
                btnLoadImage.Enabled = true;
                checkBoxHighSensitivity.Enabled = true;
                trackBarSensitivity.Enabled = true;
            }
        }

        private void btnSaveImage_Click(object sender, EventArgs e)
        {
            if (processedImage == null)
            {
                MessageBox.Show("Нет обработанного изображения для сохранения", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            saveFileDialog.FileName = Path.GetFileNameWithoutExtension(lastImagePath) + "_processed.png";
            
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    processedImage.Save(saveFileDialog.FileName);
                    lblStatus.Text = "Изображение сохранено";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения изображения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
