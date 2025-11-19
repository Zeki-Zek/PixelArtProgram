

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PixelArtEditor
{
    public class PixelArtForm : Form
    {
        private PictureBox canvas;
        private Panel toolPanel;
        private Panel colorPanel;
        private TrackBar zoomBar;
        private Label zoomLabel;
        private Button btnPencil, btnEraser, btnFill, btnEyedropper, btnClear, btnExport;
        private Panel primaryColorBox, secondaryColorBox;

        private Bitmap canvasBitmap;
        private int canvasWidth = 32;
        private int canvasHeight = 32;
        private int pixelSize = 16;
        private Color primaryColor = Color.Black;
        private Color secondaryColor = Color.White;
        private Tool currentTool = Tool.Pencil;
        private bool isDrawing = false;

        private Stack<Bitmap> undoStack = new Stack<Bitmap>();
        private Stack<Bitmap> redoStack = new Stack<Bitmap>();

        enum Tool { Pencil, Eraser, Fill, Eyedropper }

        public PixelArtForm()
        {
            InitializeComponents();
            InitializeCanvas();
        }

        private void InitializeComponents()
        {
            this.Text = "Pixel Art Editor";
            this.Size = new Size(1000, 700);
            this.KeyPreview = true;
            this.KeyDown += Form_KeyDown;

            // Tool Panel
            toolPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 80,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            btnPencil = CreateToolButton("Pencil", 10, Tool.Pencil);
            btnEraser = CreateToolButton("Eraser", 60, Tool.Eraser);
            btnFill = CreateToolButton("Fill", 110, Tool.Fill);
            btnEyedropper = CreateToolButton("Dropper", 160, Tool.Eyedropper);
            btnClear = CreateToolButton("Clear", 210, Tool.Pencil);
            btnClear.Click -= ToolButton_Click;
            btnClear.Click += (s, e) => ClearCanvas();

            btnExport = CreateToolButton("Export", 260, Tool.Pencil);
            btnExport.Click -= ToolButton_Click;
            btnExport.Click += (s, e) => ExportImage();

            toolPanel.Controls.AddRange(new Control[] {
                btnPencil, btnEraser, btnFill, btnEyedropper, btnClear, btnExport
            });

            // Color Panel
            colorPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 150,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(10)
            };

            Label lblColors = new Label
            {
                Text = "Colors",
                ForeColor = Color.White,
                Location = new Point(10, 10),
                AutoSize = true
            };

            primaryColorBox = new Panel
            {
                Size = new Size(60, 60),
                Location = new Point(10, 40),
                BackColor = primaryColor,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            primaryColorBox.Click += (s, e) => PickColor(true);

            secondaryColorBox = new Panel
            {
                Size = new Size(60, 60),
                Location = new Point(80, 40),
                BackColor = secondaryColor,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            secondaryColorBox.Click += (s, e) => PickColor(false);

            // Preset colors
            int y = 120;
            Color[] presets = { Color.Black, Color.White, Color.Red, Color.Green,
                              Color.Blue, Color.Yellow, Color.Cyan, Color.Magenta,
                              Color.Orange, Color.Purple, Color.Brown, Color.Pink };

            for (int i = 0; i < presets.Length; i++)
            {
                Panel preset = new Panel
                {
                    Size = new Size(30, 30),
                    Location = new Point(10 + (i % 4) * 35, y + (i / 4) * 35),
                    BackColor = presets[i],
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand,
                    Tag = presets[i]
                };
                preset.Click += PresetColor_Click;
                colorPanel.Controls.Add(preset);
            }

            colorPanel.Controls.AddRange(new Control[] { lblColors, primaryColorBox, secondaryColorBox });

            // Zoom Control
            Panel zoomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            zoomLabel = new Label
            {
                Text = "Zoom: 100%",
                ForeColor = Color.White,
                Location = new Point(10, 10),
                AutoSize = true
            };

            zoomBar = new TrackBar
            {
                Minimum = 4,
                Maximum = 32,
                Value = pixelSize,
                TickFrequency = 4,
                Location = new Point(10, 30),
                Width = 300
            };
            zoomBar.ValueChanged += ZoomBar_ValueChanged;

            zoomPanel.Controls.AddRange(new Control[] { zoomLabel, zoomBar });

            // Canvas
            canvas = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(60, 60, 60),
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.Paint += Canvas_Paint;

            this.Controls.Add(canvas);
            this.Controls.Add(toolPanel);
            this.Controls.Add(colorPanel);
            this.Controls.Add(zoomPanel);

            btnPencil.BackColor = Color.FromArgb(0, 122, 204);
        }

        private Button CreateToolButton(string text, int y, Tool tool)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(10, y),
                Size = new Size(60, 40),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Tag = tool,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btn.Click += ToolButton_Click;
            return btn;
        }

        private void InitializeCanvas()
        {
            canvasBitmap = new Bitmap(canvasWidth, canvasHeight);
            using (Graphics g = Graphics.FromImage(canvasBitmap))
            {
                g.Clear(Color.Transparent);
            }
            UpdateCanvasDisplay();
        }

        private void UpdateCanvasDisplay()
        {
            Bitmap display = new Bitmap(canvasWidth * pixelSize, canvasHeight * pixelSize);
            using (Graphics g = Graphics.FromImage(display))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(canvasBitmap, 0, 0, display.Width, display.Height);

                // Draw grid
                using (Pen gridPen = new Pen(Color.FromArgb(50, 255, 255, 255)))
                {
                    for (int x = 0; x <= canvasWidth; x++)
                        g.DrawLine(gridPen, x * pixelSize, 0, x * pixelSize, display.Height);
                    for (int y = 0; y <= canvasHeight; y++)
                        g.DrawLine(gridPen, 0, y * pixelSize, display.Width, y * pixelSize);
                }
            }
            canvas.Image?.Dispose();
            canvas.Image = display;
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                SaveUndo();
                isDrawing = true;
                DrawPixel(e.X, e.Y, e.Button == MouseButtons.Right);
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing && (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right))
            {
                DrawPixel(e.X, e.Y, e.Button == MouseButtons.Right);
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            isDrawing = false;
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            // Additional rendering if needed
        }

        private void DrawPixel(int mouseX, int mouseY, bool useSecondary)
        {
            if (canvas.Image == null) return;

            int offsetX = (canvas.Width - canvas.Image.Width) / 2;
            int offsetY = (canvas.Height - canvas.Image.Height) / 2;

            int x = (mouseX - offsetX) / pixelSize;
            int y = (mouseY - offsetY) / pixelSize;

            if (x < 0 || x >= canvasWidth || y < 0 || y >= canvasHeight) return;

            Color colorToUse = useSecondary ? secondaryColor : primaryColor;

            switch (currentTool)
            {
                case Tool.Pencil:
                    canvasBitmap.SetPixel(x, y, colorToUse);
                    break;
                case Tool.Eraser:
                    canvasBitmap.SetPixel(x, y, Color.Transparent);
                    break;
                case Tool.Fill:
                    if (!isDrawing) return;
                    isDrawing = false;
                    FloodFill(x, y, canvasBitmap.GetPixel(x, y), colorToUse);
                    break;
                case Tool.Eyedropper:
                    Color picked = canvasBitmap.GetPixel(x, y);
                    if (useSecondary)
                    {
                        secondaryColor = picked;
                        secondaryColorBox.BackColor = picked;
                    }
                    else
                    {
                        primaryColor = picked;
                        primaryColorBox.BackColor = picked;
                    }
                    return;
            }

            UpdateCanvasDisplay();
        }

        private void FloodFill(int x, int y, Color targetColor, Color replacementColor)
        {
            if (targetColor.ToArgb() == replacementColor.ToArgb()) return;

            Stack<Point> pixels = new Stack<Point>();
            pixels.Push(new Point(x, y));

            while (pixels.Count > 0)
            {
                Point p = pixels.Pop();
                if (p.X < 0 || p.X >= canvasWidth || p.Y < 0 || p.Y >= canvasHeight) continue;
                if (canvasBitmap.GetPixel(p.X, p.Y).ToArgb() != targetColor.ToArgb()) continue;

                canvasBitmap.SetPixel(p.X, p.Y, replacementColor);

                pixels.Push(new Point(p.X + 1, p.Y));
                pixels.Push(new Point(p.X - 1, p.Y));
                pixels.Push(new Point(p.X, p.Y + 1));
                pixels.Push(new Point(p.X, p.Y - 1));
            }
        }

        private void ToolButton_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            currentTool = (Tool)btn.Tag;

            foreach (Control ctrl in toolPanel.Controls)
            {
                if (ctrl is Button b && b != btnClear && b != btnExport)
                    b.BackColor = Color.FromArgb(60, 60, 60);
            }
            btn.BackColor = Color.FromArgb(0, 122, 204);
        }

        private void ZoomBar_ValueChanged(object sender, EventArgs e)
        {
            pixelSize = zoomBar.Value;
            zoomLabel.Text = $"Zoom: {(pixelSize * 100 / 16)}%";
            UpdateCanvasDisplay();
        }

        private void PickColor(bool isPrimary)
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.Color = isPrimary ? primaryColor : secondaryColor;
                dialog.FullOpen = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (isPrimary)
                    {
                        primaryColor = dialog.Color;
                        primaryColorBox.BackColor = dialog.Color;
                    }
                    else
                    {
                        secondaryColor = dialog.Color;
                        secondaryColorBox.BackColor = dialog.Color;
                    }
                }
            }
        }

        private void PresetColor_Click(object sender, EventArgs e)
        {
            Panel panel = (Panel)sender;
            primaryColor = (Color)panel.Tag;
            primaryColorBox.BackColor = primaryColor;
        }

        private void ClearCanvas()
        {
            SaveUndo();
            using (Graphics g = Graphics.FromImage(canvasBitmap))
            {
                g.Clear(Color.Transparent);
            }
            UpdateCanvasDisplay();
        }

        private void SaveUndo()
        {
            undoStack.Push((Bitmap)canvasBitmap.Clone());
            redoStack.Clear();
            if (undoStack.Count > 50) // Limit undo stack
            {
                var temp = undoStack.ToArray();
                undoStack.Clear();
                for (int i = 0; i < 49; i++)
                    undoStack.Push(temp[i]);
            }
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z && undoStack.Count > 0)
            {
                redoStack.Push((Bitmap)canvasBitmap.Clone());
                canvasBitmap.Dispose();
                canvasBitmap = undoStack.Pop();
                UpdateCanvasDisplay();
            }
            else if (e.Control && e.KeyCode == Keys.Y && redoStack.Count > 0)
            {
                undoStack.Push((Bitmap)canvasBitmap.Clone());
                canvasBitmap.Dispose();
                canvasBitmap = redoStack.Pop();
                UpdateCanvasDisplay();
            }
        }

        private void ExportImage()
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "PNG Image|*.png|Bitmap Image|*.bmp";
                dialog.DefaultExt = "png";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    canvasBitmap.Save(dialog.FileName);
                    MessageBox.Show("Image exported successfully!", "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            canvasBitmap?.Dispose();
            canvas.Image?.Dispose();
            base.OnFormClosing(e);
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PixelArtForm());
        }
    }
}