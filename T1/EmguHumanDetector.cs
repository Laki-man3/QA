using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Dnn;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kursuck
{
    public class EmguHumanDetector
    {
        private Net? _net;
        private readonly string[] _classNames;
        private string _modelPath;
        private string _configPath;
        private float _confidenceThreshold = 0.25f;
        private readonly float _nmsThreshold = 0.4f;
        private bool _isInitialized = false;
        private bool _isDownloading = false;
        private bool _highSensitivityMode = false;

        public event EventHandler<string>? StatusChanged;

        public EmguHumanDetector()
        {
            // Загружаем названия классов COCO
            _classNames = new[]
            {
                "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat",
                "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat",
                "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack",
                "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball",
                "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle",
                "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich",
                "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa",
                "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote",
                "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book",
                "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush"
            };
            
            // Пути для файлов модели будут определены позже
            _modelPath = string.Empty;
            _configPath = string.Empty;
        }

        public float ConfidenceThreshold 
        { 
            get => _confidenceThreshold;
            set => _confidenceThreshold = value; 
        }

        public bool HighSensitivityMode
        {
            get => _highSensitivityMode;
            set => _highSensitivityMode = value;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized || _isDownloading)
                return;

            _isDownloading = true;
            
            try
            {
                OnStatusChanged("Поиск файлов модели...");
                
                // Находим модель и конфигурацию
                if (!await FindModelFilesAsync())
                {
                    OnStatusChanged("Модель не найдена в локальных директориях. Выберите файлы вручную...");
                    
                    // Предложим пользователю выбрать файл
                    if (!await SelectModelFilesManuallyAsync())
                    {
                        throw new Exception("Необходимые файлы модели не были предоставлены");
                    }
                }

                OnStatusChanged("Инициализация нейронной сети...");
                
                // Инициализация нейронной сети
                _net = DnnInvoke.ReadNetFromDarknet(_configPath, _modelPath);
                
                // Устанавливаем бэкенд для вычислений
                _net.SetPreferableBackend(Emgu.CV.Dnn.Backend.OpenCV);
                _net.SetPreferableTarget(Target.Cpu);

                _isInitialized = true;
                _isDownloading = false;
                
                OnStatusChanged("Детектор успешно инициализирован!");
            }
            catch (Exception ex)
            {
                _isDownloading = false;
                OnStatusChanged($"Ошибка инициализации: {ex.Message}");
                MessageBox.Show($"Ошибка при инициализации детектора: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Ищет файлы модели по различным путям
        private async Task<bool> FindModelFilesAsync()
        {
            // Список возможных путей для поиска
            List<string> searchDirectories = new List<string>();
            
            // 1. Текущая директория
            searchDirectories.Add(Directory.GetCurrentDirectory());
            
            // 2. Директория приложения
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            searchDirectories.Add(appDir);
            
            // 3. Директория исполняемого файла
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? string.Empty;
            searchDirectories.Add(exeDir);
            
            // 4. Директория проекта
            string projectDir = @"C:\labs\kursuck";
            searchDirectories.Add(projectDir);
            
            // 5. Директория сборки
            string buildDir = Path.Combine(projectDir, "bin", "Debug", "net9.0-windows");
            searchDirectories.Add(buildDir);
            
            // 6. Директория Models в корне проекта
            searchDirectories.Add(Path.Combine(projectDir, "Models"));
            
            // 7. Директория Models в директории сборки
            searchDirectories.Add(Path.Combine(buildDir, "Models"));
            
            OnStatusChanged($"Поиск файлов в {searchDirectories.Count} директориях...");
            
            foreach (string directory in searchDirectories)
            {
                if (!Directory.Exists(directory))
                    continue;
                
                string weightsPath = Path.Combine(directory, "yolov4.weights");
                string configPath = Path.Combine(directory, "yolov4.cfg");
                
                if (File.Exists(weightsPath) && File.Exists(configPath))
                {
                    OnStatusChanged($"Найдены файлы модели в директории: {directory}");
                    _modelPath = weightsPath;
                    _configPath = configPath;
                    return true;
                }
            }
            
            OnStatusChanged("Файлы модели не найдены в стандартных директориях");
            return false;
        }
        
        // Предлагает пользователю выбрать файлы модели вручную
        private async Task<bool> SelectModelFilesManuallyAsync()
        {
            bool success = false;
            
            await Task.Run(() =>
            {
                // Запрашиваем UI-поток для диалогов выбора файлов
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                
                if (Application.OpenForms.Count > 0 && Application.OpenForms[0] != null)
                {
                    Application.OpenForms[0].Invoke(new Action(() =>
                    {
                        try
                        {
                            using (OpenFileDialog weightDialog = new OpenFileDialog())
                            {
                                weightDialog.Title = "Выберите файл весов модели YOLOv4 (yolov4.weights)";
                                weightDialog.Filter = "YOLO weights|*.weights|Все файлы|*.*";
                                weightDialog.InitialDirectory = @"C:\labs\kursuck";
                                
                                if (weightDialog.ShowDialog() == DialogResult.OK)
                                {
                                    // Сохраняем путь к выбранному файлу весов
                                    _modelPath = weightDialog.FileName;
                                    OnStatusChanged($"Выбран файл весов: {_modelPath}");
                                    
                                    // Теперь запрашиваем файл конфигурации
                                    using (OpenFileDialog configDialog = new OpenFileDialog())
                                    {
                                        configDialog.Title = "Выберите файл конфигурации YOLOv4 (yolov4.cfg)";
                                        configDialog.Filter = "YOLO config|*.cfg|Все файлы|*.*";
                                        configDialog.InitialDirectory = Path.GetDirectoryName(_modelPath) ?? @"C:\labs\kursuck";
                                        
                                        if (configDialog.ShowDialog() == DialogResult.OK)
                                        {
                                            // Сохраняем путь к выбранному файлу конфигурации
                                            _configPath = configDialog.FileName;
                                            OnStatusChanged($"Выбран файл конфигурации: {_configPath}");
                                            tcs.SetResult(true);
                                        }
                                        else
                                        {
                                            tcs.SetResult(false);
                                        }
                                    }
                                }
                                else
                                {
                                    tcs.SetResult(false);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            OnStatusChanged($"Ошибка при выборе файлов: {ex.Message}");
                            tcs.SetResult(false);
                        }
                    }));
                }
                else
                {
                    OnStatusChanged("Не найдена главная форма приложения");
                    tcs.SetResult(false);
                }
                
                success = tcs.Task.Result;
            });
            
            return success;
        }

        private void OnStatusChanged(string message)
        {
            StatusChanged?.Invoke(this, message);
            Console.WriteLine(message);
        }

        public bool IsReady => _isInitialized;

        public List<HumanDetectionResult> DetectHumans(Bitmap image, bool onlyHumans = true)
        {
            if (!_isInitialized || _net == null)
            {
                throw new InvalidOperationException("Детектор не инициализирован. Вызовите InitializeAsync() и дождитесь завершения инициализации.");
            }

            // Конвертируем Bitmap в Mat
            Mat imageMat = BitmapToMat(image);

            // Получаем размеры изображения
            int imageHeight = imageMat.Height;
            int imageWidth = imageMat.Width;

            // Используем различные размеры входа в режиме высокой чувствительности
            List<Size> inputSizes = _highSensitivityMode
                ? new List<Size> { new Size(416, 416), new Size(608, 608) }
                : new List<Size> { new Size(416, 416) };

            // Получаем базовый порог уверенности в зависимости от режима
            float baseConfidenceThreshold = _highSensitivityMode
                ? Math.Min(0.1f, _confidenceThreshold) // Очень низкий порог для режима высокой чувствительности
                : _confidenceThreshold;

            // Используем несколько входных размеров в режиме высокой чувствительности, 
            // чтобы увеличить шансы обнаружения объектов разных размеров
            List<HumanDetectionResult> allResults = new List<HumanDetectionResult>();
            
            foreach (var inputSize in inputSizes)
            {
                // Преобразуем изображение в блоб для подачи в сеть
                using var blob = DnnInvoke.BlobFromImage(
                    imageMat,
                    1 / 255.0,
                    inputSize,
                    new MCvScalar(0, 0, 0),
                    true,
                    false);

                _net.SetInput(blob);

                // Получаем названия выходных слоев
                var outNames = _net.UnconnectedOutLayersNames;
                
                using var outs = new VectorOfMat();
                _net.Forward(outs, outNames);

                // Списки для хранения обнаруженных объектов
                List<Rectangle> boxes = new List<Rectangle>();
                List<float> confidences = new List<float>();
                List<int> classIds = new List<int>();

                // Обрабатываем выходные тензоры (feature maps)
                for (int i = 0; i < outs.Size; i++)
                {
                    // Используем небезопасный код для работы с указателями
                    unsafe
                    {
                        Mat output = outs[i];
                        float* data = (float*)output.DataPointer;

                        // Для каждого обнаружения в выходном слое
                        for (int j = 0; j < output.Rows; j++)
                        {
                            try
                            {
                                // В YOLO первые 4 значения - это координаты бокса, 5-е - уверенность, остальные - вероятности классов
                                int numClasses = output.Cols - 5;
                                
                                if (numClasses <= 0 || output.Cols < 5)
                                {
                                    OnStatusChanged($"Недопустимое количество классов: {numClasses}, cols: {output.Cols}");
                                    continue;
                                }
                                
                                // Очень низкий порог начальной фильтрации в режиме высокой чувствительности
                                float initialThreshold = _highSensitivityMode ? 0.01f : baseConfidenceThreshold * 0.5f;
                                float objectness = data[j * output.Cols + 4];

                                if (objectness > initialThreshold)
                                {
                                    // Находим класс с максимальной вероятностью
                                    int classIdMax = 0;
                                    float maxProb = 0;
                                    
                                    for (int c = 0; c < numClasses && c < _classNames.Length; c++)
                                    {
                                        int index = j * output.Cols + 5 + c;
                                        if (index >= 0 && index < output.Rows * output.Cols)
                                        {
                                            float prob = data[index];
                                            if (prob > maxProb)
                                            {
                                                maxProb = prob;
                                                classIdMax = c;
                                            }
                                        }
                                    }

                                    // Проверяем, что классификатор в допустимых пределах
                                    if (classIdMax >= _classNames.Length)
                                    {
                                        OnStatusChanged($"Индекс класса {classIdMax} выходит за границы массива имен классов ({_classNames.Length})");
                                        continue;
                                    }

                                    float confidence = objectness * maxProb;
                                    float finalThreshold = _highSensitivityMode ? 0.05f : baseConfidenceThreshold * 0.8f;

                                    // Если уверенность выше порога и (запрашиваем только людей и это человек, или запрашиваем все объекты)
                                    if (confidence > finalThreshold && 
                                        (!onlyHumans || classIdMax == 0)) // 0 - индекс класса "person"
                                    {
                                        // Координаты центра объекта
                                        int centerXIndex = j * output.Cols + 0;
                                        int centerYIndex = j * output.Cols + 1;
                                        int widthIndex = j * output.Cols + 2;
                                        int heightIndex = j * output.Cols + 3;
                                        
                                        if (centerXIndex >= output.Rows * output.Cols ||
                                            centerYIndex >= output.Rows * output.Cols ||
                                            widthIndex >= output.Rows * output.Cols ||
                                            heightIndex >= output.Rows * output.Cols)
                                        {
                                            OnStatusChanged("Индекс выходит за границы массива при расчете координат");
                                            continue;
                                        }
                                        
                                        float centerX = data[centerXIndex] * imageWidth;
                                        float centerY = data[centerYIndex] * imageHeight;
                                        
                                        // Ширина и высота объекта
                                        float width = data[widthIndex] * imageWidth;
                                        float height = data[heightIndex] * imageHeight;
                                        
                                        // Верхний левый угол
                                        float left = centerX - width / 2;
                                        float top = centerY - height / 2;

                                        // Добавляем обнаружение в списки
                                        classIds.Add(classIdMax);
                                        confidences.Add(confidence);
                                        boxes.Add(new Rectangle((int)left, (int)top, (int)width, (int)height));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                OnStatusChanged($"Ошибка при обработке обнаружения {j}: {ex.Message}");
                            }
                        }
                    }
                }

                try
                {
                    // Применяем non-maximum suppression, чтобы удалить перекрывающиеся боксы
                    if (boxes.Count > 0)
                    {
                        // Используем более высокий порог NMS в режиме высокой чувствительности
                        float nmsThreshold = _highSensitivityMode ? 0.6f : _nmsThreshold;
                        
                        using var indices = new VectorOfInt();
                        using var boxesVector = new VectorOfRect(boxes.ToArray());
                        using var confidencesVector = new VectorOfFloat(confidences.ToArray());
                        
                        DnnInvoke.NMSBoxes(boxesVector, confidencesVector, 
                            _highSensitivityMode ? 0.05f : baseConfidenceThreshold * 0.7f, 
                            nmsThreshold, indices);
                        
                        // Собираем результаты
                        for (int i = 0; i < indices.Size; i++)
                        {
                            int idx = indices[i];
                            if (idx >= 0 && idx < boxes.Count && idx < classIds.Count && idx < confidences.Count)
                            {
                                Rectangle box = boxes[idx];
                                int classId = classIds[idx];
                                
                                // Проверяем допустимость индекса класса
                                if (classId >= 0 && classId < _classNames.Length)
                                {
                                    // Создаем результат детекции
                                    var detection = new HumanDetectionResult
                                    {
                                        X = box.X,
                                        Y = box.Y,
                                        Width = box.Width,
                                        Height = box.Height,
                                        ClassName = _classNames[classId],
                                        Confidence = confidences[idx]
                                    };
                                    
                                    allResults.Add(detection);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnStatusChanged($"Ошибка при финальной обработке результатов: {ex.Message}");
                }
            }
            
            // В режиме высокой чувствительности удаляем дубликаты (вызвано использованием разных входных размеров)
            if (_highSensitivityMode && allResults.Count > 0)
            {
                allResults = RemoveDuplicates(allResults);
            }
            
            return allResults;
        }
        
        // Удаляет дублирующиеся обнаружения
        private List<HumanDetectionResult> RemoveDuplicates(List<HumanDetectionResult> detections)
        {
            List<HumanDetectionResult> uniqueDetections = new List<HumanDetectionResult>();
            
            foreach (var detection in detections)
            {
                bool isDuplicate = false;
                
                foreach (var uniqueDetection in uniqueDetections)
                {
                    // Рассчитываем перекрытие боксов
                    var intersection = new Rectangle(
                        Math.Max(detection.X, uniqueDetection.X),
                        Math.Max(detection.Y, uniqueDetection.Y),
                        Math.Min(detection.X + detection.Width, uniqueDetection.X + uniqueDetection.Width) - Math.Max(detection.X, uniqueDetection.X),
                        Math.Min(detection.Y + detection.Height, uniqueDetection.Y + uniqueDetection.Height) - Math.Max(detection.Y, uniqueDetection.Y)
                    );
                    
                    // Если есть перекрытие и оно значительное, считаем как дубликат
                    if (intersection.Width > 0 && intersection.Height > 0)
                    {
                        double intersectionArea = intersection.Width * intersection.Height;
                        double detectionArea = detection.Width * detection.Height;
                        double uniqueArea = uniqueDetection.Width * uniqueDetection.Height;
                        double overlapRatio = intersectionArea / Math.Min(detectionArea, uniqueArea);
                        
                        if (overlapRatio > 0.5)
                        {
                            isDuplicate = true;
                            
                            // Если текущее обнаружение имеет более высокую уверенность, заменяем существующее
                            if (detection.Confidence > uniqueDetection.Confidence)
                            {
                                uniqueDetections.Remove(uniqueDetection);
                                uniqueDetections.Add(detection);
                            }
                            
                            break;
                        }
                    }
                }
                
                if (!isDuplicate)
                {
                    uniqueDetections.Add(detection);
                }
            }
            
            return uniqueDetections;
        }
        
        private Mat BitmapToMat(Bitmap bitmap)
        {
            // Простой способ - сохранить изображение во временный файл и загрузить его с помощью CvInvoke
            string tempFile = Path.GetTempFileName() + ".png";
            bitmap.Save(tempFile);
            Mat mat = new Mat(tempFile);
            File.Delete(tempFile);
            return mat;
        }
        
        public Bitmap DrawDetections(Bitmap originalImage, List<HumanDetectionResult> detections)
        {
            // Клонируем изображение для рисования
            Bitmap resultImage = new Bitmap(originalImage);
            
            using (Graphics g = Graphics.FromImage(resultImage))
            {
                foreach (var detection in detections)
                {
                    // Создаем прямоугольник для обнаруженного объекта
                    Rectangle rect = new Rectangle(detection.X, detection.Y, detection.Width, detection.Height);
                    
                    // Определяем цвет в зависимости от уверенности
                    Color boxColor = GetConfidenceColor(detection.Confidence);
                    
                    // Рисуем прямоугольник
                    using (Pen pen = new Pen(boxColor, 3))
                    {
                        g.DrawRectangle(pen, rect);
                    }
                    
                    // Рисуем метку класса и уверенность
                    string label = $"{detection.ClassName}: {detection.Confidence:P0}";
                    using (Font font = new Font("Arial", 12))
                    using (SolidBrush brushBg = new SolidBrush(Color.Black))
                    using (SolidBrush brushFg = new SolidBrush(Color.White))
                    {
                        SizeF textSize = g.MeasureString(label, font);
                        PointF textLocation = new PointF(rect.X, rect.Y > textSize.Height ? rect.Y - textSize.Height : rect.Y);
                        g.FillRectangle(brushBg, textLocation.X, textLocation.Y, textSize.Width, textSize.Height);
                        g.DrawString(label, font, brushFg, textLocation);
                    }
                }
            }
            
            return resultImage;
        }
        
        // Возвращает цвет в зависимости от уверенности обнаружения
        private Color GetConfidenceColor(float confidence)
        {
            if (confidence > 0.7f)
                return Color.Green;  // Высокая уверенность
            else if (confidence > 0.4f)
                return Color.Red;    // Средняя уверенность
            else
                return Color.Orange; // Низкая уверенность
        }
    }

    // Класс для хранения результатов детекции
    public class HumanDetectionResult
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }
} 