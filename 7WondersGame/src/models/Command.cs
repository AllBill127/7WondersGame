using OfficeOpenXml.FormulaParsing.Excel.Functions.Engineering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace _7WondersGame.src.models
{
    public class Command
    {
        public string Subcommand { get; set; }
        public Card? Argument { get; set; }

        public Command(string subcommand, Card? argument)
        {
            this.Subcommand = subcommand;
            this.Argument = argument;
        }

        public override bool Equals(object? obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Command c = (Command)obj;
                return Subcommand == c.Subcommand &&
                    Argument == c.Argument;
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Subcommand, Argument);
        }
    }
}
