using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GateController;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            GateController.GateController aa = new GateController.GateController();
            aa.OnTimer(null, null);
            Console.ReadKey();
        }
    }
}
