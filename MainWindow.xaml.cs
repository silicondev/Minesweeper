using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Minesweeper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Difficulty CurrentDifficulty { get; set; }
        public static Difficulty EASY = new Difficulty((10, 10), 5);
        public static Difficulty MEDIUM = new Difficulty((20, 30), 10);
        public static Difficulty HARD = new Difficulty((50, 70), 15);

        public static int ButtonSize = 18;

        public Tile[,] Tiles;

        public Stopwatch Watch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();

            Startup();
        }

        public void Startup()
        {
            Watch.Stop();
            Watch.Reset();
            Hide();
            Menu menu = new Menu();
            menu.Show();
            menu.Closed += OnSetup;
        }

        public void OnSetup(object sender, EventArgs e)
        {
            Show();

            var menu = sender as Menu;
            if (menu == null) return;

            switch (menu.Difficulty)
            {
                case 1:
                    CurrentDifficulty = EASY;
                    break;
                case 2:
                    CurrentDifficulty = MEDIUM;
                    break;
                case 3:
                    CurrentDifficulty = HARD;
                    break;
            }

            Tiles = new Tile[CurrentDifficulty.Size.X, CurrentDifficulty.Size.Y];
            Random rng = new Random();

            MinesweeperGrid.ColumnDefinitions.Clear();
            MinesweeperGrid.RowDefinitions.Clear();

            for (int y = 0; y < CurrentDifficulty.Size.Y; y++)
            {
                var row = new RowDefinition
                {
                    Height = new GridLength(ButtonSize)
                };
                MinesweeperGrid.RowDefinitions.Add(row);

                for (int x = 0; x < CurrentDifficulty.Size.X; x++)
                {
                    var col = new ColumnDefinition
                    {
                        Width = new GridLength(ButtonSize)
                    };

                    var btn = new Button
                    {
                        Name = $"button_{x}_{y}",
                        Height = ButtonSize,
                        Width = ButtonSize,
                        Content = "",
                        FontWeight = FontWeights.Bold,
                        FontSize = 9,
                        Tag = new Point(x, y)
                    };

                    int chance = rng.Next(100) + 1;

                    btn.PreviewMouseDown += OnClick;

                    Grid.SetColumn(btn, x);
                    Grid.SetRow(btn, y);

                    Tiles[x, y] = new Tile(btn, (x, y), chance <= CurrentDifficulty.Chance);

                    MinesweeperGrid.ColumnDefinitions.Add(col);
                    MinesweeperGrid.Children.Add(btn);
                }
            }

            var gridSizeX = CurrentDifficulty.Size.X * ButtonSize;
            var gridSizeY = CurrentDifficulty.Size.Y * ButtonSize;

            WindowGrid.Width = gridSizeX + 20;
            WindowGrid.Height = gridSizeY + 20;

            Watch.Start();
        }

        public void OnClick(object sender, MouseButtonEventArgs e)
        {
            Button btn = sender as Button;
            Point loc = btn.Tag as Point;

            if (e.ChangedButton == MouseButton.Right)
            {
                Tiles[loc.X, loc.Y].Flag();
            } else if (e.ChangedButton == MouseButton.Left)
            {
                Tiles[loc.X, loc.Y].Show(this);
            }
            e.Handled = true;
        }

        public void CheckGrid()
        {
            Watch.Stop();
            bool complete = true;
            for (int y = 0; y < CurrentDifficulty.Size.Y && complete; y++)
            {
                for (int x = 0; x < CurrentDifficulty.Size.X && complete; x++)
                {
                    if (Tiles[x, y].Status == TileStatus.HIDDEN && !Tiles[x, y].IsBomb)
                        complete = false;
                }
            }

            if (complete)
            {
                Win();
            } else
            {
                Watch.Start();
            }
        }

        public void Win()
        {
            Watch.Stop();
            if (MessageBox.Show($"You Win!\rYour time was {Watch.Elapsed}\rPlay Again?", "Well Done!", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                Startup();
            else
                Environment.Exit(0);
        }

        public void Lose()
        {
            Watch.Stop();
            if (MessageBox.Show("You Lost! Play Again?", "BOOM!", MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.Yes)
                Startup();
            else
                Environment.Exit(0);
        }
    }

    public class Difficulty
    {
        public Point Size { get; }
        public int Chance { get; }

        public Difficulty(Point size, int chance)
        {
            Size = size;
            Chance = chance;
        }
    }

    public class Tile
    {
        public static string DefaultValue = "";
        public bool IsBomb { get; }
        public Point Location { get; }
        public TileStatus Status { get; private set; } = TileStatus.HIDDEN;
        public int Value { get; set; } = 0;
        public Button Btn { get; set; }

        public Tile(Button btn, Point loc, bool isBomb)
        {
            Btn = btn;
            Location = loc;
            IsBomb = isBomb;
            Update();
        }

        public void Show(MainWindow window)
        {
            if (Status == TileStatus.HIDDEN)
            {
                Status = TileStatus.SHOWN;
                if (IsBomb)
                {
                    Update();
                    window.Lose();
                }
                else
                {
                    Value = 0;

                    bool l = Location.X > 0;
                    bool t = Location.Y > 0;
                    bool r = Location.X < window.CurrentDifficulty.Size.X - 1;
                    bool b = Location.Y < window.CurrentDifficulty.Size.Y - 1;

                    if (l && window.Tiles[Location.X - 1, Location.Y].IsBomb)
                        Value++;

                    if (t && window.Tiles[Location.X, Location.Y - 1].IsBomb)
                        Value++;

                    if (r && window.Tiles[Location.X + 1, Location.Y].IsBomb)
                        Value++;

                    if (b && window.Tiles[Location.X, Location.Y + 1].IsBomb)
                        Value++;

                    if (l && t && window.Tiles[Location.X - 1, Location.Y - 1].IsBomb)
                        Value++;

                    if (t && r && window.Tiles[Location.X + 1, Location.Y - 1].IsBomb)
                        Value++;

                    if (r && b && window.Tiles[Location.X + 1, Location.Y + 1].IsBomb)
                        Value++;

                    if (b && l && window.Tiles[Location.X - 1, Location.Y + 1].IsBomb)
                        Value++;

                    if (Value == 0)
                    {
                        if (l) window.Tiles[Location.X - 1, Location.Y].Show(window);
                        if (t) window.Tiles[Location.X, Location.Y - 1].Show(window);
                        if (r) window.Tiles[Location.X + 1, Location.Y].Show(window);
                        if (b) window.Tiles[Location.X, Location.Y + 1].Show(window);
                        if (l && t) window.Tiles[Location.X - 1, Location.Y - 1].Show(window);
                        if (t && r) window.Tiles[Location.X + 1, Location.Y - 1].Show(window);
                        if (r && b) window.Tiles[Location.X + 1, Location.Y + 1].Show(window);
                        if (b && l) window.Tiles[Location.X - 1, Location.Y + 1].Show(window);
                    }
                }
                window.CheckGrid();
            }
            Update();
        }

        public void Flag()
        {
            if (Status == TileStatus.HIDDEN)
                Status = TileStatus.FLAGGED;
            else if (Status == TileStatus.FLAGGED)
                Status = TileStatus.HIDDEN;
            Update();
        }

        public void Update()
        {
            Btn.Background = Brushes.LightGray;
            switch (Status)
            {
                case TileStatus.HIDDEN:
                    Btn.Foreground = Brushes.Black;
                    Btn.Content = DefaultValue;
                    break;
                case TileStatus.FLAGGED:
                    Btn.Content = new Image
                    {
                        Source = new BitmapImage(new Uri("Resources\\Flag.png", UriKind.Relative)),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    break;
                default:
                    if (IsBomb)
                    {
                        Btn.Background = Brushes.Red;
                        Btn.Content = new Image
                        {
                            Source = new BitmapImage(new Uri("Resources\\Bomb.png", UriKind.Relative)),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                    } else
                    {
                        Btn.Background = Brushes.White;
                        Brush br = Brushes.Black;
                        switch (Value)
                        {
                            case 1:
                                br = Brushes.Blue;
                                break;
                            case 2:
                                br = Brushes.DarkGreen;
                                break;
                            case 3:
                                br = Brushes.Red;
                                break;
                            case 4:
                                br = Brushes.DarkBlue;
                                break;
                            case 5:
                                br = Brushes.Maroon;
                                break;
                            case 6:
                                br = Brushes.Cyan;
                                break;
                            case 7:
                                br = Brushes.Black;
                                break;
                            case 8:
                                br = Brushes.DarkGray;
                                break;
                        }
                        Btn.Foreground = br;
                        Btn.Content = Value > 0 ? Value.ToString() : "";
                    }
                    break;
            }
            //Btn.IsEnabled = Status != TileStatus.SHOWN;
        }
    }

    public enum TileStatus
    {
        HIDDEN,
        SHOWN,
        FLAGGED
    }

    public class Point : IEquatable<Point>
    {
        public int X { get; set; }
        public int Y { get; set; }

        public static implicit operator Point((int x, int y) value)
        {
            return new Point(value);
        }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Point((int x, int y) value)
        {
            X = value.x;
            Y = value.y;
        }

        public bool Equals(Point other) => this.X == other.X && this.Y == other.Y;
        public override bool Equals(object obj) => obj is Point point ? Equals(point) : false;

        public static bool operator ==(Point a, Point b) => a.Equals(b);
        public static bool operator !=(Point a, Point b) => !a.Equals(b);

        public override int GetHashCode() => ((2773 + X.GetHashCode()) * 59) + Y.GetHashCode();
    }
}
