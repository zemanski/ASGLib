using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Nested Data Namespace : ASG Data Types and Transfer Objects
namespace ASGLib.Data
{

    //ASG Generic Data Transfer Object
    public class ASGDTO<TMoveDTO> where TMoveDTO : ASGMoveDTO
    {
        public string GameEvent { get; set; } = "";
        public string GameSite { get; set; } = "";
        public string GameDate { get; set; } = "";
        public string GameRound { get; set; } = "";
        public string GameResult { get; set; } = "";
        public Dictionary<string, string> GamePlayers { get; set; } = new();
        public Dictionary<string, string> GameTags { get; set; } = new();
        public List<TMoveDTO> Moves { get; set; } = new();
    }
}
