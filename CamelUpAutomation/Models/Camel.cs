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
        int Id { get; set; }
        CamelColor Color { get; set; }
        
        bool IsCrazyCamel { get; set; }
    }
}
