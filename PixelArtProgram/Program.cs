using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace PixelArtEditor
{
    // Layer class
    public class Layer
    {
        public string Name { get; set; }
        public Bitmap Bitmap { get; set; }
        public bool Visible { get; set; } = true;
        public float Opacity { get; set; } = 1.0f;

        public Layer(int width, int height, string name)
        {
            Name = name;
            Bitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(Bitmap))
            {
                g.Clear(Color.Transparent);
            }
        }

        public Layer Clone()
        {
            Layer clone = new Layer(Bitmap.Width, Bitmap.Height, Name);
            clone.Visible = Visible;
            clone.Opacity = Opacity;
            using (Graphics g = Graphics.FromImage(clone.Bitmap))
            {
                g.DrawImage(Bitmap, 0, 0);
            }
            return clone;
        }
    }

    // Start Screen Dialog
    public class StartScreenDialog : Form
    {
        private NumericUpDown widthInput;
        private NumericUpDown heightInput;
        private Button btnCreate;
        private Button btnCancel;
        private Label lblPreview;

        public int CanvasWidth { get; private set; }
        public int CanvasHeight { get; private set; }

        public StartScreenDialog()
        {
            this.Text = "Pixel Art Editor - New Canvas";
            this.Size = new Size(450, 350);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);

            Label lblTitle = new Label
            {
                Text = "Create New Canvas",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };

            Label lblWidth = new Label
            {
                Text = "Width (pixels):",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                Location = new Point(30, 80),
                AutoSize = true
            };

            widthInput = new NumericUpDown
            {
                Location = new Point(150, 78),
                Width = 120,
                Font = new Font("Segoe UI", 10),
                Minimum = 16,
                Maximum = 512,
                Value = 64,
                Increment = 16
            };
            widthInput.ValueChanged += UpdatePreview;

            Label lblHeight = new Label
            {
                Text = "Height (pixels):",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                Location = new Point(30, 120),
                AutoSize = true
            };

            heightInput = new NumericUpDown
            {
                Location = new Point(150, 118),
                Width = 120,
                Font = new Font("Segoe UI", 10),
                Minimum = 16,
                Maximum = 512,
                Value = 64,
                Increment = 16
            };
            heightInput.ValueChanged += UpdatePreview;

            lblPreview = new Label
            {
                Text = "Canvas: 64x64 px (4x4 tiles @ 16px each)",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGray,
                Location = new Point(30, 160),
                AutoSize = true
            };

            Label lblNote = new Label
            {
                Text = "Note: Each tile on the canvas is 16x16 pixels.\nYour canvas will be divided into these tiles.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(30, 200),
                Size = new Size(380, 50)
            };

            btnCreate = new Button
            {
                Text = "Create Canvas",
                Location = new Point(150, 260),
                Size = new Size(120, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK,
                Cursor = Cursors.Hand
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.Click += (s, e) =>
            {
                CanvasWidth = (int)widthInput.Value;
                CanvasHeight = (int)heightInput.Value;
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(280, 260),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] {
                lblTitle, lblWidth, widthInput, lblHeight, heightInput,
                lblPreview, lblNote, btnCreate, btnCancel
            });
            this.AcceptButton = btnCreate;
            this.CancelButton = btnCancel;
        }

        private void UpdatePreview(object sender, EventArgs e)
        {
            int w = (int)widthInput.Value;
            int h = (int)heightInput.Value;
            int tilesW = w / 16;
            int tilesH = h / 16;
            lblPreview.Text = $"Canvas: {w}x{h} px ({tilesW}x{tilesH} tiles @ 16px each)";
        }
    }

    public class PixelArtForm : Form
    {
        private PictureBox canvas;
        private Panel toolPanel;
        private Panel colorPanel;
        private Panel layerPanel;
        private TrackBar zoomBar;
        private Label zoomLabel;
        private Button btnPencil, btnEraser, btnFill, btnEyedropper, btnLasso, btnRectSelect,
                       btnLine, btnBlur, btnClear, btnExport, btnNewCanvas;
        private Panel currentColorDisplay;
        private TrackBar hueBar, satBar, valBar, alphaBar;
        private Label lblHue, lblSat, lblVal, lblAlpha;
        private Panel colorPreview;
        private ListBox layerListBox;
        private Button btnNewLayer, btnDeleteLayer, btnRenameLayer, btnMergeDown;

        private List<Layer> layers = new List<Layer>();
        private int currentLayerIndex = 0;
        private int canvasWidth;
        private int canvasHeight;
        private int pixelSize = 20;
        private Color currentColor = Color.Black;
        private int currentAlpha = 255;
        private Tool currentTool = Tool.Pencil;
        private bool isDrawing = false;
        private Point lastPoint;
        private Point startPoint;

        private Stack<List<Layer>> undoStack = new Stack<List<Layer>>();
        private Stack<List<Layer>> redoStack = new Stack<List<Layer>>();

        private Rectangle selectionRect;
        private bool hasSelection = false;
        private List<Point> lassoPoints = new List<Point>();
        private Bitmap selectionBitmap;
        private Point selectionOffset;
        private bool isMovingSelection = false;
        private SelectionMode selectionMode = SelectionMode.None;

        enum Tool { Pencil, Eraser, Fill, Eyedropper, Lasso, RectSelect, Line, Blur }
        enum SelectionMode { None, Lasso, Rectangle }

        public PixelArtForm(int width, int height)
        {
            canvasWidth = width;
            canvasHeight = height;
            InitializeComponents();
            InitializeCanvas();
        }

        private void InitializeComponents()
        {
            this.Text = "Pixel Art Editor";
            this.Size = new Size(1400, 800);
            this.KeyPreview = true;
            this.KeyDown += Form_KeyDown;
            this.BackColor = Color.FromArgb(30, 30, 30);

            // Tool Panel
            toolPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 90,
                BackColor = Color.FromArgb(45, 45, 48),
                AutoScroll = true
            };

            btnPencil = CreateToolButton("Pencil", 10, Tool.Pencil);
            btnEraser = CreateToolButton("Eraser", 60, Tool.Eraser);
            btnFill = CreateToolButton("Fill", 110, Tool.Fill);
            btnEyedropper = CreateToolButton("Dropper", 160, Tool.Eyedropper);
            btnLasso = CreateToolButton("Lasso", 210, Tool.Lasso);
            btnRectSelect = CreateToolButton("Rect Sel", 260, Tool.RectSelect);
            btnLine = CreateToolButton("Line", 310, Tool.Line);
            btnBlur = CreateToolButton("Blur", 360, Tool.Blur);
            btnClear = CreateToolButton("Clear", 410, Tool.Pencil);
            btnClear.Click -= ToolButton_Click;
            btnClear.Click += (s, e) => ClearCanvas();

            btnExport = CreateToolButton("Export", 460, Tool.Pencil);
            btnExport.Click -= ToolButton_Click;
            btnExport.Click += (s, e) => ExportImage();

            btnNewCanvas = CreateToolButton("New", 510, Tool.Pencil);
            btnNewCanvas.Click -= ToolButton_Click;
            btnNewCanvas.Click += (s, e) => ShowNewCanvasDialog();

            toolPanel.Controls.AddRange(new Control[] {
                btnPencil, btnEraser, btnFill, btnEyedropper, btnLasso, btnRectSelect,
                btnLine, btnBlur, btnClear, btnExport, btnNewCanvas
            });

            // Color Panel
            colorPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 280,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(10),
                AutoScroll = true
            };

            Label lblColorTitle = new Label
            {
                Text = "Color Mixer",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                AutoSize = true
            };

            currentColorDisplay = new Panel
            {
                Size = new Size(250, 60),
                Location = new Point(15, 40),
                BackColor = currentColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            colorPreview = new Panel
            {
                Size = new Size(250, 40),
                Location = new Point(15, 110),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblHue = new Label { Text = "Hue: 0°", ForeColor = Color.White, Location = new Point(15, 160), AutoSize = true };
            hueBar = new TrackBar
            {
                Location = new Point(15, 180),
                Width = 250,
                Minimum = 0,
                Maximum = 360,
                Value = 0,
                TickFrequency = 30
            };
            hueBar.ValueChanged += ColorSlider_Changed;

            lblSat = new Label { Text = "Saturation: 0%", ForeColor = Color.White, Location = new Point(15, 230), AutoSize = true };
            satBar = new TrackBar
            {
                Location = new Point(15, 250),
                Width = 250,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                TickFrequency = 10
            };
            satBar.ValueChanged += ColorSlider_Changed;

            lblVal = new Label { Text = "Value: 0%", ForeColor = Color.White, Location = new Point(15, 300), AutoSize = true };
            valBar = new TrackBar
            {
                Location = new Point(15, 320),
                Width = 250,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                TickFrequency = 10
            };
            valBar.ValueChanged += ColorSlider_Changed;

            lblAlpha = new Label { Text = "Opacity: 100%", ForeColor = Color.White, Location = new Point(15, 370), AutoSize = true };
            alphaBar = new TrackBar
            {
                Location = new Point(15, 390),
                Width = 250,
                Minimum = 0,
                Maximum = 255,
                Value = 255,
                TickFrequency = 25
            };
            alphaBar.ValueChanged += AlphaSlider_Changed;

            Label lblPresets = new Label
            {
                Text = "Color Presets",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 450),
                AutoSize = true
            };

            Color[] presets = {
                Color.Black, Color.FromArgb(64, 64, 64), Color.FromArgb(128, 128, 128), Color.White,
                Color.FromArgb(128, 0, 0), Color.Red, Color.FromArgb(255, 128, 0), Color.Yellow,
                Color.FromArgb(0, 128, 0), Color.Lime, Color.Cyan, Color.FromArgb(0, 128, 128),
                Color.FromArgb(0, 0, 128), Color.Blue, Color.FromArgb(128, 0, 128), Color.Magenta
            };

            for (int i = 0; i < presets.Length; i++)
            {
                Panel preset = new Panel
                {
                    Size = new Size(28, 28),
                    Location = new Point(15 + (i % 4) * 32, 480 + (i / 4) * 32),
                    BackColor = presets[i],
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand,
                    Tag = presets[i]
                };
                preset.Click += PresetColor_Click;
                colorPanel.Controls.Add(preset);
            }

            colorPanel.Controls.AddRange(new Control[] {
                lblColorTitle, currentColorDisplay, colorPreview,
                lblHue, hueBar, lblSat, satBar, lblVal, valBar, lblAlpha, alphaBar, lblPresets
            });

            // Layer Panel
            layerPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 250,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(10)
            };

            Label lblLayers = new Label
            {
                Text = "Layers",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                AutoSize = true
            };

            layerListBox = new ListBox
            {
                Location = new Point(15, 40),
                Size = new Size(220, 300),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            layerListBox.SelectedIndexChanged += LayerListBox_SelectedIndexChanged;

            btnNewLayer = new Button
            {
                Text = "New Layer",
                Location = new Point(15, 350),
                Size = new Size(105, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8)
            };
            btnNewLayer.FlatAppearance.BorderSize = 0;
            btnNewLayer.Click += (s, e) => AddNewLayer();

            btnDeleteLayer = new Button
            {
                Text = "Delete",
                Location = new Point(130, 350),
                Size = new Size(105, 30),
                BackColor = Color.FromArgb(180, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8)
            };
            btnDeleteLayer.FlatAppearance.BorderSize = 0;
            btnDeleteLayer.Click += (s, e) => DeleteCurrentLayer();

            btnRenameLayer = new Button
            {
                Text = "Rename",
                Location = new Point(15, 390),
                Size = new Size(105, 30),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8)
            };
            btnRenameLayer.FlatAppearance.BorderSize = 0;
            btnRenameLayer.Click += (s, e) => RenameCurrentLayer();

            btnMergeDown = new Button
            {
                Text = "Merge Down",
                Location = new Point(130, 390),
                Size = new Size(105, 30),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8)
            };
            btnMergeDown.FlatAppearance.BorderSize = 0;
            btnMergeDown.Click += (s, e) => MergeLayerDown();

            layerPanel.Controls.AddRange(new Control[] {
                lblLayers, layerListBox, btnNewLayer, btnDeleteLayer, btnRenameLayer, btnMergeDown
            });

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
                Location = new Point(15, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };

            zoomBar = new TrackBar
            {
                Minimum = 4,
                Maximum = 40,
                Value = pixelSize,
                TickFrequency = 4,
                Location = new Point(15, 35),
                Width = 300
            };
            zoomBar.ValueChanged += ZoomBar_ValueChanged;

            Label canvasSizeLabel = new Label
            {
                Text = $"Canvas: {canvasWidth}x{canvasHeight} px ({canvasWidth / 16}x{canvasHeight / 16} tiles)",
                ForeColor = Color.LightGray,
                Location = new Point(330, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };

            zoomPanel.Controls.AddRange(new Control[] { zoomLabel, zoomBar, canvasSizeLabel });

            // Canvas
            canvas = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 50, 50),
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.Paint += Canvas_Paint;

            this.Controls.Add(canvas);
            this.Controls.Add(toolPanel);
            this.Controls.Add(layerPanel);
            this.Controls.Add(colorPanel);
            this.Controls.Add(zoomPanel);

            btnPencil.BackColor = Color.FromArgb(0, 122, 204);
            UpdateColorPreview();
        }

        private Button CreateToolButton(string text, int y, Tool tool)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(15, y),
                Size = new Size(60, 40),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Tag = tool,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8)
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btn.Click += ToolButton_Click;
            return btn;
        }

        private void InitializeCanvas()
        {
            layers.Clear();
            layers.Add(new Layer(canvasWidth, canvasHeight, "Background"));
            currentLayerIndex = 0;
            UpdateLayerList();
            UpdateCanvasDisplay();
        }

        private void UpdateLayerList()
        {
            layerListBox.Items.Clear();
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                layerListBox.Items.Add($"{layers[i].Name} {(layers[i].Visible ? "👁" : "🚫")}");
            }
            layerListBox.SelectedIndex = layers.Count - 1 - currentLayerIndex;
        }

        private void LayerListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (layerListBox.SelectedIndex >= 0)
            {
                currentLayerIndex = layers.Count - 1 - layerListBox.SelectedIndex;
                UpdateCanvasDisplay();
            }
        }

        private void AddNewLayer()
        {
            SaveUndo();
            int newIndex = layers.Count;
            layers.Add(new Layer(canvasWidth, canvasHeight, $"Layer {newIndex}"));
            currentLayerIndex = layers.Count - 1;
            UpdateLayerList();
            UpdateCanvasDisplay();
        }

        private void DeleteCurrentLayer()
        {
            if (layers.Count <= 1)
            {
                MessageBox.Show("Cannot delete the last layer!", "Delete Layer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveUndo();
            layers.RemoveAt(currentLayerIndex);
            if (currentLayerIndex >= layers.Count)
                currentLayerIndex = layers.Count - 1;
            UpdateLayerList();
            UpdateCanvasDisplay();
        }

        private void RenameCurrentLayer()
        {
            string newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter new layer name:",
                "Rename Layer",
                layers[currentLayerIndex].Name);

            if (!string.IsNullOrWhiteSpace(newName))
            {
                layers[currentLayerIndex].Name = newName;
                UpdateLayerList();
            }
        }

        private void MergeLayerDown()
        {
            if (currentLayerIndex == 0)
            {
                MessageBox.Show("Cannot merge down the bottom layer!", "Merge Down", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveUndo();
            Layer current = layers[currentLayerIndex];
            Layer below = layers[currentLayerIndex - 1];

            using (Graphics g = Graphics.FromImage(below.Bitmap))
            {
                g.DrawImage(current.Bitmap, 0, 0);
            }

            layers.RemoveAt(currentLayerIndex);
            currentLayerIndex--;
            UpdateLayerList();
            UpdateCanvasDisplay();
        }

        private Bitmap CompositeLayers(bool dimInactive = true)
        {
            Bitmap composite = new Bitmap(canvasWidth, canvasHeight);
            using (Graphics g = Graphics.FromImage(composite))
            {
                g.Clear(Color.Transparent);

                for (int i = 0; i < layers.Count; i++)
                {
                    if (!layers[i].Visible) continue;

                    // Calculate opacity - dim inactive layers to 40%
                    float opacity = layers[i].Opacity;
                    if (dimInactive && i != currentLayerIndex)
                    {
                        opacity *= 0.4f;
                    }

                    ColorMatrix matrix = new ColorMatrix();
                    matrix.Matrix33 = opacity;

                    ImageAttributes attributes = new ImageAttributes();
                    attributes.SetColorMatrix(matrix);

                    g.DrawImage(layers[i].Bitmap,
                        new Rectangle(0, 0, canvasWidth, canvasHeight),
                        0, 0, canvasWidth, canvasHeight,
                        GraphicsUnit.Pixel, attributes);
                }
            }
            return composite;
        }

        private void UpdateCanvasDisplay()
        {
            int displayWidth = canvasWidth * pixelSize;
            int displayHeight = canvasHeight * pixelSize;

            Bitmap display = new Bitmap(displayWidth, displayHeight);
            using (Graphics g = Graphics.FromImage(display))
            {
                // Checkerboard background
                int tileSize = 16 * pixelSize;
                for (int y = 0; y < canvasHeight / 16; y++)
                {
                    for (int x = 0; x < canvasWidth / 16; x++)
                    {
                        Color tileColor = (x + y) % 2 == 0 ? Color.White : Color.FromArgb(220, 220, 220);
                        g.FillRectangle(new SolidBrush(tileColor),
                            x * tileSize, y * tileSize, tileSize, tileSize);
                    }
                }

                // Draw composited layers
                using (Bitmap composite = CompositeLayers(true))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(composite, 0, 0, displayWidth, displayHeight);
                }

                // Draw 16x16 tile grid
                using (Pen gridPen = new Pen(Color.FromArgb(100, 100, 100, 100), 2))
                {
                    for (int x = 0; x <= canvasWidth / 16; x++)
                        g.DrawLine(gridPen, x * tileSize, 0, x * tileSize, displayHeight);
                    for (int y = 0; y <= canvasHeight / 16; y++)
                        g.DrawLine(gridPen, 0, y * tileSize, displayWidth, y * tileSize);
                }

                // Draw pixel grid
                using (Pen pixelGrid = new Pen(Color.FromArgb(30, 255, 255, 255)))
                {
                    if (pixelSize >= 8)
                    {
                        for (int x = 0; x <= canvasWidth; x++)
                            g.DrawLine(pixelGrid, x * pixelSize, 0, x * pixelSize, displayHeight);
                        for (int y = 0; y <= canvasHeight; y++)
                            g.DrawLine(pixelGrid, 0, y * pixelSize, displayWidth, y * pixelSize);
                    }
                }

                // Draw selection
                if (hasSelection && selectionMode == SelectionMode.Rectangle)
                {
                    using (Pen selPen = new Pen(Color.FromArgb(0, 150, 255), 2))
                    {
                        selPen.DashStyle = DashStyle.Dash;
                        g.DrawRectangle(selPen,
                            selectionRect.X * pixelSize,
                            selectionRect.Y * pixelSize,
                            selectionRect.Width * pixelSize,
                            selectionRect.Height * pixelSize);
                    }
                }
            }

            canvas.Image?.Dispose();
            canvas.Image = display;
        }

        private Point GetCanvasPoint(int mouseX, int mouseY)
        {
            if (canvas.Image == null) return new Point(-1, -1);

            int offsetX = (canvas.Width - canvas.Image.Width) / 2;
            int offsetY = (canvas.Height - canvas.Image.Height) / 2;

            int x = (mouseX - offsetX) / pixelSize;
            int y = (mouseY - offsetY) / pixelSize;

            if (x < 0 || x >= canvasWidth || y < 0 || y >= canvasHeight)
                return new Point(-1, -1);

            return new Point(x, y);
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            Point p = GetCanvasPoint(e.X, e.Y);
            if (p.X < 0) return;

            // Check if clicking inside existing selection for moving
            if (hasSelection && selectionBitmap != null && IsPointInSelection(p))
            {
                isMovingSelection = true;
                selectionOffset = new Point(p.X - selectionRect.X, p.Y - selectionRect.Y);
                canvas.Cursor = Cursors.SizeAll;
                return;
            }

            SaveUndo();
            isDrawing = true;
            startPoint = p;
            lastPoint = p;

            if (currentTool == Tool.Lasso)
            {
                hasSelection = false;
                selectionBitmap?.Dispose();
                selectionBitmap = null;
                selectionMode = SelectionMode.Lasso;
                lassoPoints.Clear();
                lassoPoints.Add(p);
            }
            else if (currentTool == Tool.RectSelect)
            {
                hasSelection = false;
                selectionBitmap?.Dispose();
                selectionBitmap = null;
                selectionMode = SelectionMode.Rectangle;
            }
            else if (currentTool != Tool.Line)
            {
                ApplyTool(p, e.Button);
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = GetCanvasPoint(e.X, e.Y);
            if (p.X < 0) return;

            // Moving selection
            if (isMovingSelection && e.Button == MouseButtons.Left)
            {
                int newX = p.X - selectionOffset.X;
                int newY = p.Y - selectionOffset.Y;
                selectionRect.X = Math.Max(0, Math.Min(canvasWidth - selectionRect.Width, newX));
                selectionRect.Y = Math.Max(0, Math.Min(canvasHeight - selectionRect.Height, newY));
                UpdateCanvasDisplay();
                return;
            }

            if (!isDrawing) return;

            if (currentTool == Tool.Pencil || currentTool == Tool.Eraser || currentTool == Tool.Blur)
            {
                DrawLine(lastPoint, p, e.Button);
                lastPoint = p;
            }
            else if (currentTool == Tool.Lasso)
            {
                lassoPoints.Add(p);
                canvas.Invalidate();
            }
            else if (currentTool == Tool.RectSelect)
            {
                UpdateCanvasDisplay();
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (isMovingSelection)
            {
                isMovingSelection = false;
                canvas.Cursor = Cursors.Default;

                // Apply the moved selection to the layer
                if (selectionBitmap != null)
                {
                    using (Graphics g = Graphics.FromImage(layers[currentLayerIndex].Bitmap))
                    {
                        g.CompositingMode = CompositingMode.SourceOver;
                        g.DrawImage(selectionBitmap, selectionRect.Location);
                    }
                    selectionBitmap?.Dispose();
                    selectionBitmap = null;
                    hasSelection = false;
                }
                UpdateCanvasDisplay();
                return;
            }

            if (!isDrawing) return;

            Point p = GetCanvasPoint(e.X, e.Y);

            if (currentTool == Tool.Line && p.X >= 0)
            {
                DrawLineBresenham(startPoint, p);
            }
            else if (currentTool == Tool.RectSelect && p.X >= 0)
            {
                int x = Math.Min(startPoint.X, p.X);
                int y = Math.Min(startPoint.Y, p.Y);
                int w = Math.Abs(p.X - startPoint.X) + 1;
                int h = Math.Abs(p.Y - startPoint.Y) + 1;

                // Clamp to canvas bounds
                if (x < 0) { w += x; x = 0; }
                if (y < 0) { h += y; y = 0; }
                if (x + w > canvasWidth) w = canvasWidth - x;
                if (y + h > canvasHeight) h = canvasHeight - y;

                if (w > 0 && h > 0)
                {
                    selectionRect = new Rectangle(x, y, w, h);
                    hasSelection = true;
                    CopySelectionToBitmap();
                }
            }
            else if (currentTool == Tool.Lasso && lassoPoints.Count > 2)
            {
                CreateLassoSelection();
                if (selectionRect.Width > 0 && selectionRect.Height > 0)
                {
                    hasSelection = true;
                    CopySelectionToBitmap();
                }
            }

            isDrawing = false;
            UpdateCanvasDisplay();
        }

        private bool IsPointInSelection(Point p)
        {
            if (!hasSelection) return false;

            if (selectionMode == SelectionMode.Rectangle)
            {
                return selectionRect.Contains(p);
            }
            else if (selectionMode == SelectionMode.Lasso && lassoPoints.Count > 2)
            {
                return IsPointInPolygon(p, lassoPoints);
            }
            return false;
        }

        private bool IsPointInPolygon(Point p, List<Point> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if ((polygon[i].Y > p.Y) != (polygon[j].Y > p.Y) &&
                    p.X < (polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private void CreateLassoSelection()
        {
            if (lassoPoints == null || lassoPoints.Count < 3) return;

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (Point pt in lassoPoints)
            {
                if (pt.X < minX) minX = pt.X;
                if (pt.X > maxX) maxX = pt.X;
                if (pt.Y < minY) minY = pt.Y;
                if (pt.Y > maxY) maxY = pt.Y;
            }

            // Clamp to canvas bounds
            minX = Math.Max(0, minX);
            minY = Math.Max(0, minY);
            maxX = Math.Min(canvasWidth - 1, maxX);
            maxY = Math.Min(canvasHeight - 1, maxY);

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            if (width > 0 && height > 0)
            {
                selectionRect = new Rectangle(minX, minY, width, height);
            }
        }

        private void CopySelectionToBitmap()
        {
            // Dispose previous bitmap
            selectionBitmap?.Dispose();

            if (selectionRect.Width <= 0 || selectionRect.Height <= 0)
            {
                selectionBitmap = null;
                return;
            }

            selectionBitmap = new Bitmap(selectionRect.Width, selectionRect.Height);

            using (Graphics g = Graphics.FromImage(selectionBitmap))
            {
                g.Clear(Color.Transparent);

                for (int y = 0; y < selectionRect.Height; y++)
                {
                    for (int x = 0; x < selectionRect.Width; x++)
                    {
                        int srcX = selectionRect.X + x;
                        int srcY = selectionRect.Y + y;

                        if (srcX >= 0 && srcX < canvasWidth && srcY >= 0 && srcY < canvasHeight)
                        {
                            bool inSelection = selectionMode == SelectionMode.Rectangle ||
                                               (selectionMode == SelectionMode.Lasso && IsPointInPolygon(new Point(srcX, srcY), lassoPoints));

                            if (inSelection)
                            {
                                Color pixel = layers[currentLayerIndex].Bitmap.GetPixel(srcX, srcY);
                                selectionBitmap.SetPixel(x, y, pixel);
                                layers[currentLayerIndex].Bitmap.SetPixel(srcX, srcY, Color.Transparent);
                            }
                        }
                    }
                }
            }

            UpdateCanvasDisplay();
        }


        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (canvas.Image == null) return;

            int offsetX = (canvas.Width - canvas.Image.Width) / 2;
            int offsetY = (canvas.Height - canvas.Image.Height) / 2;

            // Draw lasso selection in progress
            if (currentTool == Tool.Lasso && lassoPoints.Count > 1 && isDrawing)
            {
                using (Pen lassoPen = new Pen(Color.FromArgb(0, 150, 255), 2))
                {
                    lassoPen.DashStyle = DashStyle.Dash;
                    for (int i = 1; i < lassoPoints.Count; i++)
                    {
                        Point p1 = lassoPoints[i - 1];
                        Point p2 = lassoPoints[i];
                        e.Graphics.DrawLine(lassoPen,
                            offsetX + p1.X * pixelSize + pixelSize / 2,
                            offsetY + p1.Y * pixelSize + pixelSize / 2,
                            offsetX + p2.X * pixelSize + pixelSize / 2,
                            offsetY + p2.Y * pixelSize + pixelSize / 2);
                    }
                }
            }
            // Draw rectangle selection in progress
            else if (currentTool == Tool.RectSelect && isDrawing)
            {
                Point p = GetCanvasPoint(canvas.PointToClient(Cursor.Position).X, canvas.PointToClient(Cursor.Position).Y);
                if (p.X >= 0)
                {
                    int x1 = Math.Min(startPoint.X, p.X);
                    int y1 = Math.Min(startPoint.Y, p.Y);
                    int w = Math.Abs(p.X - startPoint.X) + 1;
                    int h = Math.Abs(p.Y - startPoint.Y) + 1;

                    using (Pen selPen = new Pen(Color.FromArgb(0, 150, 255), 2))
                    {
                        selPen.DashStyle = DashStyle.Dash;
                        e.Graphics.DrawRectangle(selPen,
                            offsetX + x1 * pixelSize,
                            offsetY + y1 * pixelSize,
                            w * pixelSize,
                            h * pixelSize);
                    }
                }
            }

            // Draw moving selection with the bitmap
            if (hasSelection && selectionBitmap != null && selectionBitmap.Width > 0 && selectionBitmap.Height > 0)
            {
                using (Graphics g = e.Graphics)
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;

                    using (Pen selPen = new Pen(Color.FromArgb(0, 150, 255), 2))
                    {
                        selPen.DashStyle = DashStyle.Dash;
                        g.DrawRectangle(selPen,
                            offsetX + selectionRect.X * pixelSize,
                            offsetY + selectionRect.Y * pixelSize,
                            selectionRect.Width * pixelSize,
                            selectionRect.Height * pixelSize);
                    }

                    if (isMovingSelection)
                    {
                        g.DrawImage(selectionBitmap,
                            offsetX + selectionRect.X * pixelSize,
                            offsetY + selectionRect.Y * pixelSize,
                            selectionRect.Width * pixelSize,
                            selectionRect.Height * pixelSize);
                    }
                }
            }

        }

        private void ApplyTool(Point p, MouseButtons button)
        {
            Color colorToUse = Color.FromArgb(currentAlpha, currentColor);

            switch (currentTool)
            {
                case Tool.Pencil:
                    SetPixelBlend(p.X, p.Y, colorToUse);
                    break;
                case Tool.Eraser:
                    layers[currentLayerIndex].Bitmap.SetPixel(p.X, p.Y, Color.Transparent);
                    break;
                case Tool.Fill:
                    FloodFill(p.X, p.Y, layers[currentLayerIndex].Bitmap.GetPixel(p.X, p.Y), colorToUse);
                    break;
                case Tool.Eyedropper:
                    using (Bitmap composite = CompositeLayers(false))
                    {
                        Color picked = composite.GetPixel(p.X, p.Y);
                        SetColorFromPicked(picked);
                    }
                    return;
                case Tool.Blur:
                    BlurPixel(p.X, p.Y);
                    break;
            }

            UpdateCanvasDisplay();
        }

        private void SetPixelBlend(int x, int y, Color color)
        {
            if (color.A == 255)
            {
                layers[currentLayerIndex].Bitmap.SetPixel(x, y, color);
            }
            else
            {
                Color existing = layers[currentLayerIndex].Bitmap.GetPixel(x, y);
                int a = color.A + existing.A * (255 - color.A) / 255;
                int r = (color.R * color.A + existing.R * existing.A * (255 - color.A) / 255) / (a == 0 ? 1 : a);
                int g = (color.G * color.A + existing.G * existing.A * (255 - color.A) / 255) / (a == 0 ? 1 : a);
                int b = (color.B * color.A + existing.B * existing.A * (255 - color.A) / 255) / (a == 0 ? 1 : a);
                layers[currentLayerIndex].Bitmap.SetPixel(x, y, Color.FromArgb(a, r, g, b));
            }
        }

        private void DrawLine(Point p1, Point p2, MouseButtons button)
        {
            DrawLineBresenham(p1, p2);
        }

        private void DrawLineBresenham(Point p1, Point p2)
        {
            int x0 = p1.X, y0 = p1.Y, x1 = p2.X, y1 = p2.Y;
            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                ApplyTool(new Point(x0, y0), MouseButtons.Left);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private void BlurPixel(int x, int y)
        {
            int r = 0, g = 0, b = 0, a = 0, count = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && nx < canvasWidth && ny >= 0 && ny < canvasHeight)
                    {
                        Color c = layers[currentLayerIndex].Bitmap.GetPixel(nx, ny);
                        r += c.R; g += c.G; b += c.B; a += c.A;
                        count++;
                    }
                }
            }
            layers[currentLayerIndex].Bitmap.SetPixel(x, y, Color.FromArgb(a / count, r / count, g / count, b / count));
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
                if (layers[currentLayerIndex].Bitmap.GetPixel(p.X, p.Y).ToArgb() != targetColor.ToArgb()) continue;

                SetPixelBlend(p.X, p.Y, replacementColor);

                pixels.Push(new Point(p.X + 1, p.Y));
                pixels.Push(new Point(p.X - 1, p.Y));
                pixels.Push(new Point(p.X, p.Y + 1));
                pixels.Push(new Point(p.X, p.Y - 1));
            }
        }

        private void ColorSlider_Changed(object sender, EventArgs e)
        {
            float h = hueBar.Value;
            float s = satBar.Value / 100f;
            float v = valBar.Value / 100f;

            currentColor = HSVToRGB(h, s, v);
            currentAlpha = alphaBar.Value;

            lblHue.Text = $"Hue: {(int)h}°";
            lblSat.Text = $"Saturation: {(int)(s * 100)}%";
            lblVal.Text = $"Value: {(int)(v * 100)}%";

            UpdateColorPreview();
        }

        private void AlphaSlider_Changed(object sender, EventArgs e)
        {
            currentAlpha = alphaBar.Value;
            lblAlpha.Text = $"Opacity: {(int)(currentAlpha / 2.55)}%";
            UpdateColorPreview();
        }

        private void UpdateColorPreview()
        {
            currentColorDisplay.BackColor = Color.FromArgb(currentAlpha, currentColor);

            Bitmap preview = new Bitmap(250, 40);
            using (Graphics g = Graphics.FromImage(preview))
            {
                for (int y = 0; y < 40; y += 10)
                {
                    for (int x = 0; x < 250; x += 10)
                    {
                        g.FillRectangle((x / 10 + y / 10) % 2 == 0 ? Brushes.White : Brushes.LightGray,
                            x, y, 10, 10);
                    }
                }
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(currentAlpha, currentColor)))
                {
                    g.FillRectangle(brush, 0, 0, 250, 40);
                }
            }
            colorPreview.BackgroundImage?.Dispose();
            colorPreview.BackgroundImage = preview;
        }

        private Color HSVToRGB(float h, float s, float v)
        {
            int hi = Convert.ToInt32(Math.Floor(h / 60)) % 6;
            float f = h / 60 - (float)Math.Floor(h / 60);

            v = v * 255;
            int vInt = Convert.ToInt32(v);
            int p = Convert.ToInt32(v * (1 - s));
            int q = Convert.ToInt32(v * (1 - f * s));
            int t = Convert.ToInt32(v * (1 - (1 - f) * s));

            if (hi == 0) return Color.FromArgb(255, vInt, t, p);
            else if (hi == 1) return Color.FromArgb(255, q, vInt, p);
            else if (hi == 2) return Color.FromArgb(255, p, vInt, t);
            else if (hi == 3) return Color.FromArgb(255, p, q, vInt);
            else if (hi == 4) return Color.FromArgb(255, t, p, vInt);
            else return Color.FromArgb(255, vInt, p, q);
        }

        private void RGBToHSV(Color color, out float h, out float s, out float v)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            h = color.GetHue();
            s = (max == 0) ? 0 : 1f - (1f * min / max);
            v = max / 255f;
        }

        private void SetColorFromPicked(Color color)
        {
            currentColor = Color.FromArgb(255, color.R, color.G, color.B);
            currentAlpha = color.A;

            float h, s, v;
            RGBToHSV(color, out h, out s, out v);

            hueBar.Value = (int)h;
            satBar.Value = (int)(s * 100);
            valBar.Value = (int)(v * 100);
            alphaBar.Value = color.A;

            UpdateColorPreview();
        }

        private void PresetColor_Click(object sender, EventArgs e)
        {
            Panel panel = (Panel)sender;
            SetColorFromPicked((Color)panel.Tag);
        }

        private void ToolButton_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            currentTool = (Tool)btn.Tag;

            foreach (Control ctrl in toolPanel.Controls)
            {
                if (ctrl is Button b && b != btnClear && b != btnExport && b != btnNewCanvas)
                    b.BackColor = Color.FromArgb(60, 60, 60);
            }
            btn.BackColor = Color.FromArgb(0, 122, 204);

            if (currentTool != Tool.Lasso && currentTool != Tool.RectSelect)
            {
                hasSelection = false;
                lassoPoints.Clear();
                selectionBitmap?.Dispose();
                selectionBitmap = null;
                UpdateCanvasDisplay();
            }
        }

        private void ZoomBar_ValueChanged(object sender, EventArgs e)
        {
            pixelSize = zoomBar.Value;
            int zoomPercent = (pixelSize * 100 / 16);
            zoomLabel.Text = $"Zoom: {zoomPercent}%";
            UpdateCanvasDisplay();
        }

        private void ClearCanvas()
        {
            if (MessageBox.Show("Clear the current layer?", "Clear",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SaveUndo();
                using (Graphics g = Graphics.FromImage(layers[currentLayerIndex].Bitmap))
                {
                    g.Clear(Color.Transparent);
                }
                UpdateCanvasDisplay();
            }
        }

        private void SaveUndo()
        {
            List<Layer> state = new List<Layer>();
            foreach (Layer layer in layers)
            {
                state.Add(layer.Clone());
            }
            undoStack.Push(state);
            redoStack.Clear();

            if (undoStack.Count > 50)
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
                var currentState = new List<Layer>();
                foreach (Layer layer in layers)
                {
                    currentState.Add(layer.Clone());
                }
                redoStack.Push(currentState);

                layers = undoStack.Pop();
                UpdateLayerList();
                UpdateCanvasDisplay();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.Y && redoStack.Count > 0)
            {
                var currentState = new List<Layer>();
                foreach (Layer layer in layers)
                {
                    currentState.Add(layer.Clone());
                }
                undoStack.Push(currentState);

                layers = redoStack.Pop();
                UpdateLayerList();
                UpdateCanvasDisplay();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete && hasSelection)
            {
                SaveUndo();
                for (int y = selectionRect.Y; y < selectionRect.Y + selectionRect.Height; y++)
                {
                    for (int x = selectionRect.X; x < selectionRect.X + selectionRect.Width; x++)
                    {
                        if (x >= 0 && x < canvasWidth && y >= 0 && y < canvasHeight)
                        {
                            Point p = new Point(x, y);
                            bool inSelection = false;

                            if (selectionMode == SelectionMode.Rectangle)
                            {
                                inSelection = true;
                            }
                            else if (selectionMode == SelectionMode.Lasso)
                            {
                                inSelection = IsPointInPolygon(p, lassoPoints);
                            }

                            if (inSelection)
                            {
                                layers[currentLayerIndex].Bitmap.SetPixel(x, y, Color.Transparent);
                            }
                        }
                    }
                }
                hasSelection = false;
                UpdateCanvasDisplay();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape && hasSelection)
            {
                hasSelection = false;
                lassoPoints.Clear();
                selectionBitmap?.Dispose();
                selectionBitmap = null;
                UpdateCanvasDisplay();
                e.Handled = true;
            }
        }

        private void ExportImage()
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Sprite Sheet|*.png";
                dialog.DefaultExt = "png";
                dialog.FileName = $"pixel_art_{canvasWidth}x{canvasHeight}";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (dialog.FilterIndex == 3)
                    {
                        ExportAsSpriteSheet(dialog.FileName);
                    }
                    else
                    {
                        using (Bitmap finalImage = CompositeLayers(false))
                        {
                            if (dialog.FileName.EndsWith(".jpg"))
                            {
                                using (Bitmap jpgBitmap = new Bitmap(canvasWidth, canvasHeight))
                                {
                                    using (Graphics g = Graphics.FromImage(jpgBitmap))
                                    {
                                        g.Clear(Color.White);
                                        g.DrawImage(finalImage, 0, 0);
                                    }
                                    jpgBitmap.Save(dialog.FileName, ImageFormat.Jpeg);
                                }
                            }
                            else
                            {
                                finalImage.Save(dialog.FileName, ImageFormat.Png);
                            }
                        }

                        MessageBox.Show("Image exported successfully!", "Export",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void ExportAsSpriteSheet(string filename)
        {
            using (SpriteSheetDialog dialog = new SpriteSheetDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    int rows = dialog.Rows;
                    int cols = dialog.Columns;
                    int spacing = dialog.Spacing;

                    int sheetWidth = (canvasWidth * cols) + (spacing * (cols + 1));
                    int sheetHeight = (canvasHeight * rows) + (spacing * (rows + 1));

                    using (Bitmap spriteSheet = new Bitmap(sheetWidth, sheetHeight))
                    {
                        using (Graphics g = Graphics.FromImage(spriteSheet))
                        {
                            g.Clear(Color.Transparent);

                            using (Bitmap finalImage = CompositeLayers(false))
                            {
                                for (int row = 0; row < rows; row++)
                                {
                                    for (int col = 0; col < cols; col++)
                                    {
                                        int x = spacing + (col * (canvasWidth + spacing));
                                        int y = spacing + (row * (canvasHeight + spacing));
                                        g.DrawImage(finalImage, x, y, canvasWidth, canvasHeight);
                                    }
                                }
                            }
                        }

                        spriteSheet.Save(filename, ImageFormat.Png);
                        MessageBox.Show($"Sprite sheet exported successfully!\nSize: {sheetWidth}x{sheetHeight} px",
                            "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void ShowNewCanvasDialog()
        {
            if (MessageBox.Show("Create a new canvas? Current work will be lost.", "New Canvas",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                using (StartScreenDialog dialog = new StartScreenDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        canvasWidth = dialog.CanvasWidth;
                        canvasHeight = dialog.CanvasHeight;
                        undoStack.Clear();
                        redoStack.Clear();
                        InitializeCanvas();
                    }
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            foreach (Layer layer in layers)
            {
                layer.Bitmap?.Dispose();
            }
            canvas.Image?.Dispose();
            selectionBitmap?.Dispose();
            base.OnFormClosing(e);
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (StartScreenDialog startDialog = new StartScreenDialog())
            {
                if (startDialog.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new PixelArtForm(startDialog.CanvasWidth, startDialog.CanvasHeight));
                }
            }
        }
    }

    public class SpriteSheetDialog : Form
    {
        private NumericUpDown rowsInput, colsInput, spacingInput;
        private Button btnOK, btnCancel;

        public int Rows { get; private set; }
        public int Columns { get; private set; }
        public int Spacing { get; private set; }

        public SpriteSheetDialog()
        {
            this.Text = "Sprite Sheet Export";
            this.Size = new Size(350, 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);

            Label lblInfo = new Label
            {
                Text = "Create a sprite sheet by tiling the current image:",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                Size = new Size(300, 30)
            };

            Label lblRows = new Label { Text = "Rows:", Font = new Font("Segoe UI", 10), ForeColor = Color.White, Location = new Point(30, 60), AutoSize = true };
            rowsInput = new NumericUpDown { Location = new Point(150, 58), Width = 160, Font = new Font("Segoe UI", 10), Minimum = 1, Maximum = 20, Value = 1 };

            Label lblCols = new Label { Text = "Columns:", Font = new Font("Segoe UI", 10), ForeColor = Color.White, Location = new Point(30, 100), AutoSize = true };
            colsInput = new NumericUpDown { Location = new Point(150, 98), Width = 160, Font = new Font("Segoe UI", 10), Minimum = 1, Maximum = 20, Value = 1 };

            Label lblSpacing = new Label { Text = "Spacing (px):", Font = new Font("Segoe UI", 10), ForeColor = Color.White, Location = new Point(30, 140), AutoSize = true };
            spacingInput = new NumericUpDown { Location = new Point(150, 138), Width = 160, Font = new Font("Segoe UI", 10), Minimum = 0, Maximum = 10, Value = 0 };

            btnOK = new Button
            {
                Text = "Export",
                Location = new Point(130, 190),
                Size = new Size(90, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK,
                Cursor = Cursors.Hand
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += (s, e) => { Rows = (int)rowsInput.Value; Columns = (int)colsInput.Value; Spacing = (int)spacingInput.Value; };

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(230, 190),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { lblInfo, lblRows, rowsInput, lblCols, colsInput, lblSpacing, spacingInput, btnOK, btnCancel });
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}