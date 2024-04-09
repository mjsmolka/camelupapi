using CamelUpAutomation.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamelUpAutomation.Models
{
    public class Camel
    {
        public string id { get; set; }
        public CamelColor Color { get; set; }
        
        public bool IsCrazyCamel { get; set; }

        public int Position { get; set; }
        public int Height { get; set; }
    }
}
