using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CafeMenu
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MenuForm());
        }
    }

    internal sealed class Product
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int Id { get; set; }
        public int? SourceId { get; set; }
        public string Portion { get; set; }
    }

    internal static class MenuData
    {
        public static List<Product> Load()
        {
            var connectionString = Environment.GetEnvironmentVariable("CAFE_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = "Server=localhost;Database=cafe;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5;";

            const string query = @"
                SELECT p.Name, c.Name, p.Price, p.Stock, p.SourceId, p.Portion, p.Id
                FROM dbo.Products p
                JOIN dbo.Categories c ON c.Id = p.CategoryId
                WHERE p.Available = 1
                ORDER BY c.Name, p.Name, p.Id;";

            var result = new List<Product>();
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new Product
                        {
                            Name = reader.GetString(0),
                            Category = reader.GetString(1),
                            Price = reader.GetDecimal(2),
                            Stock = reader.GetInt32(3),
                            SourceId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            Portion = reader.IsDBNull(5) ? "" : reader.GetString(5), Id = reader.GetInt32(6)
                        });
                    }
                }
            }

            return result;
        }
    }

    public sealed class MenuForm : Form
    {
        private readonly TextBox search = new TextBox { Dock = DockStyle.Top };
        private readonly ComboBox category = MakeComboBox();
        private readonly ComboBox sort = MakeComboBox();
        private readonly Button refresh = new Button { Text = "Обновить", AutoSize = true };
        private readonly Label status = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        private readonly TableLayoutPanel cards = new TableLayoutPanel
        {
            ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top
        };
        private readonly CultureInfo russian = CultureInfo.GetCultureInfo("ru-RU");
        private List<Product> products = new List<Product>();

        public MenuForm()
        {
            Text = "Меню кафе";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(760, 460);
            Size = new Size(1050, 680);
            Font = new Font("Arial", 10);
            BackColor = Color.White;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 4 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            Controls.Add(layout);

            layout.Controls.Add(MakeLabel("Меню кафе", true), 0, 0);
            var filters = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            filters.Controls.Add(MakeLabel("Поиск по названию"), 0, 0);
            filters.Controls.Add(MakeLabel("Категория"), 1, 0);
            filters.Controls.Add(MakeLabel("Сортировка по цене"), 2, 0);
            filters.Controls.Add(search, 0, 1);
            filters.Controls.Add(category, 1, 1);
            filters.Controls.Add(sort, 2, 1);
            var reset = new Button { Text = "Сбросить", AutoSize = true };
            filters.Controls.Add(reset, 3, 1);
            layout.Controls.Add(filters, 0, 1);

            var viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            viewport.Controls.Add(cards);
            layout.Controls.Add(viewport, 0, 2);
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footer.Controls.Add(status, 0, 0);
            footer.Controls.Add(refresh, 1, 0);
            layout.Controls.Add(footer, 0, 3);

            category.Items.Add("Все категории");
            category.SelectedIndex = 0;
            sort.Items.AddRange(new object[] { "Без сортировки", "Сначала дешевле", "Сначала дороже" });
            sort.SelectedIndex = 0;
            search.TextChanged += (sender, args) => RenderCards();
            category.SelectedIndexChanged += (sender, args) => RenderCards();
            sort.SelectedIndexChanged += (sender, args) => RenderCards();
            reset.Click += (sender, args) =>
            {
                search.Clear();
                category.SelectedIndex = 0;
                sort.SelectedIndex = 0;
            };
            refresh.Click += async (sender, args) => await ReloadAsync();
            Shown += async (sender, args) => await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            refresh.Enabled = false;
            try
            {
                status.Text = "Загрузка меню…";
                products = await Task.Run(() => MenuData.Load());
                var previousCategory = category.SelectedItem as string;
                category.Items.Clear();
                category.Items.Add("Все категории");
                foreach (var name in products.Select(product => product.Category).Distinct().OrderBy(name => name))
                    category.Items.Add(name);
                category.SelectedIndex = previousCategory == null ? 0 : Math.Max(0, category.Items.IndexOf(previousCategory));
            }
            catch (Exception error)
            {
                status.Text = "Ошибка загрузки меню";
                MessageBox.Show(this, error.Message, "База данных cafe", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                refresh.Enabled = true;
            }
        }

        private void RenderCards()
        {
            var filter = search.Text.Trim();
            IEnumerable<Product> visible = products.Where(product =>
                (category.SelectedIndex <= 0 || product.Category == (string)category.SelectedItem) &&
                russian.CompareInfo.IndexOf(product.Name, filter, CompareOptions.IgnoreCase) >= 0);
            if (sort.SelectedIndex == 1)
                visible = visible.OrderBy(product => product.Price);
            if (sort.SelectedIndex == 2)
                visible = visible.OrderByDescending(product => product.Price);

            cards.SuspendLayout();
            while (cards.Controls.Count > 0)
                cards.Controls[0].Dispose();
            cards.RowStyles.Clear();
            cards.RowCount = 0;
            foreach (var product in visible)
            {
                cards.RowStyles.Add(new RowStyle(SizeType.Absolute, 143));
                cards.RowCount++;
                cards.Controls.Add(CreateCard(product), 0, cards.RowCount - 1);
            }

            cards.ResumeLayout();
            status.Text = cards.RowCount == 0 ? "Товары не найдены" : "Найдено товаров: " + cards.RowCount;
        }

        private Control CreateCard(Product product)
        {
            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 3, 8), Padding = new Padding(10)
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 174));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));

            var photoName = product.SourceId.HasValue
                ? product.SourceId.Value + ".jpg"
                : "product-" + product.Id + ".jpg";
            var photoPath = Path.Combine(Path.GetDirectoryName(typeof(MenuForm).Assembly.Location), "Photos", photoName);
            if (File.Exists(photoPath))
            {
                var photo = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
                using (var image = Image.FromFile(photoPath))
                    photo.Image = new Bitmap(image);
                photo.Disposed += (sender, args) => photo.Image.Dispose();
                card.Controls.Add(photo, 0, 0);
            }
            else
            {
                card.Controls.Add(new Label
                {
                    Text = "Фото", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                    BorderStyle = BorderStyle.FixedSingle, ForeColor = Color.DimGray
                }, 0, 0);
            }

            var details = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, Padding = new Padding(12, 2, 0, 0)
            };
            details.Controls.Add(MakeLabel(product.Category + " | " + product.Name, true));
            if (!string.IsNullOrEmpty(product.Portion))
                details.Controls.Add(MakeLabel("Порция: " + product.Portion));
            details.Controls.Add(MakeLabel("Цена: " + product.Price.ToString("N2", russian) + " ₽"));
            details.Controls.Add(MakeLabel("Остаток: " + product.Stock + " шт."));
            card.Controls.Add(details, 1, 0);

            card.Controls.Add(new Label
            {
                Text = product.Stock > 0 ? "В наличии" : "Нет в наличии",
                ForeColor = product.Stock > 0 ? Color.DarkGreen : Color.DimGray,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter
            }, 2, 0);
            return card;
        }

        private static ComboBox MakeComboBox()
        {
            return new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
        }

        private static Label MakeLabel(string text, bool bold = false)
        {
            return new Label
            {
                AutoSize = true, Text = text,
                Font = bold ? new Font("Arial", 12, FontStyle.Bold) : new Font("Arial", 10)
            };
        }
    }
}




