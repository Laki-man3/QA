namespace kursuck
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pictureBoxOriginal = new PictureBox();
            pictureBoxProcessed = new PictureBox();
            btnLoadImage = new Button();
            btnDetectHuman = new Button();
            btnSaveImage = new Button();
            openFileDialog = new OpenFileDialog();
            saveFileDialog = new SaveFileDialog();
            lblStatus = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBoxOriginal).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxProcessed).BeginInit();
            SuspendLayout();
            // 
            // pictureBoxOriginal
            // 
            pictureBoxOriginal.BorderStyle = BorderStyle.FixedSingle;
            pictureBoxOriginal.Location = new Point(12, 50);
            pictureBoxOriginal.Name = "pictureBoxOriginal";
            pictureBoxOriginal.Size = new Size(400, 380);
            pictureBoxOriginal.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxOriginal.TabIndex = 0;
            pictureBoxOriginal.TabStop = false;
            // 
            // pictureBoxProcessed
            // 
            pictureBoxProcessed.BorderStyle = BorderStyle.FixedSingle;
            pictureBoxProcessed.Location = new Point(430, 50);
            pictureBoxProcessed.Name = "pictureBoxProcessed";
            pictureBoxProcessed.Size = new Size(400, 380);
            pictureBoxProcessed.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxProcessed.TabIndex = 1;
            pictureBoxProcessed.TabStop = false;
            // 
            // btnLoadImage
            // 
            btnLoadImage.Location = new Point(12, 12);
            btnLoadImage.Name = "btnLoadImage";
            btnLoadImage.Size = new Size(150, 30);
            btnLoadImage.TabIndex = 2;
            btnLoadImage.Text = "Загрузить изображение";
            btnLoadImage.UseVisualStyleBackColor = true;
            btnLoadImage.Click += btnLoadImage_Click;
            // 
            // btnDetectHuman
            // 
            btnDetectHuman.Enabled = false;
            btnDetectHuman.Location = new Point(168, 12);
            btnDetectHuman.Name = "btnDetectHuman";
            btnDetectHuman.Size = new Size(150, 30);
            btnDetectHuman.TabIndex = 3;
            btnDetectHuman.Text = "Выделить контур человека";
            btnDetectHuman.UseVisualStyleBackColor = true;
            btnDetectHuman.Click += btnDetectHuman_Click;
            // 
            // btnSaveImage
            // 
            btnSaveImage.Enabled = false;
            btnSaveImage.Location = new Point(324, 12);
            btnSaveImage.Name = "btnSaveImage";
            btnSaveImage.Size = new Size(150, 30);
            btnSaveImage.TabIndex = 4;
            btnSaveImage.Text = "Сохранить изображение";
            btnSaveImage.UseVisualStyleBackColor = true;
            btnSaveImage.Click += btnSaveImage_Click;
            // 
            // openFileDialog
            // 
            openFileDialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
            openFileDialog.Title = "Выберите изображение";
            // 
            // saveFileDialog
            // 
            saveFileDialog.Filter = "Изображения PNG|*.png|Все файлы|*.*";
            saveFileDialog.Title = "Сохранить обработанное изображение";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(480, 19);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(48, 15);
            lblStatus.TabIndex = 5;
            lblStatus.Text = "Готово";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(842, 450);
            Controls.Add(lblStatus);
            Controls.Add(btnSaveImage);
            Controls.Add(btnDetectHuman);
            Controls.Add(btnLoadImage);
            Controls.Add(pictureBoxProcessed);
            Controls.Add(pictureBoxOriginal);
            Name = "Form1";
            Text = "Выделение контуров человека";
            ((System.ComponentModel.ISupportInitialize)pictureBoxOriginal).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxProcessed).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBoxOriginal;
        private PictureBox pictureBoxProcessed;
        private Button btnLoadImage;
        private Button btnDetectHuman;
        private Button btnSaveImage;
        private OpenFileDialog openFileDialog;
        private SaveFileDialog saveFileDialog;
        private Label lblStatus;
    }
}
