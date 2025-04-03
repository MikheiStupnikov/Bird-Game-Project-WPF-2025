using System;
using System.Windows;
using System.Windows.Controls;


namespace ParabolaSimulation
{
    public class MainWindow : Window
    {
        private TextBox velocityInput, angleInput, timeStepInput, massInput;
        private TextBlock resultBlock;
        

        public MainWindow()
        {
            Title = "Моделирование полета птицы";
            Width = 400;
            Height = 400;

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

            Button calculateButton = new Button { Content = "Получить результат" };
            calculateButton.Click += CalculateButton_Click;
            panel.Children.Add(calculateButton);

            resultBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(10) };
            panel.Children.Add(resultBlock);

            Content = panel;
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            resultBlock.Text = ""; // Очистка предыдущего вывода
            

            if (double.TryParse(velocityInput.Text, out double V1) &&
                double.TryParse(angleInput.Text, out double ugol1) &&
                double.TryParse(timeStepInput.Text, out double dt) &&
                double.TryParse(massInput.Text, out double m))
            {
                Parabola p = new Parabola();
                p.InformLanding += message => resultBlock.Text += message + "\n";


                p.LaunchConditions(V1, ugol1, m);
                p.EulerMethodForBirdGame(p.V, p.ugol, dt);
                
                resultBlock.FontSize = 18;
                resultBlock.Text += $"Дальность полёта: {p.x_max:F4} м\n";
                resultBlock.Text += $"Максимальная высота: {p.y_max:F4} м\n";
                resultBlock.Text += $"Время полёта: {p.T_max:F4} с\n";
            }
            else
            {
                resultBlock.FontSize = 24;
                resultBlock.Text = "Ошибка ввода! Проверьте корректность данных: все поля ввода принимают только числа. Дроби надо писать через запятую.";
            }
        }
    }

    public class Parabola
    {
        private double g = 9.81, k = 0.1, pi = Math.PI;
        public double V, ugol, T_max, x_max, y_max, X, Y, X0, Y0, t0, Vx, Vy, dt, M, t;

        public delegate void BirdFall(string message);
        public event BirdFall InformLanding;

        public void LaunchConditions(double V1, double ugol1, double m)
        {
            ugol = ugol1 * (pi / 180);
            V = V1;
            M = m;
        }

        public void EulerMethodForBirdGame(double V, double ugol, double dt)
        {
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
            InformLanding?.Invoke("Птица достигла земли.");
        }
    }

    public class App : Application
    {
        [STAThread]
        public static void Main()
        {
            App app = new App();
            app.Run(new MainWindow());
        }
    }
}