using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ParabolaSimulation
{
    public class App : Application
    {
        [STAThread]
        public static void Main()
        {
            App app = new App();
            app.Run(new MainWindow());
        }
    }

    public class MainWindow : Window
    {
        private TextBox velocityInput, angleInput, timeStepInput, massInput, ResKoefInput;
        private TextBlock resultBlock;
        private TextBlock timeDisplay;
        private Canvas drawingCanvas;

        public MainWindow()
        {
            Title = "Моделирование полета птицы";
            Width = 1000;
            Height = 800;

            DockPanel dock = new DockPanel();
            Menu menu = CreateMenu();
            DockPanel.SetDock(menu, Dock.Top);
            dock.Children.Add(menu);

            ScrollViewer scrollViewer = new ScrollViewer();
            StackPanel panel = new StackPanel { Margin = new Thickness(10) };

            panel.Children.Add(new TextBlock { Text = "Укажите скорость тела:" });
            velocityInput = new TextBox();
            panel.Children.Add(velocityInput);

            panel.Children.Add(new TextBlock { Text = "Укажите угол броска (градусы):" });
            angleInput = new TextBox();
            panel.Children.Add(angleInput);

            panel.Children.Add(new TextBlock { Text = "Укажите шаг времени dt:" });
            timeStepInput = new TextBox();
            panel.Children.Add(timeStepInput);

            panel.Children.Add(new TextBlock { Text = "Укажите массу птицы:" });
            massInput = new TextBox();
            panel.Children.Add(massInput);

            panel.Children.Add(new TextBlock { Text = "Укажите коэффициент сопротивления воздуха:" });
            ResKoefInput = new TextBox();
            panel.Children.Add(ResKoefInput);

            Button calculateButton = new Button { Content = "Рассчитать" };
            calculateButton.Click += CalculateButton_Click;
            panel.Children.Add(calculateButton);

            resultBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10),
                FontSize = 16
            };
            panel.Children.Add(resultBlock);

            drawingCanvas = new Canvas
            {
                Background = Brushes.White,
                Height = 300,
                Margin = new Thickness(10)
            };
            panel.Children.Add(drawingCanvas);


            timeDisplay = new TextBlock
            {
                FontSize = 18,
                Margin = new Thickness(10),
                Text = "Время полёта: 0.00 с"
            };
            panel.Children.Add(timeDisplay);

            scrollViewer.Content = panel;
            dock.Children.Add(scrollViewer);
            Content = dock;
        }

        private Menu CreateMenu()
        {
            Menu mainMenu = new Menu();

            MenuItem basicMenu = new MenuItem { Header = "Меню" };
            MenuItem exitItem = new MenuItem { Header = "Выход" };
            MenuItem clearInputItem = new MenuItem { Header = "Очистить ввод" };

            clearInputItem.Click += (s, e) =>
            {
                MessageBoxResult result = MessageBox.Show(
                    "Вы уверены, что хотите очистить введённые данные?",
                    "Подтверждение действия",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    ClearInputFields();
                }
            };
            exitItem.Click += (s, e) =>
            {
                MessageBoxResult result = MessageBox.Show(
                    "Вы уверены, что хотите выйти?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Close();
                }
            };

            basicMenu.Items.Add(clearInputItem);
            basicMenu.Items.Add(exitItem);

            MenuItem helpMenu = new MenuItem { Header = "Справка" };
            MenuItem aboutItem = new MenuItem { Header = "О программе" };

            aboutItem.Click += (s, e) =>
            {
                MessageBox.Show("Игра про птичку\n2025", "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
            };

            helpMenu.Items.Add(aboutItem);

            mainMenu.Items.Add(basicMenu);
            mainMenu.Items.Add(helpMenu);

            return mainMenu;
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            resultBlock.Text = "";
            drawingCanvas.Children.Clear();
            timeDisplay.Text = "Время полёта: 0.00 с";

            if (double.TryParse(velocityInput.Text, out double V1) &&
                double.TryParse(angleInput.Text, out double ugol1) &&
                double.TryParse(timeStepInput.Text, out double dt) &&
                double.TryParse(massInput.Text, out double m) &&
                double.TryParse(ResKoefInput.Text, out double k))
            {
                Parabola p = new Parabola();
                p.InformLanding += message => resultBlock.Text += message + "\n";

                p.LaunchConditions(V1, ugol1, m, k);
                p.EulerMethodForBirdGame(p.V, p.ugol, dt, k);

                resultBlock.Text += $"Дальность полёта: {p.x_max:F2} м\n";
                resultBlock.Text += $"Максимальная высота: {p.y_max:F2} м\n";
                resultBlock.Text += $"Время полёта: {p.T_max:F2} с\n";

                p.DrawGrid(drawingCanvas);
                p.TrajectoryAnimation(drawingCanvas, timeDisplay, k);
            }
            else
            {
                MessageBox.Show("Ошибка ввода! Используйте только числа. Дроби — через запятую.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearInputFields()
        {
            velocityInput.Clear();
            angleInput.Clear();
            timeStepInput.Clear();
            massInput.Clear();
            ResKoefInput.Clear();
            resultBlock.Text = "";
            timeDisplay.Text = "Время полёта: 0.00 с";
            drawingCanvas.Children.Clear();
        }
    }

    public class Parabola
    {
        private double g = 9.81, pi = Math.PI;
        public double V, ugol, T_max, x_max, y_max, X, Y, X0, Y0, t0, Vx, Vy, dt, M, t, k0;

        public delegate void BirdFall(string message);
        public event BirdFall InformLanding;

        private DispatcherTimer animationTimer;
        private Polyline animatedPolyline;
        private double X_anim, Y_anim, Vx_anim, Vy_anim, t_anim;

        public void LaunchConditions(double V1, double ugol1, double m, double k)
        {
            ugol = ugol1 * (pi / 180);
            V = V1;
            M = m;
            k0 = k;
        }

        public void EulerMethodForBirdGame(double V, double ugol, double dt, double k)
        {
            this.dt = dt;
            t = t0;
            Vx = V * Math.Cos(ugol);
            Vy = V * Math.Sin(ugol);
            X = X0;
            Y = Y0;
            y_max = 0;

            while (Y >= 0)
            {
                if (Y > y_max) y_max = Y;

                double Ax = -(k / M) * Vx;
                double Ay = -g - (k / M) * Vy;
                Vx += Ax * dt;
                Vy += Ay * dt;
                X += Vx * dt;
                Y += Vy * dt;
                t += dt;
            }

            T_max = t;
            x_max = X;
        }

        public void TrajectoryAnimation(Canvas canvas, TextBlock timeTextBlock, double k)
        {
            X_anim = 0;
            Y_anim = 0;
            t_anim = 0;
            Vx_anim = V * Math.Cos(ugol);
            Vy_anim = V * Math.Sin(ugol);

            animatedPolyline = new Polyline
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };
            canvas.Children.Add(animatedPolyline);

            animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(30);
            animationTimer.Tick += (s, e) => //Обработка события, когда таймер совершил тик. Рисование одной точки траектории.
            {
                if (Y_anim < 0)
                {
                    animationTimer.Stop();
                    InformLanding?.Invoke("Птица завершила полёт.");
                    return;
                }

                double scaleX = 10;
                double scaleY = 10;

                Point point = new Point(X_anim * scaleX, canvas.Height - Y_anim * scaleY);
                animatedPolyline.Points.Add(point);

                double Ax_anim = -(k / M) * Vx_anim;
                double Ay_anim = -g - (k / M) * Vy_anim;

                Vx_anim += Ax_anim * dt;
                Vy_anim += Ay_anim * dt;
                X_anim += Vx_anim * dt;
                Y_anim += Vy_anim * dt;
                t_anim += dt;


                timeTextBlock.Text = $"Время полёта: {t_anim:F2} с";
            };

            animationTimer.Start();
        }

        public void DrawGrid(Canvas canvas, double step = 30)
        {
            double width = canvas.ActualWidth;
            double height = canvas.ActualHeight;

            for (double y = 0; y <= height; y += step)
            {
                Line hLine = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };
                canvas.Children.Add(hLine);
            }

            for (double x = 0; x <= width; x += step)
            {
                Line vLine = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };
                canvas.Children.Add(vLine);
            }

            Line xAxis = new Line
            {
                X1 = 0,
                Y1 = height,
                X2 = width,
                Y2 = height,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            canvas.Children.Add(xAxis);

            Line yAxis = new Line
            {
                X1 = 0,
                Y1 = 0,
                X2 = 0,
                Y2 = height,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            canvas.Children.Add(yAxis);
        }
    }
}
