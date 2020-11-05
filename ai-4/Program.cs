using System;
using System.Windows.Forms;

namespace GraphFunc
{
    class Program
    {
        static void Main(string[] args)
        {
            var _form = new Form(Evaluator.LoadFromFile("Substances.txt", "Chemistry.txt"));
            Application.Run(_form);
        }
    }
}
