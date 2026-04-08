using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Nested Data Namespace : ASG Data Types and Transfer Objects
namespace ASGLib.Data
{

    //Abstract ASGMove Base Data Transfer Object
    internal abstract class ASGMoveDTO
    {
        public string MoveString { get; set; } = "";
        public int TurnNum { get; set; }
        public string TurnPlayer { get; set; } = "";
    }
}
