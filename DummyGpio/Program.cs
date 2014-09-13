using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DummyGpio
{
    class Program
    {
        static void Main(string[] args)
        {
            RaspberryPi pi = new RaspberryPi();

            switch(args[0])
            {
                case "-g":
                    pi.Gpio(RaspberryPi.NextStepArray(args));
                    break;
            }


        }


    }

    class RaspberryPi
    {
        public void Gpio(string[] args)
        {
            switch(args[0])
            {
                case "mode":
                    this.Mode(RaspberryPi.NextStepArray(args));
                    break;
                case "write":
                    this.Write(RaspberryPi.NextStepArray(args));
                    break;
                case "read":
                    this.Read(RaspberryPi.NextStepArray(args));
                    break;
            }
        }

        private void Mode(string[] args)
        {
            int no = int.Parse(args[0]);

            switch(args[1])
            {
                case "in":
                    break;
                case "out":
                    break;
            }
        }

        private void Read(string[] args)
        {
            System.Console.WriteLine("1");
        }

        private void Write(string[] args)
        {
        }

        public static string[] NextStepArray(string[] args)
        {
            var list = new List<string>();

            for (int i = 1; i < args.Length; i++)
            {
                list.Add(args[i]);
            }
            return list.ToArray();
        }
    }
}
