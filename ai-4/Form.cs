using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GraphFunc
{
    class Form : System.Windows.Forms.Form
    {
        private TextBox _evaluationBox;

        private readonly List<string> _categories;

        private readonly List<CheckBox> _categoryBoxes = new List<CheckBox>();

        private readonly HashSet<string> _checked = new HashSet<string>();

        private string _targetElement = "Au";

        private readonly Evaluator _evaluator;

        public Form(Evaluator evaluator)
        {
            _categories = evaluator.Items.Select(i => i.tag).OrderBy(x => x).ToList();
            _evaluator = evaluator;
            CategoryField();
            AddEvaluationBox();
        }

        private void AddEvaluationBox()
        {
            _evaluationBox = new TextBox
            {
                Left = 50,
                Top = 30,
                Width = 500,
                Height = 530,
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.LightYellow,
                ForeColor = Color.DarkBlue,
                Text = $"Давайте-ка попробуем получить {_targetElement}",
            };
            Controls.Add(_evaluationBox);
        }

        private void CategoryField()
        {
            var controlsPanel = new Panel
            {
                Left = 560,
                Top = 30,
                Width = 200,
                Height = 500,
                AutoScroll = true,
                BackColor = Color.DarkGreen,
            };
            controlsPanel.Scroll += (sender, args) => controlsPanel.Invalidate();

            Width = controlsPanel.Left + controlsPanel.Width + 20;
            Height = controlsPanel.Top + controlsPanel.Height + 100;

            foreach (var cat in _categories)
                AddCategory(cat, controlsPanel);
            Controls.Add(controlsPanel);

            var goButton = new Button
            {
                Top = controlsPanel.Top + controlsPanel.Height,
                Left = controlsPanel.Left,
                Width = controlsPanel.Width,
                Height = 30,
                Text = "GO",
            };
            goButton.Click += (sender, args) => Evaluate();
            Controls.Add(goButton);
        }

        private void AddCategory(string name, Panel controlsPanel)
        {
            var i = _categoryBoxes.Count;
            var label = new Label
            {
                Width = 140,
                Height = 15,
                Top = i * 20,
                Left = 15,
                Text = name,
                ForeColor = Color.Gold,
            };

            var categoryBox = new CheckBox
            {
                Width = 10,
                Height = 10,
                Top = i * 20 + 3,
            };
            var j = i;
            categoryBox.CheckedChanged += (sender, args) =>
            {
                if (categoryBox.Checked)
                    _checked.Add(_categories[j]);
                else
                    _checked.Remove(_categories[j]);
            };
            _categoryBoxes.Add(categoryBox);

            var categoryRadioButton = new RadioButton
            {
                Width = 10,
                Height = 10,
                Top = i * 20 + 3,
                Left = 170,
                BackColor = controlsPanel.BackColor,
            };

            categoryRadioButton.CheckedChanged += (sender, args) =>
            {
                if (categoryRadioButton.Checked)
                    _targetElement = _categories[j];
                Evaluate();
            };

            controlsPanel.Controls.Add(label);
            controlsPanel.Controls.Add(categoryBox);
            controlsPanel.Controls.Add(categoryRadioButton);
        }

        private void Evaluate()
        {
            _evaluationBox.Text =
                $"Давайте-ка попробуем получить {_targetElement}\r\n"
                + string.Join("\r\n", _evaluator.Eval(_checked, _targetElement));
        }
    }
}