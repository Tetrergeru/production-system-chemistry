using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace GraphFunc
{
    public class Evaluator
    {
        private List<string> _textRules;

        public List<(string name, string tag)> Items;

        private List<(List<int> left, int right, int trueRule)> _rules;

        private Dictionary<int, int> _checkedItems;

        private List<string> _evaluation;

        private HashSet<int> _usedTrueRules;
        
        private HashSet<int> _usedRules;

        public IEnumerable<string> Eval(IEnumerable<string> startItems, string desiredItem)
        {
            var goal = ItemByNameOrTag(desiredItem);
            if (goal < 0)
                return new[] {$"Cant find item {desiredItem}"};

            _checkedItems = new Dictionary<int, int>();
            foreach (var item in startItems)
            {
                var idx = ItemByNameOrTag(item);
                if (goal < 0)
                    return new[] {$"Cant find item {desiredItem}"};
                _checkedItems[idx] = -1;
            }

            Eval(goal);
            if (_checkedItems.ContainsKey(goal))
            {
                _evaluation = new List<string>();
                _usedTrueRules = new HashSet<int>();
                CollectEvaluation(goal);
                return _evaluation;
            }

            return new[] {"Реакция невозможна!"};
        }

        public IEnumerable<string> EvalBack(IEnumerable<string> startItems, string desiredItem)
        {
            var goal = ItemByNameOrTag(desiredItem);
            if (goal < 0)
                return new[] {$"Cant find item {desiredItem}"};

            _checkedItems = new Dictionary<int, int>();
            foreach (var item in startItems)
            {
                var idx = ItemByNameOrTag(item);
                if (goal < 0)
                    return new[] {$"Cant find item {desiredItem}"};
                _checkedItems[idx] = -1;
            }

            _usedRules = new HashSet<int>();
            EvalBack(goal);
            if (_checkedItems.ContainsKey(goal))
            {
                _evaluation = new List<string>();
                _usedTrueRules = new HashSet<int>();
                CollectEvaluation(goal);
                return _evaluation;
            }


            return new[] { "Реакция невозможна!" };
        }

        private void EvalBack(int goal)
        {
            if (_checkedItems.ContainsKey(goal))
                return;
            var rules = _rules
                .Select((r, i) => (r.left, r.right, r.trueRule, rule: i))
                .Where(r => r.right == goal)
                .ToList();
            foreach (var rule in rules)
            {
                if (_usedRules.Contains(rule.rule))
                    continue;
                _usedRules.Add(rule.rule);
                foreach (var item in rule.left.Where(item => !_checkedItems.ContainsKey(item) && item != goal))
                    EvalBack(item);

                if (_checkedItems.ContainsKey(goal))
                    return;
                
                if (rule.left.All(item => _checkedItems.ContainsKey(item)))
                {
                    _checkedItems[goal] = rule.rule;
                    break;
                }
            }
        }

        private void Eval(int goal)
        {
            var prevLen = _checkedItems.Count;
            var rIdx = 0;
            var rules = _rules.ToDictionary(r => rIdx++, r => r);
            while (true)
            {
                if (_checkedItems.ContainsKey(goal))
                    return;
                var rulesToRemove = new List<int>();
                foreach (var idxRule in rules)
                {
                    if (_checkedItems.ContainsKey(idxRule.Value.right))
                        rulesToRemove.Add(idxRule.Key);
                    else if (idxRule.Value.left.Select(i => _checkedItems.ContainsKey(i)).All(x => x))
                    {
                        _checkedItems.Add(idxRule.Value.right, idxRule.Key);
                        rulesToRemove.Add(idxRule.Key);
                    }
                }

                rulesToRemove.ForEach(r => rules.Remove(r));
                if (_checkedItems.Count == prevLen)
                    return;
                prevLen = _checkedItems.Count;
            }
        }


        private void CollectEvaluation(int currentGoal)
        {
            if (_checkedItems[currentGoal] == -1 || _usedTrueRules.Contains(currentGoal))
                return;
            var rule = _checkedItems[currentGoal];
            foreach (var item in _rules[rule].left)
            {
                CollectEvaluation(item);
                _usedTrueRules.Add(item);
            }

            if (!_usedTrueRules.Contains(currentGoal))
                _evaluation.Add(_textRules[_rules[rule].trueRule]);
        }

        public int ItemByNameOrTag(string item)
        {
            var idx = ItemIndex(item);
            return idx >= 0
                ? idx
                : ItemByTag(item);
        }

        public int ItemIndex(string item)
        {
            var idx = Items.FindIndex(i => i.name == item);
            return idx >= Items.Count
                ? -1
                : idx;
        }

        public int ItemByTag(string item)
        {
            var idx = Items.FindIndex(i => i.tag == item);
            return idx >= Items.Count
                ? -1
                : idx;
        }

        public void InsertItem(string item, string description)
        {
            if (ItemIndex(item) < 0)
                Items.Add((item, description));
        }

        public static Evaluator LoadFromFile(string items, string rules)
        {
            var evaluator = new Evaluator();
            evaluator.Items = new List<(string name, string tag)>();
            foreach (var (file, description) in
                File
                    .ReadAllLines(items)
                    .Select(s => s.Split(new[] {"->"}, StringSplitOptions.None))
                    .Where(l => l.Length == 2)
                    .Select(l => (l[0].Trim(), l[1].Trim())))
                evaluator.InsertItem(file, description);

            evaluator._textRules = File
                .ReadAllLines(rules)
                .Where(s => s.Split(new[] {"->"}, StringSplitOptions.None).Length == 2)
                .ToList();

            var parsedRules = evaluator._textRules
                .Select(s => s.Split(new[] {"->"}, StringSplitOptions.None))
                .Where(l => l.Length == 2)
                .Select(l => (left: l[0], right: l[1]))
                .Select((l, trueRule) => (
                    left: l.left.Split('+').Select(i => i.Trim()).ToList(),
                    right: l.right.Split('+').Select(i => i.Trim()),
                    trueRule
                ))
                .SelectMany((l, _) => l.right.Select(r => (l.left, r, l.trueRule)))
                .ToList();

            foreach (var item in parsedRules.SelectMany(l => l.left.Append(l.r)))
                evaluator.InsertItem(item, item);

            evaluator._rules = parsedRules
                .Select(l => (
                        l.left.Select(i => evaluator.ItemIndex(i)).ToList(),
                        evaluator.ItemIndex(l.r),
                        l.trueRule
                    )
                ).ToList();

            return evaluator;
        }
    }
}