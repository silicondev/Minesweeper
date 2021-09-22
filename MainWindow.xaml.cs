using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        public static Difficulty EASY = new Difficulty("Easy", (10, 10), 5);
        public static Difficulty MEDIUM = new Difficulty("Medium", (20, 30), 10);
        public static Difficulty HARD = new Difficulty("Hard", (50, 70), 15);

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

                    btn.PreviewMouseDown += OnClick;

                    Grid.SetColumn(btn, x);
                    Grid.SetRow(btn, y);

                    Tiles[x, y] = new Tile(btn, (x, y), rng.Next(100) + 1 <= CurrentDifficulty.Chance);

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
                Tiles[loc.X, loc.Y].Show(this, true);
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
                    if (Tiles[x, y].Status != TileStatus.SHOWN && !Tiles[x, y].IsBomb)
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

        public void ShowAllBombs(Point exclude)
        {
            for (int y = 0; y < CurrentDifficulty.Size.Y; y++)
            {
                for (int x = 0; x < CurrentDifficulty.Size.X; x++)
                {
                    if (Tiles[x, y].IsBomb && (x, y) != exclude)
                        Tiles[x, y].Show(this, false, false);
                }
            }
        }

        public void Win()
        {
            DateTime currentDate = DateTime.Now;
            TimeSpan currentTime = Watch.Elapsed;
            currentDate = DateTime.Parse($"{currentDate:dd/MM/yyyy hh:mm:ss}");

            string str = $"You Win!\rYour time was {currentTime:hh\\:mm\\:ss\\.ff}\rPlay Again?\r";

            var newLb = new List<string>();
            int num = 1;
            int addNum = -1;
            if (File.Exists($"leaderboard{CurrentDifficulty.Name}.csv"))
            {
                string[] lb = File.ReadAllLines($"leaderboard{CurrentDifficulty.Name}.csv");
                bool placed = false;
                string currentStr = $"{currentDate:dd/MM/yyyy hh:mm:ss},{currentTime:hh\\,mm\\,ss\\,ff}";
                foreach (var val in lb)
                {
                    string[] vals = val.Split(',');
                    int hours = int.Parse(vals[2]);
                    int minutes = int.Parse(vals[3]);
                    int seconds = int.Parse(vals[4]);
                    int miliseconds = int.Parse(vals[5]);

                    TimeSpan span = new TimeSpan(0, hours, minutes, seconds, miliseconds);

                    if (span > currentTime && !placed)
                    {
                        newLb.Add($"{num},{currentStr}");
                        addNum = num;
                        num++;
                        placed = true;
                    }

                    newLb.Add($"{num},{vals[1]},{span:hh\\,mm\\,ss\\,ff}");
                    num++;
                }

                if (!placed)
                {
                    newLb.Add($"{num},{currentStr}");
                    addNum = num;
                }

                File.Delete($"leaderboard{CurrentDifficulty.Name}.csv");
            } else
            {
                newLb.Add($"{num},{currentDate:dd/MM/yyyy hh:mm:ss},{currentTime:hh\\,mm\\,ss\\,ff}");
                addNum = num;
            }

            using (File.Create($"leaderboard{CurrentDifficulty.Name}.csv")) { }
            File.WriteAllLines($"leaderboard{CurrentDifficulty.Name}.csv", newLb.ToArray());

            bool found = false;
            foreach (var val in newLb.Count >= 10 ? newLb.GetRange(0, 10) : newLb)
            {
                string[] vals = val.Split(',');

                str += "\r";

                if (int.Parse(vals[0]) == addNum)
                {
                    str += "> ";
                    found = true;
                }

                str += $"{vals[0]}: {vals[1]} - {vals[2]}:{vals[3]}:{vals[4]}.{vals[5]}";
            }

            if (!found)
            {
                string val = newLb.Find(x => x.StartsWith(addNum.ToString()));
                string[] vals = val.Split(',');
                str += $"\r> {vals[0]}: {vals[1]} - {vals[2]}:{vals[3]}:{vals[4]}.{vals[5]}";
            }

            Watch.Stop();
            if (MessageBox.Show(str, "Well Done!", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
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
        public string Name { get; }
        public Point Size { get; }
        public int Chance { get; }

        public Difficulty(string name, Point size, int chance)
        {
            Size = size;
            Chance = chance;
            Name = name;
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
            Update(false);
        }

        public void Show(MainWindow window, bool click, bool showOthers = true)
        {
            if (Status != TileStatus.SHOWN)
            {
                if (IsBomb && Status != TileStatus.FLAGGED)
                {
                    Status = TileStatus.SHOWN;
                    Update(click, window);
                    if (click)
                        window.Lose();
                }
                else if ((click && Status != TileStatus.FLAGGED) || !click)
                {
                    Status = TileStatus.SHOWN;
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

                    if (Value == 0 && showOthers)
                    {
                        if (l) window.Tiles[Location.X - 1, Location.Y].Show(window, false);
                        if (t) window.Tiles[Location.X, Location.Y - 1].Show(window, false);
                        if (r) window.Tiles[Location.X + 1, Location.Y].Show(window, false);
                        if (b) window.Tiles[Location.X, Location.Y + 1].Show(window, false);
                        if (l && t) window.Tiles[Location.X - 1, Location.Y - 1].Show(window, false);
                        if (t && r) window.Tiles[Location.X + 1, Location.Y - 1].Show(window, false);
                        if (r && b) window.Tiles[Location.X + 1, Location.Y + 1].Show(window, false);
                        if (b && l) window.Tiles[Location.X - 1, Location.Y + 1].Show(window, false);
                    }
                }
                window.CheckGrid();
            }
            Update(click);
        }

        public void Flag()
        {
            if (Status == TileStatus.HIDDEN)
                Status = TileStatus.FLAGGED;
            else if (Status == TileStatus.FLAGGED)
                Status = TileStatus.HIDDEN;
            Update(false);
        }

        public void Update(bool click, MainWindow window = null)
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
                        if (click)
                        {
                            Btn.Background = Brushes.Red;
                            if (window != null)
                                window.ShowAllBombs(Location);
                        }

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
