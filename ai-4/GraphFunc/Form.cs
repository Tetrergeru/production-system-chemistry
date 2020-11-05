using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace GraphFunc
{
    class Form : System.Windows.Forms.Form
    {
        private readonly PictureBox _functionBox;

        private Graphics _drawer;
        
        private const double Step = 0.01;
        
        private (double from, double to) _range;

        private (double min, double max) _bounds;
        
        private void SetBounds(Func<double, double> func)
        {
            var max = double.MinValue;
            var min = double.MaxValue;
            var pos = _range.from;
            while (pos < _range.to)
            {
                var fPos = func(pos);
                if (!IsBad(fPos) && fPos > max)
                    max = fPos;
                if (!IsBad(fPos) && fPos < min)
                    min = fPos;
                pos += Step;
            }

            _bounds = (min, max);
        }

        private void DrawCoords()
        {
            var w = _functionBox.Width;
            var h = _functionBox.Height;
            var zeroH = _bounds.max * h / (_bounds.max - _bounds.min);
            var zeroW = _range.from * w / (_range.from - _range.to);
            _drawer.DrawLine(new Pen(Color.Black, 2), (int)zeroW, 0, (int)zeroW, h);
            _drawer.DrawLine(new Pen(Color.Black, 2), 0, (int)zeroH, w, (int)zeroH);
            for (var i = (int) _range.from; i <= _range.to; i++)
            {
                (int x, int y) c = Translate(i, 0);
                _drawer.DrawLine(new Pen(Color.Black, 2), c.x, c.y - 3, c.x, c.y + 3);
            }
            for (var i = (int) _bounds.min; i <= _bounds.max; i++)
            {
                (int x, int y) c = Translate(0, i);
                _drawer.DrawLine(new Pen(Color.Black, 2), c.x - 3, c.y, c.x + 3, c.y);
            }
        }

        private (int, int) Translate(double x, double y)
        {
            var w = _functionBox.Width;
            var h = _functionBox.Height;
            var X = (int)((_range.from - x) * w / (_range.from - _range.to));
            var Y = (int)((_bounds.max - y) * h / (_bounds.max - _bounds.min));
            return (X, Y);
        }

        private static bool IsBad(double d)
            => double.IsNaN(d) || double.IsInfinity(d) || d > 10000 || d < -10000;

        private static bool IsBad((double x, double y) coordinate)
            => IsBad(coordinate.x) || IsBad(coordinate.y);

        private void DrawFunc(Func<double, double> func)
        {
            var pos = _range.from;
            (int x, int y) prev = Translate(pos, func(pos));
            while (pos < _range.to)
            {
                var fPos = func(pos);
                (int x, int y) cur = Translate(pos, fPos);
                if (IsBad(prev))
                {
                    prev = cur;
                    pos += Step;
                    continue;
                }
                if (IsBad(cur))
                {
                    prev = cur;
                    pos += Step;
                    continue;
                }

                _drawer.DrawLine(new Pen(Color.Red, 2), prev.x, prev.y, cur.x, cur.y);
                prev = cur;
                pos += Step;
            }
        }

        private void Draw(Func<double, double> func)
        {
            SetBounds(func);
            var picture = new Bitmap(_functionBox.Width, _functionBox.Height);
            _drawer = Graphics.FromImage(picture);
            _drawer.FillRectangle(new SolidBrush(Color.Beige), 0, 0, _functionBox.Width, _functionBox.Height);
            DrawCoords();
            DrawFunc(func);
            _functionBox.Image = picture;
        }

        private Func<double, double> GetFunc(string func)
        {
            var list = func.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            return x =>
            {
                var stack = new Stack<double>();
                foreach (var command in list)
                {
                    switch (command.ToLower())
                    {
                        case "x":
                            stack.Push(x);
                            break;
                        case "e":
                            stack.Push(Math.E);
                            break;
                        case "pi":
                            stack.Push(Math.PI);
                            break;
                        case "+":
                            stack.Push(stack.Pop() + stack.Pop());
                            break;
                        case "--":
                        {
                            var b = stack.Pop();
                            var a = stack.Pop();
                            stack.Push(a - b);
                            break;
                        }
                        case "*":
                            stack.Push(stack.Pop() * stack.Pop());
                            break;
                        case "-":
                            stack.Push(-stack.Pop());
                            break;
                        case "/":
                        {
                            var b = stack.Pop();
                            var a = stack.Pop();
                            stack.Push(a / b);
                            break;
                        }
                        case "^":
                        {
                            var b = stack.Pop();
                            var a = stack.Pop();
                            stack.Push(Math.Pow(a, b));
                            break;
                        }
                        case "sin":
                            stack.Push(Math.Sin(stack.Pop()));
                            break;
                        case "cos":
                            stack.Push(Math.Cos(stack.Pop()));
                            break;
                        case "tg":
                            stack.Push(Math.Tan(stack.Pop()));
                            break;
                        case "lg":
                            stack.Push(Math.Log(stack.Pop(), 2));
                            break;
                        case "log":
                        {
                            var b = stack.Pop();
                            var a = stack.Pop();
                            stack.Push(Math.Log(a, b));
                            break;
                        }
                        default:
                            stack.Push(double.Parse(command, new CultureInfo("en-US")));
                            break;
                    }
                }

                return stack.Pop();
            };
        }

        public Form()
        {
            Width = 1000;
            Height = 600;
            
            _functionBox = new PictureBox()
            {
                Width = 800,
                Height = 400,
                Left = 100,
                Top = 75,
            };
            Controls.Add(_functionBox);

            var sourceBox = new TextBox()
            {
                Width = 200,
                Height = 20,
                Left = 110,
                Top = 20,
                Text = "x sin",
            };
            Controls.Add(sourceBox);

            var rangeBox = new TextBox()
            {
                Width = 50,
                Height = 20,
                Left = 310,
                Top = 20,
                Text = "-10 10",
            };
            Controls.Add(rangeBox);
            
            var sendButton = new Button()
            {
                Width = 50,
                Height = 20,
                Text = "Draw!",
                Left = 360,
                Top = 20,
            };
            Controls.Add(sendButton);
            sendButton.Click += (o, e) =>
            {
                var spl = rangeBox.Text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                _range = (double.Parse(spl[0], new CultureInfo("en-US")), double.Parse(spl[1], new CultureInfo("en-US")));
                Draw(GetFunc(sourceBox.Text));
            };
            
            _range = (-10, 10);
            Draw(GetFunc(sourceBox.Text));
        }
    }
}