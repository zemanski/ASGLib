using ASGLib.Chess;
using ASGLib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

//Superior Namespace : Abstract ASG Class
namespace ASGLib
{

    //Abstract Class for an Abstract Strategy Game Match : Constrained by ASGMove's Decendants and Inputs as well as Decendant Class for Serialization/Deserialization
    public abstract class ASG<TASG, TMove, TMoveDTO> where TASG : ASG<TASG, TMove, TMoveDTO> where TMove : ASGMove<TMoveDTO> where TMoveDTO : ASGMoveDTO
    {

        //Immutable Game Filepath : Set on Declaration for New or Non-Serialized Game Objects 
        private String gameFilePath;

        //Feilds Required by PGN (Default for PDN)
        protected String gameEvent;     //Event Name
        protected String gameSite;      //Location of Match
        protected String gameDate;      //Date of Match
        protected String gameRound;     //Round of Match (Tournament)
        protected String gameResult;    //Result of Match
        protected Dictionary<String, String> gamePlayers;   //Player Color/Number : Player Name

        //Dictionary for User-Defined or Optional Tags
        protected Dictionary<String, String> gameTags;      //Tag Pairs: Tag , Value

        //Generic Moveset
        protected List<TMove> moves;

        //Constructor : Accessible by Child Constructor Chain
        protected ASG(String gameFilePath)
        {
            this.gameFilePath = gameFilePath;
            this.gameEvent    = "";
            this.gameSite     = "";
            this.gameDate     = "";
            this.gameRound    = "";
            this.gamePlayers  = new Dictionary<String, String>();
            this.gameResult   = "";
            this.gameTags     = new Dictionary<String, String>();
            this.moves        = new List<TMove>();
        }

        //Abstract Json Moves Serializer and Move Deserializer Contracts
        protected abstract TMove MoveFromDTO(TMoveDTO dto);
        protected abstract List<TMoveDTO> MovesToDTO();

        //Generic Json Serializer
        protected string ToJson()
        {
            Data.ASGDTO<TMoveDTO> dto = new Data.ASGDTO<TMoveDTO>
            {
                GameEvent   = gameEvent,
                GameSite    = gameSite,
                GameDate    = gameDate,
                GameRound   = gameRound,
                GameResult  = gameResult,
                GamePlayers = gamePlayers,
                GameTags    = gameTags,
                Moves       = MovesToDTO()
            };
            return JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        }

        //Static Generic Json Deserializer, Runs on Game Decendent Factory
        protected static TASG FromJson(string json, Func<TASG> factory)
        {
            Data.ASGDTO<TMoveDTO> dto = JsonSerializer.Deserialize<Data.ASGDTO<TMoveDTO>>(json)
                ?? throw new Exception("Failed Deserialization.");
            TASG game = factory();
            game.gameEvent   = dto.GameEvent;
            game.gameSite    = dto.GameSite;
            game.gameDate    = dto.GameDate;
            game.gameRound   = dto.GameRound;
            game.gameResult  = dto.GameResult;
            game.gamePlayers = dto.GamePlayers;
            game.gameTags    = dto.GameTags;
            game.moves       = dto.Moves.Select(game.MoveFromDTO).ToList();
            return game;
        }
    }
}
